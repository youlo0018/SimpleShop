using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.Create;

/// <summary>商户入驻命令：平台/名称/联系人/费率。</summary>
public sealed record CreateMerchantCommand : IRequest<object>
{
    /// <summary>平台 ID。</summary>
    public long PlatformId { get; init; }
    /// <summary>商户名称。</summary>
    public string MerchantName { get; init; } = string.Empty;
    /// <summary>联系人。</summary>
    public string ContactName { get; init; } = string.Empty;
    /// <summary>联系电话。</summary>
    public string ContactPhone { get; init; } = string.Empty;
    /// <summary>联系邮箱。</summary>
    public string ContactEmail { get; init; } = string.Empty;
    /// <summary>佣金比例（百分比）。</summary>
    public decimal CommissionRate { get; init; }
}
