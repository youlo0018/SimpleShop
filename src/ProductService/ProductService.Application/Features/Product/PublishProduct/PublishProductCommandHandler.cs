using MediatR;
using EntityProduct = ProductService.Domain.Entity.Product;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Product.PublishProduct;

public class PublishProductCommandHandler(
    IProductRepository<EntityProduct> repository,
    ISkuRepository<Sku> skuRepository)
    : IRequestHandler<PublishProductCommand, object>
{
    public async Task<object> Handle(PublishProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id);
        if (product == null)
            return "商品不存在";

        if (product.MerchantId != request.MerchantId)
            return "无权操作该商品";

        if (!request.Approved)
        {
            product.ReviewStatus = 2;
            product.Status = 2;
            await repository.UpdateAsync(product);
            return new { success = true, reviewStatus = product.ReviewStatus, status = product.Status };
        }

        // 审核与上架一次完成：业务员点击“审核上架”后商品必须立刻进入可售状态。
        product.ReviewStatus = 1;
        var skus = await skuRepository.QueryAsync(s => s.ProductId == request.Id && s.IsActive && s.Stock > 0);
        if (skus.Count == 0)
            return "商品没有可售SKU，无法上架";

        product.Status = 1;
        await repository.UpdateAsync(product);
        return new { success = true, reviewStatus = product.ReviewStatus, status = product.Status };
    }
}
