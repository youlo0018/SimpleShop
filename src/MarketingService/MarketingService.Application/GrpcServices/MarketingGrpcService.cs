using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using MagicOnion;
using MagicOnion.Server;
using MarketingService.Application.Services;

namespace MarketingService.Application.GrpcServices;

/// <summary>
/// 营销服务 gRPC：OrderService 下单时调用。Preview 无副作用；Settle 占用券；
/// Commit 落记录（订单落库后）；Release 回退券（下单失败/取消）。
/// </summary>
public sealed class MarketingGrpcService(DiscountEngine engine, MarketingCommitService commitService)
    : ServiceBase<IMarketingService>, IMarketingService
{
    public async UnaryResult<MarketingPreviewResponse> PreviewAsync(MarketingSettleRequest request)
        => await engine.PreviewAsync(request, CancellationToken.None);

    public async UnaryResult<MarketingSettleResponse> SettleAsync(MarketingSettleRequest request)
        => await engine.SettleAsync(request, CancellationToken.None);

    public async UnaryResult<MarketingCommitResponse> CommitAsync(MarketingCommitRequest request)
        => await commitService.CommitAsync(request, CancellationToken.None);

    public async UnaryResult<MarketingCommitResponse> ReleaseAsync(MarketingReleaseRequest request)
        => await commitService.ReleaseAsync(request.OrderNo, CancellationToken.None);
}
