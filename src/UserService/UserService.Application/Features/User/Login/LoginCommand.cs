using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.User.Login;

public record LoginCommand(string UserName, string Password) : IRequest<ApiResponse>;
