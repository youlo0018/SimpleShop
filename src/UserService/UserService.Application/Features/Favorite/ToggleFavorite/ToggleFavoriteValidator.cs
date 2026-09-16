using FluentValidation;

namespace UserService.Application.Features.Favorite.ToggleFavorite;

/// <summary>
/// 收藏切换参数校验：商品 ID 必填；UserId 由 Handler 按登录态裁剪（客户强制本人），不在此校验。
/// </summary>
public class ToggleFavoriteValidator : AbstractValidator<ToggleFavoriteCommand>
{
    public ToggleFavoriteValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("商品不能为空");
    }
}
