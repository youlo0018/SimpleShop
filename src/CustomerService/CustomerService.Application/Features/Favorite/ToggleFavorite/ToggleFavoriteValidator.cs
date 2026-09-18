using FluentValidation;

namespace CustomerService.Application.Features.Favorite.ToggleFavorite;

/// <summary>收藏切换参数校验：商品 ID 必填；客户 ID 由 Handler 按登录态裁剪。</summary>
public class ToggleFavoriteValidator : AbstractValidator<ToggleFavoriteCommand>
{
    /// <summary>规则覆盖：商品 ID &gt; 0。</summary>
    public ToggleFavoriteValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("商品不能为空");
    }
}
