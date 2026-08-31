using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.User.UpdateUser;

public record UpdateUserCommand(
    long Id, string UserName, string Email, string Phone, string Role,
    long PlatformId = 0, long MerchantId = 0, string? Password = null) : IRequest<ApiResponse>;
