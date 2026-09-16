using FluentValidation;

namespace OrderService.Application.Features.CreateShipment;

/// <summary>
/// 发货参数校验：物流公司与单号必填（对齐 shipment 表 64 字符列），发货明细不能为空且数量合法。
/// </summary>
public class CreateShipmentValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0).WithMessage("订单不能为空");
        RuleFor(x => x.LogisticsCompany)
            .NotEmpty().WithMessage("物流公司不能为空")
            .MaximumLength(64).WithMessage("物流公司不能超过64个字符");
        RuleFor(x => x.TrackingNo)
            .NotEmpty().WithMessage("物流单号不能为空")
            .MaximumLength(64).WithMessage("物流单号不能超过64个字符");
        RuleFor(x => x.Items).NotEmpty().WithMessage("请选择发货商品");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.OrderItemId).GreaterThan(0).WithMessage("订单明细不能为空");
            item.RuleFor(i => i.SkuId).GreaterThan(0).WithMessage("SKU 不能为空");
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 10000).WithMessage("发货数量必须为1-10000");
        });
    }
}
