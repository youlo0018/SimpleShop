using MediatR;

namespace ProductService.Application.Features.Product.GetProductDetail;

public record GetProductDetailCommand : IRequest<object>
{
    /// <summary>主键（雪花 ID）。</summary>
    public long Id { get; set; }
}
