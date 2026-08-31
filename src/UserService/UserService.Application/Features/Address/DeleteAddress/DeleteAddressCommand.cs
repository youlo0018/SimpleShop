using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.Address.DeleteAddress;

public record DeleteAddressCommand(long Id) : IRequest<ApiResponse>;
