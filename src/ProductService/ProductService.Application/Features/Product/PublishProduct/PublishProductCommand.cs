using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Product.PublishProduct;

public record PublishProductCommand : IRequest<ApiResponse>
{
    /// <summary>主键（雪花 ID）。</summary>
    public long Id { get; set; }
    /// <summary>商户 ID。</summary>
    public long MerchantId { get; set; }
    /// <summary>审核是否通过。</summary>
    public bool Approved { get; set; } = true;
}
