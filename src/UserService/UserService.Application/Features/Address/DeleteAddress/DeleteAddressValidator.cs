using FluentValidation;

namespace UserService.Application.Features.Address.DeleteAddress;

/// <summary>
/// 删除地址参数校验：地址 ID 必填；归属校验在 Handler（地址必须属于当前用户）。
/// </summary>
public class DeleteAddressValidator : AbstractValidator<DeleteAddressCommand>
{
    public DeleteAddressValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("地址不能为空");
    }
}
