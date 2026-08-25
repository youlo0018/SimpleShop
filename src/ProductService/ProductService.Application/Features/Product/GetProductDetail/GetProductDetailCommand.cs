using MediatR;

namespace ProductService.Application.Features.Product.GetProductDetail;

public record GetProductDetailCommand : IRequest<object>
{
    public long Id { get; set; }
}
