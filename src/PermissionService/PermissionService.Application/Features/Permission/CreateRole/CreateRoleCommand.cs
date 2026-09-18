using CommunalService.Domain;
using MediatR;

namespace PermissionService.Application.Features.Permission.CreateRole;

/// <summary>新增角色（平台/商户范围由 TenantType 决定）。</summary>
public record CreateRoleCommand(string Code, string Name, int TenantType, string Description, List<string> Permissions)
    : IRequest<ApiResponse>;
