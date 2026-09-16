using FluentValidation;

namespace UserService.Application.Features.User.ListUsers;

/// <summary>
/// 用户分页参数校验：页码与页大小限制范围；Role 只允许空/customer/admin（与 user.role 两值对应）。
/// </summary>
public class ListUsersValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("页码不能小于1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每页数量必须为1-100");
        RuleFor(x => x.Keyword).MaximumLength(64).WithMessage("关键词不能超过64个字符");
        RuleFor(x => x.Role)
            .Must(role => string.IsNullOrWhiteSpace(role) || role is "customer" or "admin")
            .WithMessage("角色筛选值不正确");
    }
}
