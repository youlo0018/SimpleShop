using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.User.Register;

public record RegisterCommand(string UserName, string Password, string Email, string Phone, string Role = "customer")
    : IRequest<ApiResponse>;
