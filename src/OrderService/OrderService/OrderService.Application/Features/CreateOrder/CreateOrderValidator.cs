using FluentValidation;

namespace OrderService.Application.Features.CreateOrder;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("客户ID不能为空");
        RuleFor(x => x.CustomerNo).NotEmpty().MaximumLength(128);
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ReceiverName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ReceiverPhone).NotEmpty().Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");
        RuleFor(x => x.ReceiverAddress).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Items).NotEmpty().WithMessage("订单商品不能为空");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.SkuId).GreaterThan(0);
            item.RuleFor(x => x.ProductName).NotEmpty().MaximumLength(40);
            item.RuleFor(x => x.Price).GreaterThan(0);
            item.RuleFor(x => x.Quantity).InclusiveBetween(1, 99);
        });
    }
}
