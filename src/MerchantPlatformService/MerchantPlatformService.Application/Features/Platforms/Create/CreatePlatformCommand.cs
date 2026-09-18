using MediatR;

namespace MerchantPlatformService.Application.Features.Platforms.Create;

/// <summary>新建平台命令：编码（6 位字母）/名称/邮箱/费率。</summary>
public sealed record CreatePlatformCommand : IRequest<object>
{
    /// <summary>平台编码（6 位字母，创建后不可改）。</summary>
    public string PlatformCode { get; init; } = string.Empty;
    /// <summary>平台名称。</summary>
    public string PlatformName { get; init; } = string.Empty;
    /// <summary>联系邮箱。</summary>
    public string ContactEmail { get; init; } = string.Empty;
    /// <summary>默认佣金比例（百分比）。</summary>
    public decimal DefaultCommissionRate { get; init; }
}
