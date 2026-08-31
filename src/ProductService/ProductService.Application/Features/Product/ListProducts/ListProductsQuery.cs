using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Product.ListProducts;

public record ListProductsQuery(
    string Keyword = "", long CategoryId = 0, long MerchantId = 0,
    int? Status = null, int Page = 1, int PageSize = 10) : IRequest<ApiResponse>;
