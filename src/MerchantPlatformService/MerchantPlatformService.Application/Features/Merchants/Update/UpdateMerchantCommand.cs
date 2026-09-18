using CommunalService.Domain;
using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.Update;

/// <summary>商户资料更新（按租户裁剪，越权拒绝）。</summary>
/// <param name="Id">主键。</param>
/// <param name="PlatformId">平台 ID。</param>
/// <param name="MerchantName">商户名称。</param>
/// <param name="ContactName">联系人。</param>
public record UpdateMerchantCommand(long Id, long PlatformId, string MerchantName, string ContactName,
    string ContactPhone, string ContactEmail, decimal CommissionRate) : IRequest<ApiResponse>;
