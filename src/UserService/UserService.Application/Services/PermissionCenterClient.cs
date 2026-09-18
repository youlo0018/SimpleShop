using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Infrastructure.Consul;
using Grpc.Net.Client;
using MagicOnion.Client;
using UserService.Domain.Entity;

namespace UserService.Application.Services;

/// <summary>
/// 权限中心访问封装：登录解析权限、后台账号绑定角色；UserService 禁止直连权限库。
/// </summary>
public sealed class PermissionCenterClient(IServiceDiscovery serviceDiscovery)
{
    /// <summary>辅助处理：ResolveAsync。</summary>
    public async Task<AuthorizationResponse> ResolveAsync(User user)
    {
        using var channel = GrpcChannel.ForAddress($"http://{await ResolveAddressAsync()}");
        var client = MagicOnionClient.Create<IPermissionService>(channel);
        return await client.ResolveAsync(new AuthorizationRequest
        {
            UserId = user.Id, UserName = user.UserName
        });
    }

    /// <summary>内部处理：AssignRoleAsync。</summary>
    public async Task AssignRoleAsync(long userId, string roleCode, long platformId, long merchantId)
    {
        if (string.IsNullOrWhiteSpace(roleCode) || roleCode is "admin" or "customer") return;
        using var channel = GrpcChannel.ForAddress($"http://{await ResolveAddressAsync()}");
        var client = MagicOnionClient.Create<IPermissionService>(channel);
        if (!await client.AssignRoleAsync(new AssignRoleRequest
            {
                UserId = userId, RoleCode = roleCode, PlatformId = platformId, MerchantId = merchantId
            }))
        {
            throw new InvalidOperationException("角色绑定失败：角色不存在或租户范围不完整");
        }
    }

    /// <summary>辅助处理：ResolveAddressAsync。</summary>
    private async Task<string> ResolveAddressAsync()
        => await serviceDiscovery.GetPollingAddressAsync("PermissionService", PollingAddressType.Grpc)
           ?? throw new InvalidOperationException("PermissionService 不可用");
}
