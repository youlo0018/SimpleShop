using FluentValidation;

namespace OrderService.Application.Features.ReceiveShipment;

/// <summary>
/// 确认收货参数校验：发货单 ID 必填；CustomerId/OverrideOwnerCheck 由控制器按登录态强制回填。
/// </summary>
public class ReceiveShipmentValidator : AbstractValidator<ReceiveShipmentCommand>
{
    public ReceiveShipmentValidator()
    {
        RuleFor(x => x.ShipmentId).GreaterThan(0).WithMessage("发货单不能为空");
    }
}
