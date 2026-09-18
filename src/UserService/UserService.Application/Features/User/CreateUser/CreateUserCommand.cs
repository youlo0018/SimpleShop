using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.User.CreateUser;

/// <summary>后台建号：账号字段 + 角色与租户（真实角色绑定在权限中心）。</summary>
public record CreateUserCommand(
    string UserName, string Password, string Email, string Phone, string Role,
    long PlatformId = 0, long MerchantId = 0) : IRequest<ApiResponse>;
