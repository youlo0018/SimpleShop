using FluentValidation;

namespace CustomerService.Application.Features.Address.DeleteAddress;

/// <summary>删除地址参数校验：地址 ID 必填；归属校验在 Handler。</summary>
public class DeleteAddressValidator : AbstractValidator<DeleteAddressCommand>
{
    /// <summary>规则覆盖：地址 ID &gt; 0。</summary>
    public DeleteAddressValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("地址不能为空");
    }
}
