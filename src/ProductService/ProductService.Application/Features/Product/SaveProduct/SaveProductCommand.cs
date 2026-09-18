using CommunalService.Domain;
using MediatR;
using ProductService.Application.Features.Product.CreateProduct;

namespace ProductService.Application.Features.Product.SaveProduct;

/// <summary>新建/编辑商品（含 SKU 列表，价格库存校验在 Validator）。</summary>
/// <param name="Id">主键。</param>
/// <param name="CategoryId">分类 ID。</param>
/// <param name="Name">名称。</param>
/// <param name="MainImage">主图。</param>
/// <param name="Description">描述。</param>
public record SaveProductCommand(long Id, long CategoryId, string Name, string MainImage, string Description,
    List<CreateSkuItem> Skus) : IRequest<ApiResponse>;
