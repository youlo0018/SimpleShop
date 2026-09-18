using FluentValidation;

namespace MerchantPlatformService.Application.Features.Merchants.Shop;

/// <summary>店铺查询校验：商户 ID 必填。</summary>
public sealed class GetShopValidator : AbstractValidator<GetShopQuery>
{
    /// <summary>规则覆盖：商户 ID &gt; 0。</summary>
    public GetShopValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("商户不能为空");
    }
}
