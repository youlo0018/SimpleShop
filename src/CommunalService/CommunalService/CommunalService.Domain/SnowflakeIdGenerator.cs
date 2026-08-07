namespace CommunalService.Domain;

public class SnowflakeIdGenerator
{
}
/*
 * 版权属于：yitter(yitter@126.com)
 * 开源地址：https://github.com/yitter/idgenerator
 * 版权协议：MIT
 * 版权说明：只要保留本版权，你可以免费使用、修改、分发本代码。
 * 免责条款：任何因为本代码产生的系统、法律、政治、宗教问题，均与版权所有者无关。
 *
 */

/// <summary>
/// 雪花漂移算法（方法1，也是默认方法）
/// </summary> 
internal class SnowWorkerM1 : ISnowWorker
{
    // ===== 只读配置字段 =====
    /// <summary>
    /// 基础时间（起始时间），用于计算时间差，从而让ID更短。
    /// 默认：2020-01-01 00:00:00 UTC
    /// </summary>
    protected readonly DateTime BaseTime;

    /// <summary>
    /// 机器码（Worker ID），唯一标识当前节点，最大由 WorkerIdBitLength 决定。
    /// </summary>
    protected readonly ushort WorkerId = 0;

    /// <summary>
    /// 机器码占用的位数，决定最多支持多少个节点（2^WorkerIdBitLength）。
    /// 默认值 6，即最多 64 个节点。
    /// </summary>
    protected readonly byte WorkerIdBitLength = 0;

    /// <summary>
    /// 序列号（自增计数器）占用的位数，决定每毫秒单个节点可生成的ID数量（2^SeqBitLength）。
    /// 默认值 6，即每毫秒 64 个，适合低并发；可调至 10 或 12 以支持更高并发。
    /// </summary>
    protected readonly byte SeqBitLength = 0;

    /// <summary>
    /// 最大序列号（含），由 SeqBitLength 自动计算得到，即 (1 << SeqBitLength) - 1。
    /// </summary>
    protected readonly int MaxSeqNumber = 0;

    /// <summary>
    /// 最小序列号（含），默认值为 5，用于预留部分序列号给时间回拨场景。
    /// </summary>
    protected readonly ushort MinSeqNumber = 0;

    /// <summary>
    /// 最大漂移次数（含），即允许在同一毫秒内连续漂移的次数上限。
    /// 超过此值后，算法会强制等待时间对齐，防止时间戳落后太多。
    /// </summary>
    protected int TopOverCostCount = 0;

    /// <summary>
    /// 时间戳左移的位数，等于 WorkerIdBitLength + SeqBitLength，
    /// 用于在 CalcId 中组装ID。
    /// </summary>
    protected byte _TimestampShift = 0;

    // ===== 线程同步对象 =====
    protected static object _SyncLock = new object();

    // ===== 运行时状态字段 =====
    protected ushort _CurrentSeqNumber = 0; // 当前的序列号（自增计数器）
    protected long _LastTimeTick = 0; // 上一次生成ID时使用的时间戳（相对BaseTime的毫秒数）
    protected long _TurnBackTimeTick = 0; // 发生时间回拨时记录的回拨时间点（用于补偿）
    protected byte _TurnBackIndex = 0; // 回拨序号（1~4循环），用于回拨时生成ID，避免重复
    protected bool _IsOverCost = false; // 是否处于“过载”（漂移）模式
    protected int _OverCostCountInOneTerm = 0; // 当前过载周期内已漂移的次数

#if DEBUG
    protected int _GenCountInOneTerm = 0;
    protected int _TermIndex = 0;
#endif

    // 可选回调，用于监控过载/回拨事件
    public Action<OverCostActionArg> GenAction { get; set; }

    // ===== 构造函数：初始化配置 =====
    public SnowWorkerM1(IdGeneratorOptions options)
    {
        // 1. 基础时间
        if (options.BaseTime != DateTime.MinValue)
        {
            BaseTime = options.BaseTime;
        }

        // 2. 机器码位长（默认6）
        if (options.WorkerIdBitLength == 0)
        {
            WorkerIdBitLength = 6;
        }
        else
        {
            WorkerIdBitLength = options.WorkerIdBitLength;
        }

        // 3. 机器码
        WorkerId = options.WorkerId;

        // 4. 序列号位长（默认6）
        if (options.SeqBitLength == 0)
        {
            SeqBitLength = 6;
        }
        else
        {
            SeqBitLength = options.SeqBitLength;
        }

        // 5. 最大序列号（含）
        if (options.MaxSeqNumber <= 0)
        {
            MaxSeqNumber = (1 << SeqBitLength) - 1; // 2^SeqBitLength - 1
        }
        else
        {
            MaxSeqNumber = options.MaxSeqNumber;
        }

        // 6. 最小序列号（默认5）
        MinSeqNumber = options.MinSeqNumber;

        // 7. 最大漂移次数（默认1000）
        TopOverCostCount = options.TopOverCostCount;

        // 8. 计算时间戳左移位数
        _TimestampShift = (byte)(WorkerIdBitLength + SeqBitLength);

        // 9. 初始序列号设为 MinSeqNumber
        _CurrentSeqNumber = options.MinSeqNumber;
    }

    // ===== DEBUG 回调方法（略，不影响核心逻辑） =====
    // ...

    // ===== 核心方法：生成下一个ID（入口） =====
    public virtual long NextId()
    {
        lock (_SyncLock) // 保证线程安全
        {
            // 根据是否处于“过载”模式，分流到不同的生成策略
            return _IsOverCost ? NextOverCostId() : NextNormalId();
        }
    }

    // ===== 正常模式（非过载） =====
    protected virtual long NextNormalId()
    {
        long currentTimeTick = GetCurrentTimeTick(); // 获取当前相对时间（毫秒）

        // ---- 情况1：发生时间回拨 ----
        if (currentTimeTick < _LastTimeTick)
        {
            // 如果回拨补偿尚未开始，则初始化回拨状态
            if (_TurnBackTimeTick < 1)
            {
                // 回拨时间点设为上一次使用的时间 - 1
                _TurnBackTimeTick = _LastTimeTick - 1;
                _TurnBackIndex++;
                if (_TurnBackIndex > 4) _TurnBackIndex = 1; // 回拨序号循环使用1~4

            }

            // 返回回拨补偿ID（使用回拨序号替代序列号）
            return CalcTurnBackId(_TurnBackTimeTick);
        }

        // ---- 情况2：时间追平，清除回拨状态 ----
        if (_TurnBackTimeTick > 0)
        {
#if DEBUG
            EndTurnBackAction(_TurnBackTimeTick);
#endif
            _TurnBackTimeTick = 0; // 回拨补偿结束
        }

        // ---- 情况3：时间推进（新毫秒） ----
        if (currentTimeTick > _LastTimeTick)
        {
            _LastTimeTick = currentTimeTick;
            _CurrentSeqNumber = MinSeqNumber; // 新毫秒，序列号复位到最小值
            return CalcId(_LastTimeTick);
        }

        // ---- 情况4：同一毫秒内 ----
        if (_CurrentSeqNumber > MaxSeqNumber)
        {
            // 序列号用尽，进入“过载”（漂移）模式
#if DEBUG
            BeginOverCostAction(currentTimeTick);
            _TermIndex++;
            _GenCountInOneTerm = 1;
#endif
            _OverCostCountInOneTerm = 1;
            _LastTimeTick++; // 时间戳微调（+1毫秒）
            _CurrentSeqNumber = MinSeqNumber; // 序列号复位
            _IsOverCost = true; // 标记为过载模式
            return CalcId(_LastTimeTick);
        }

        // ---- 常规情况：序列号未用尽，直接自增返回 ----
        return CalcId(_LastTimeTick);
    }

    // ===== 过载模式（漂移） =====
    protected virtual long NextOverCostId()
    {
        long currentTimeTick = GetCurrentTimeTick();

        // ---- 情况1：时间已推进（新毫秒），退出过载模式 ----
        if (currentTimeTick > _LastTimeTick)
        {
#if DEBUG
            EndOverCostAction(currentTimeTick);
            _GenCountInOneTerm = 0;
#endif
            _LastTimeTick = currentTimeTick;
            _CurrentSeqNumber = MinSeqNumber;
            _IsOverCost = false;
            _OverCostCountInOneTerm = 0;
            return CalcId(_LastTimeTick);
        }

        // ---- 情况2：漂移次数已达上限，强制等待时间对齐 ----
        if (_OverCostCountInOneTerm >= TopOverCostCount)
        {
#if DEBUG
            EndOverCostAction(currentTimeTick);
            _GenCountInOneTerm = 0;
#endif
            // 等待时间推进到下一毫秒
            _LastTimeTick = GetNextTimeTick(); // 自旋等待直到时间变化
            _CurrentSeqNumber = MinSeqNumber;
            _IsOverCost = false;
            _OverCostCountInOneTerm = 0;
            return CalcId(_LastTimeTick);
        }

        // ---- 情况3：继续漂移 ----
        if (_CurrentSeqNumber > MaxSeqNumber)
        {
#if DEBUG
            _GenCountInOneTerm++;
#endif
            _LastTimeTick++; // 再次微调时间戳
            _CurrentSeqNumber = MinSeqNumber;
            _IsOverCost = true;
            _OverCostCountInOneTerm++; // 漂移计数器+1
            return CalcId(_LastTimeTick);
        }

        // ---- 情况4：序列号未满，继续使用当前时间戳 ----
#if DEBUG
        _GenCountInOneTerm++;
#endif
        return CalcId(_LastTimeTick);
    }

    // ===== 辅助方法：拼接正常ID =====
    protected virtual long CalcId(long useTimeTick)
    {
        // 组装：时间戳(左移) | 机器码(左移序列号位数) | 序列号
        var result = ((useTimeTick << _TimestampShift) +
                      ((long)WorkerId << SeqBitLength) +
                      (uint)_CurrentSeqNumber);
        _CurrentSeqNumber++; // 自增，为下次准备
        return result;
    }

    // ===== 辅助方法：拼接回拨补偿ID =====
    protected virtual long CalcTurnBackId(long useTimeTick)
    {
        // 使用回拨序号（_TurnBackIndex，1~4）替代序列号，避免与正常ID冲突
        var result = ((useTimeTick << _TimestampShift) +
                      ((long)WorkerId << SeqBitLength) + _TurnBackIndex);
        _TurnBackTimeTick--; // 每次消耗一个回拨时间点，控制补偿次数
        return result;
    }

    // ===== 获取当前相对时间戳（毫秒） =====
    protected virtual long GetCurrentTimeTick()
    {
        // 计算当前UTC时间与BaseTime的毫秒差值
        return (long)(DateTime.UtcNow - BaseTime).TotalMilliseconds;
    }

    // ===== 获取下一毫秒时间戳（自旋等待） =====
    protected virtual long GetNextTimeTick()
    {
        long tempTimeTicker = GetCurrentTimeTick();
        // 循环直到时间大于_LastTimeTick
        while (tempTimeTicker <= _LastTimeTick)
        {
            // 短暂休眠1毫秒（或使用SpinWait）
            SpinWait.SpinUntil(() => false, 1);
            tempTimeTicker = GetCurrentTimeTick();
        }

        return tempTimeTicker;
    }
}