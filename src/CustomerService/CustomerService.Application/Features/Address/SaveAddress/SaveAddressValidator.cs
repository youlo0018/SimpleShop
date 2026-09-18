using System.Text.RegularExpressions;
using FluentValidation;

namespace CustomerService.Application.Features.Address.SaveAddress;

/// <summary>地址参数校验：省市区/详细地址长度对齐 customer_address 列宽；手机号可选但填了必须合法。</summary>
public class SaveAddressValidator : AbstractValidator<SaveAddressCommand>
{
    /// <summary>手机号正则（与实体列宽/校验规则对齐）。</summary>
    private static readonly Regex PhoneRegex = new("^1[3-9]\\d{9}$", RegexOptions.Compiled);

    /// <summary>规则覆盖：收货人/详细地址必填、手机号格式、省市区长度。</summary>
    public SaveAddressValidator()
    {
        RuleFor(x => x.ReceiverName)
            .NotEmpty().WithMessage("请填写收货人")
            .MaximumLength(32).WithMessage("收货人不能超过32个字符");
        RuleFor(x => x.Detail)
            .NotEmpty().WithMessage("请填写详细地址")
            .MaximumLength(255).WithMessage("详细地址不能超过255个字符");
        RuleFor(x => x.ReceiverPhone)
            .Must(phone => string.IsNullOrWhiteSpace(phone) || PhoneRegex.IsMatch(phone))
            .WithMessage("手机号格式不正确");
        RuleFor(x => x.Province).MaximumLength(64).WithMessage("省份不能超过64个字符");
        RuleFor(x => x.City).MaximumLength(64).WithMessage("城市不能超过64个字符");
        RuleFor(x => x.District).MaximumLength(64).WithMessage("区县不能超过64个字符");
    }
}
