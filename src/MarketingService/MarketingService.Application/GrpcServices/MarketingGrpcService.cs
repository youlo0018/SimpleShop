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
    /// <summary>预览：纯计算（不占券不落库），供购物车/提交页展示浮动金额。</summary>
    public async UnaryResult<MarketingPreviewResponse> PreviewAsync(MarketingSettleRequest request)
        => await engine.PreviewAsync(request, CancellationToken.None);

    /// <summary>结算：计算并原子占用所选券，失败整单回退；OrderNo 为占用的关联键。</summary>
    public async UnaryResult<MarketingSettleResponse> SettleAsync(MarketingSettleRequest request)
        => await engine.SettleAsync(request, CancellationToken.None);

    /// <summary>落账：写参与/用券记录，按订单号幂等，重复提交直接成功。</summary>
    public async UnaryResult<MarketingCommitResponse> CommitAsync(MarketingCommitRequest request)
        => await commitService.CommitAsync(request, CancellationToken.None);

    /// <summary>回退：释放该订单占用的券（可重复调用）。</summary>
    public async UnaryResult<MarketingCommitResponse> ReleaseAsync(MarketingReleaseRequest request)
        => await commitService.ReleaseAsync(request.OrderNo, CancellationToken.None);
}
