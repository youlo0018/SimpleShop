using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.User.CreateUser;

public record CreateUserCommand(
    string UserName, string Password, string Email, string Phone, string Role,
    long PlatformId = 0, long MerchantId = 0) : IRequest<ApiResponse>;
