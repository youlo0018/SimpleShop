using FluentValidation;

namespace OrderService.Application.Features.GetOrderDetail;

/// <summary>
/// 后台订单详情参数校验：订单 ID 必填；租户字段由控制器按登录态回填。
/// </summary>
public class GetOrderDetailValidator : AbstractValidator<GetOrderDetailQuery>
{
    public GetOrderDetailValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("订单不能为空");
    }
}
