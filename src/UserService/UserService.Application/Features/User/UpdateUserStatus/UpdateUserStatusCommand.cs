using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.User.UpdateUserStatus;

public record UpdateUserStatusCommand(long Id, bool IsEnabled) : IRequest<ApiResponse>;
