using MediatR;

namespace ProductService.Application.Features.Product.PublishProduct;

public record PublishProductCommand : IRequest<object>
{
    public long Id { get; set; }
    public long MerchantId { get; set; }
    public bool Approved { get; set; } = true;
}
