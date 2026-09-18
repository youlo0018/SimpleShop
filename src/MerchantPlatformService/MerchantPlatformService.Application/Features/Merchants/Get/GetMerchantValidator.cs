using FluentValidation;

namespace MerchantPlatformService.Application.Features.Merchants.Get;

/// <summary>
/// 商户详情参数校验：商户 ID 必填。
/// </summary>
public class GetMerchantValidator : AbstractValidator<GetMerchantQuery>
{
    public GetMerchantValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("商户不能为空");
    }
}
