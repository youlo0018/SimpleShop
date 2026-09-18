using FluentValidation;

namespace PaymentService.Application.Features.Payment.ListPayments;

/// <summary>
/// 支付单分页参数校验：页码与页大小限制范围，防止一次拉取过大结果集。
/// </summary>
public class ListPaymentsValidator : AbstractValidator<ListPaymentsQuery>
{
    public ListPaymentsValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("页码不能小于1");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("每页数量必须为1-100");
        RuleFor(x => x.Keyword).MaximumLength(64).WithMessage("关键词不能超过64个字符");
    }
}
