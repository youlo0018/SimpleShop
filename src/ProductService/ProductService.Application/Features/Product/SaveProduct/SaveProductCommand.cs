using CommunalService.Domain;
using MediatR;
using ProductService.Application.Features.Product.CreateProduct;

namespace ProductService.Application.Features.Product.SaveProduct;

public record SaveProductCommand(long Id, long CategoryId, string Name, string MainImage, string Description,
    List<CreateSkuItem> Skus) : IRequest<ApiResponse>;
