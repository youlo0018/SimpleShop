using FluentValidation;

namespace MerchantPlatformService.Application.Features.Merchants.List;

/// <summary>商户列表分页校验：页码≥1，每页 1-100（避免一次拉取过大结果集）。</summary>
public sealed class ListMerchantsValidator : AbstractValidator<ListMerchantsQuery>
{
    /// <summary>规则覆盖：页码与每页条数范围。</summary>
    public ListMerchantsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("分页参数不正确（页码≥1，每页1-100）");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("分页参数不正确（页码≥1，每页1-100）");
    }
}
