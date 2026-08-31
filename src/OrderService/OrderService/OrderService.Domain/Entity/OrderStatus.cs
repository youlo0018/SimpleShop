namespace OrderService.Domain.Entity;

/// <summary>
/// 订单状态机：10 待支付 →（支付确认/支付成功事件）20 已支付 →（发货）40 已发货 →（签收）50 已完成；
/// 待支付超时（ScheduledService 关单）→ 91 已关闭；用户/后台取消 → 90 已取消；退款审批通过 → 60 已退款。
/// 所有状态迁移必须竞争 lock:order:{orderId}，并用仓储条件更新（TryXxx）做数据库层兜底。
/// </summary>
public enum OrderState
{
    AwaitPayment = 10,
    Paid = 20,
    Shipped = 40,
    Completed = 50,
    Refunded = 60,
    Cancelled = 90,
    Closed = 91
}
