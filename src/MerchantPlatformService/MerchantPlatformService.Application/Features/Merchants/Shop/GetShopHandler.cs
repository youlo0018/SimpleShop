using MerchantPlatformService.Domain.Enums;
using MerchantPlatformService.Domain.IRepository;
using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.Shop;

/// <summary>
/// 店铺主页公开信息：只返回已审核商户，且只暴露展示字段（不含联系人/佣金等经营数据）。
/// 商城店铺页头部使用；商品列表走 /products/List?merchantId= 单独查询。
/// </summary>
public sealed class GetShopHandler(IMerchantRepository repository) : IRequestHandler<GetShopQuery, object>
{
    /// <summary>查询已入驻店铺的公开字段；不存在或未入驻返回失败标记。</summary>
    public async Task<object> Handle(GetShopQuery request, CancellationToken cancellationToken)
    {
        var merchant = await repository.GetByIdAsync(request.Id);
        if (merchant is null || merchant.IsDeleted || merchant.Status != (int)MerchantStatus.Approved)
            return new { success = false, message = "店铺不存在或未营业" };
        return new { merchant.Id, merchant.MerchantName, merchant.PlatformId, merchant.Status, merchant.CreatedAt };
    }
}
