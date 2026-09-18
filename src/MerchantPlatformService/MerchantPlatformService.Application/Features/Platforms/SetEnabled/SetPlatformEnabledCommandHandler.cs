using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Platforms.SetEnabled;

/// <summary>
/// 平台启停：停用后商城端 MiniAppPlatforms 不再返回该平台，新用户无法进入。
/// </summary>
public class SetPlatformEnabledCommandHandler(IPlatformRepository repository)
    : IRequestHandler<SetPlatformEnabledCommand, ApiResponse>
{
    /// <summary>处理入口：平台启停：停用后商城端 MiniAppPlatforms 不再返回该平台，新用户无法进入。</summary>
    public async Task<ApiResponse> Handle(SetPlatformEnabledCommand request, CancellationToken cancellationToken)
    {
        var platform = await repository.GetByIdAsync(request.Id);
        if (platform is null || platform.IsDeleted)
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "平台不存在");

        platform.IsEnabled = request.IsEnabled;
        await repository.UpdateAsync(platform);
        return ApiResults.Ok(new { success = true });
    }
}
