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
        RuleFor(x => x.IdempotencyKey).MaximumLength(64).WithMessage("幂等键不能超过64个字符");
        RuleFor(x => x.Items).NotEmpty().WithMessage("订单商品不能为空");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.SkuId).GreaterThan(0);
            item.RuleFor(x => x.ProductName).NotEmpty().MaximumLength(40);
            item.RuleFor(x => x.Price).GreaterThan(0);
            item.RuleFor(x => x.Quantity).InclusiveBetween(1, 99);
        });
        // StockItems 允许不传（下单方自行锁库存时为可选），但一旦传入每项必须合法，防止非法数量进入库存锁。
        RuleForEach(x => x.StockItems).ChildRules(stock =>
        {
            stock.RuleFor(s => s.SkuId).GreaterThan(0).WithMessage("库存 SKU 不能为空");
            stock.RuleFor(s => s.Quantity).InclusiveBetween(1, 99).WithMessage("库存数量必须为1-99");
        });
    }
}
