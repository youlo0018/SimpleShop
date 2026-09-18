using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Product.OffShelfProduct;

/// <summary>商品下架（发布态商品不可售）。</summary>
/// <param name="Id">主键。</param>
public record OffShelfProductCommand(long Id) : IRequest<ApiResponse>;
