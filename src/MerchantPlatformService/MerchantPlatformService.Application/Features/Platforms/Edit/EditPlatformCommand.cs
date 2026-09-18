using CommunalService.Domain;
using MediatR;

namespace MerchantPlatformService.Application.Features.Platforms.Edit;

/// <summary>编辑平台（编码不可改）。</summary>
/// <param name="Id">主键。</param>
/// <param name="PlatformCode">平台编码（6 位字母）。</param>
/// <param name="PlatformName">平台名称。</param>
/// <param name="ContactEmail">联系邮箱。</param>
public record EditPlatformCommand(long Id, string PlatformCode, string PlatformName, string ContactEmail,
    decimal DefaultCommissionRate) : IRequest<ApiResponse>;
