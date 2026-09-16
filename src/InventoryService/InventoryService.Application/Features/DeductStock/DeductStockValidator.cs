using FluentValidation;

namespace InventoryService.Application.Features.DeductStock;

/// <summary>
/// 扣减库存参数校验：BizNo 是流水幂等键，不能为空；空明细会直接"成功"返回，必须拦截。
/// </summary>
public class DeductStockValidator : AbstractValidator<DeductStockCommand>
{
    public DeductStockValidator()
    {
        RuleFor(x => x.BizNo)
            .NotEmpty().WithMessage("业务单号不能为空")
            .MaximumLength(64).WithMessage("业务单号不能超过64个字符");
        RuleFor(x => x.Items).NotEmpty().WithMessage("库存明细不能为空");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.SkuId).GreaterThan(0).WithMessage("SKU 不能为空");
            item.RuleFor(i => i.Quantity).InclusiveBetween(1, 10000).WithMessage("扣减数量必须为1-10000");
        });
    }
}
