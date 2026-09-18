using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.Review;

/// <summary>
/// 商户审核：新前端传 Approved，旧前端可传 Status（10=通过）；两者都缺省视为无效请求。
/// 归一化与状态机校验在 Handler，参数校验在 ReviewMerchantValidator。
/// </summary>
public sealed record ReviewMerchantCommand : IRequest<object>
{
    /// <summary>商户 ID。</summary>
    public long Id { get; init; }

    /// <summary>审核结论：true 通过 / false 驳回。</summary>
    public bool? Approved { get; init; }

    /// <summary>兼容旧前端的审核状态数字（10=通过）。</summary>
    public int? Status { get; init; }

    /// <summary>驳回原因（驳回时必填，空则用默认文案）。</summary>
    public string? Reason { get; init; }
}
