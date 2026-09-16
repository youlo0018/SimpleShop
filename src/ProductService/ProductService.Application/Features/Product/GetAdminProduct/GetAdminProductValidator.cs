using FluentValidation;

namespace ProductService.Application.Features.Product.GetAdminProduct;

/// <summary>
/// 后台商品详情参数校验：商品 ID 必填。
/// </summary>
public class GetAdminProductValidator : AbstractValidator<GetAdminProductQuery>
{
    public GetAdminProductValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("商品不能为空");
    }
}
