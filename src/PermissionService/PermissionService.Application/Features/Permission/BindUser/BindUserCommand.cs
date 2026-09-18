using CommunalService.Domain;
using MediatR;

namespace PermissionService.Application.Features.Permission.BindUser;

/// <summary>用户绑定角色（一用户一主角色，重绑物理替换）。</summary>
public record BindUserCommand(long UserId, long RoleId, long PlatformId, long MerchantId) : IRequest<ApiResponse>;
