using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.Address.ListAddresses;

public record ListAddressesQuery(long UserId = 0) : IRequest<ApiResponse>;
