using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Product.GetAdminProduct;

public record GetAdminProductQuery(long Id) : IRequest<ApiResponse>;
