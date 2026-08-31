using CommunalService.Domain;
using MediatR;

namespace PermissionService.Application.Features.Permission.BindUser;

public record BindUserCommand(long UserId, long RoleId, long PlatformId, long MerchantId) : IRequest<ApiResponse>;
