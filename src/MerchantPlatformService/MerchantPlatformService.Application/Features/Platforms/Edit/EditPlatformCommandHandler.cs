using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using MerchantPlatformService.Domain.IRepository;

namespace MerchantPlatformService.Application.Features.Platforms.Edit;

/// <summary>
/// 平台编辑：编码/名称/邮箱/佣金率字段级校验见 EditPlatformValidator；此处只做存在性与落库。
/// </summary>
public class EditPlatformCommandHandler(IPlatformRepository repository)
    : IRequestHandler<EditPlatformCommand, ApiResponse>
{
    /// <summary>处理入口：平台编辑：编码/名称/邮箱/佣金率字段级校验见 EditPlatformValidator；此处只做存在性与落库。</summary>
    public async Task<ApiResponse> Handle(EditPlatformCommand request, CancellationToken cancellationToken)
    {
        var platform = await repository.GetByIdAsync(request.Id);
        if (platform is null || platform.IsDeleted)
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "平台不存在");

        platform.PlatformCode = request.PlatformCode;
        platform.PlatformName = request.PlatformName;
        platform.ContactEmail = request.ContactEmail;
        platform.DefaultCommissionRate = request.DefaultCommissionRate;
        await repository.UpdateAsync(platform);
        return ApiResults.Ok(new { success = true });
    }
}
