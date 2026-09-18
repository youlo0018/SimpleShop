using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using CommunalService.Domain.Enums;
using CommunalService.Domain.Infrastructure.Consul;
using Grpc.Net.Client;
using MagicOnion.Client;

namespace OrderService.Infrastructure.ExternalServices;

/// <summary>
/// 营销服务 gRPC 客户端：下单时结算/落账/回退，经 Consul 发现健康实例。
/// 营销服务不可用时下单直接失败（结算失败不能静默按原价成交）。
/// </summary>
public sealed class MarketingClient(IServiceDiscovery serviceDiscovery)
{
    /// <summary>更新：SettleAsync。</summary>
    public async Task<MarketingSettleResponse?> SettleAsync(MarketingSettleRequest request, CancellationToken cancellationToken)
    {
        var address = await serviceDiscovery.GetPollingAddressAsync("MarketingService", PollingAddressType.Grpc);
        if (string.IsNullOrWhiteSpace(address)) return null;
        using var channel = GrpcChannel.ForAddress($"http://{address}");
        var client = MagicOnionClient.Create<IMarketingService>(channel);
        return await client.SettleAsync(request);
    }

    /// <summary>内部处理：CommitAsync。</summary>
    public async Task<bool> CommitAsync(MarketingCommitRequest request, CancellationToken cancellationToken)
    {
        var address = await serviceDiscovery.GetPollingAddressAsync("MarketingService", PollingAddressType.Grpc);
        if (string.IsNullOrWhiteSpace(address)) return false;
        using var channel = GrpcChannel.ForAddress($"http://{address}");
        var client = MagicOnionClient.Create<IMarketingService>(channel);
        var response = await client.CommitAsync(request);
        return response.Success;
    }

    /// <summary>库存操作（幂等键由调用方提供）：ReleaseAsync。</summary>
    public async Task ReleaseAsync(string orderNo, CancellationToken cancellationToken)
    {
        var address = await serviceDiscovery.GetPollingAddressAsync("MarketingService", PollingAddressType.Grpc);
        if (string.IsNullOrWhiteSpace(address)) return;
        using var channel = GrpcChannel.ForAddress($"http://{address}");
        var client = MagicOnionClient.Create<IMarketingService>(channel);
        await client.ReleaseAsync(new MarketingReleaseRequest { OrderNo = orderNo });
    }
}
