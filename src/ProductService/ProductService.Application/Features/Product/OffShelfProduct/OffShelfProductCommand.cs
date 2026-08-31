using CommunalService.Domain;
using MediatR;

namespace ProductService.Application.Features.Product.OffShelfProduct;

public record OffShelfProductCommand(long Id) : IRequest<ApiResponse>;
