using System.Text.RegularExpressions;
using FluentValidation;

namespace UserService.Application.Features.Address.SaveAddress;

public class SaveAddressValidator : AbstractValidator<SaveAddressCommand>
{
    private static readonly Regex PhoneRegex = new("^1[3-9]\\d{9}$", RegexOptions.Compiled);

    public SaveAddressValidator()
    {
        RuleFor(x => x.ReceiverName).NotEmpty().WithMessage("请填写收货人");
        RuleFor(x => x.Detail).NotEmpty().WithMessage("请填写详细地址");
        RuleFor(x => x.ReceiverPhone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || PhoneRegex.IsMatch(phone))
            .WithMessage("手机号格式不正确");
    }
}
