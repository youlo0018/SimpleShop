using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Product.GetAdminProduct;

/// <summary>后台商品详情（含 SKU，按租户范围校验）。</summary>
/// <param name="Id">主键。</param>
public record GetAdminProductQuery(long Id) : IRequest<ApiResponse>;
