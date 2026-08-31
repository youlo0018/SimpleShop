using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Product.PublishProduct;

public record PublishProductCommand : IRequest<ApiResponse>
{
    public long Id { get; set; }
    public long MerchantId { get; set; }
    public bool Approved { get; set; } = true;
}
