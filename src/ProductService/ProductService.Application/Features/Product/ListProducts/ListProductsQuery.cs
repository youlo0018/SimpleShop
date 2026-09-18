using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Product.ListProducts;

/// <summary>商品列表查询（游客可访问，仅返回上架商品）。</summary>
public record ListProductsQuery(
    string Keyword = "", long CategoryId = 0, long MerchantId = 0,
    int? Status = null, int Page = 1, int PageSize = 10) : IRequest<ApiResponse>;
