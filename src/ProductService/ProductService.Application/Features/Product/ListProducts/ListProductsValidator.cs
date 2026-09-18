using FluentValidation;

namespace ProductService.Application.Features.Product.ListProducts;

/// <summary>
/// 商品分页参数校验：页码与页大小限制范围，关键词长度对齐可检索字段。
/// </summary>
public class ListProductsValidator : AbstractValidator<ListProductsQuery>
{
    public ListProductsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("页码不能小于1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每页数量必须为1-100");
        RuleFor(x => x.Keyword).MaximumLength(64).WithMessage("关键词不能超过64个字符");
    }
}
