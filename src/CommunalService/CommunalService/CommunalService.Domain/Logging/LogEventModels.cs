using System.Text.Json;
using CommunalService.Domain.Messaging;

namespace CommunalService.Domain.Logging;

/// <summary>
/// PV 日志：记录用户看了什么页面、哪个商品、从哪里进来，用于转化分析和推荐。
/// </summary>
public sealed record PageViewEvent
{
    /// <summary>页码（从 1 开始）。</summary>
    public string Page { get; init; } = string.Empty;

    /// <summary>条目 ID。</summary>
    public long ItemId { get; init; }

    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; init; }

    /// <summary>来源。</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>停留毫秒数。</summary>
    public int StayMilliseconds { get; init; }
}

/// <summary>
/// 业务操作日志：后台改了什么数据、谁改的、什么时候改的，审计时能还原现场。
/// </summary>
public sealed record OperationLogEvent
{
    /// <summary>操作类型。</summary>
    public string OperationType { get; init; } = string.Empty;

    /// <summary>操作对象类型。</summary>
    public string ObjectType { get; init; } = string.Empty;

    /// <summary>操作对象 ID。</summary>
    public string ObjectId { get; init; } = string.Empty;

    /// <summary>操作员类型（平台/商户/客户/系统）。</summary>
    public string OperatorType { get; init; } = string.Empty;

    /// <summary>操作摘要（脱敏后）。</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>客户端 IP（脱敏后）。</summary>
    public string Ip { get; init; } = string.Empty;
}

/// <summary>
/// 异常日志：记录接口异常堆栈和请求摘要；用户只需要看到“系统繁忙”，排障信息进日志。
/// </summary>
public sealed record ExceptionLogEvent
{
    /// <summary>服务名。</summary>
    public string Service { get; init; } = string.Empty;

    /// <summary>请求路径。</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>HTTP 方法。</summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>HTTP 状态码。</summary>
    public int StatusCode { get; init; }

    /// <summary>异常类型。</summary>
    public string ExceptionType { get; init; } = string.Empty;

    /// <summary>消息内容。</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>异常堆栈（脱敏后）。</summary>
    public string StackTrace { get; set; } = string.Empty;
}
