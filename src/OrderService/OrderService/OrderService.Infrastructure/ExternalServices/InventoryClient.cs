using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Infrastructure.Consul;
using Grpc.Net.Client;
using MagicOnion.Client;
using OrderService.Domain.IRepository;

namespace OrderService.Infrastructure.ExternalServices;

/// <summary>
/// 库存服务 gRPC 客户端：通过 Consul 发现健康实例，避免内网 HTTP 依赖和硬编码端口。
/// </summary>
public sealed class InventoryClient(IServiceDiscovery serviceDiscovery)
{
    public async Task<bool> LockAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken)
        => await SendAsync("lock", orderNo, items, cancellationToken);

    public async Task<bool> ReleaseAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken)
        => await SendAsync("release", orderNo, items, cancellationToken);

    private async Task<bool> SendAsync(string action, string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken)
    {
        var address = await serviceDiscovery.GetPollingAddressAsync("InventoryService", PollingAddressType.Grpc);
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        using var channel = GrpcChannel.ForAddress($"http://{address}");
        var client = MagicOnionClient.Create<IInventoryStockService>(channel);
        var request = new InventoryStockRequest
        {
            BizNo = orderNo,
            Items = items.Select(item => new InventoryStockItem { SkuId = item.SkuId, Quantity = item.Quantity }).ToList()
        };

        var response = action switch
        {
            "lock" => await client.LockAsync(request),
            "release" => await client.ReleaseAsync(request),
            _ => throw new NotSupportedException($"不支持的库存操作：{action}")
        };
        return response.Success;
    }
}
