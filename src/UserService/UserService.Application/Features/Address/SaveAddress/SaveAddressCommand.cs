using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.Address.SaveAddress;

public record SaveAddressCommand(long Id, long UserId, string ReceiverName, string ReceiverPhone,
    string Province, string City, string District, string Detail, bool IsDefault) : IRequest<ApiResponse>;
