using CommunalService.Domain;
using MediatR;

namespace PermissionService.Application.Features.Permission.ListBindings;

public record ListBindingsQuery : IRequest<ApiResponse>;
