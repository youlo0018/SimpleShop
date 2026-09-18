using FluentValidation;

namespace MerchantPlatformService.Application.Features.Platforms.Get;

/// <summary>
/// 平台详情参数校验：平台 ID 必填。
/// </summary>
public class GetPlatformValidator : AbstractValidator<GetPlatformQuery>
{
    public GetPlatformValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("平台不能为空");
    }
}
