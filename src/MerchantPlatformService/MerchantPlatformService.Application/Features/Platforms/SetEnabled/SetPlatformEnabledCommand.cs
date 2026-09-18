using CommunalService.Domain;
using MediatR;

namespace MerchantPlatformService.Application.Features.Platforms.SetEnabled;

/// <summary>平台启停（停用后小程序不可进入）。</summary>
/// <param name="Id">主键。</param>
/// <param name="IsEnabled">是否启用。</param>
public record SetPlatformEnabledCommand(long Id, bool IsEnabled) : IRequest<ApiResponse>;
