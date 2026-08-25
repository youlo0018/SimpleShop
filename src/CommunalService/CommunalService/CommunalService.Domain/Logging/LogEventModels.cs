using System.Text.Json;
using CommunalService.Domain.Messaging;

namespace CommunalService.Domain.Logging;

/// <summary>
/// PV 日志：记录用户看了什么页面、哪个商品、从哪里进来，用于转化分析和推荐。
/// </summary>
public sealed record PageViewEvent
{
    public string Page { get; init; } = string.Empty;

    public long ItemId { get; init; }

    public long SkuId { get; init; }

    public string Source { get; init; } = string.Empty;

    public int StayMilliseconds { get; init; }
}

/// <summary>
/// 业务操作日志：后台改了什么数据、谁改的、什么时候改的，审计时能还原现场。
/// </summary>
public sealed record OperationLogEvent
{
    public string OperationType { get; init; } = string.Empty;

    public string ObjectType { get; init; } = string.Empty;

    public string ObjectId { get; init; } = string.Empty;

    public string OperatorType { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string Ip { get; init; } = string.Empty;
}

/// <summary>
/// 异常日志：记录接口异常堆栈和请求摘要；用户只需要看到“系统繁忙”，排障信息进日志。
/// </summary>
public sealed record ExceptionLogEvent
{
    public string Service { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public string Method { get; init; } = string.Empty;

    public int StatusCode { get; init; }

    public string ExceptionType { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string StackTrace { get; set; } = string.Empty;
}
