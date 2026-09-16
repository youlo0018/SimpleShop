using CommunalService.Domain.Contracts.Messages;
using MagicOnion;

namespace CommunalService.Domain.Contracts.Services;

/// <summary>
/// 营销服务内部契约（MagicOnion gRPC）：
/// Preview=结算预览（无副作用，购物车/提交页用）；Settle=占用券并返回逐商品优惠（下单用）；
/// Commit=落参与/用券记录（订单落库后调用）；Release=订单失败/取消时回退券。
/// </summary>
public interface IMarketingService : IService<IMarketingService>
{
    UnaryResult<MarketingPreviewResponse> PreviewAsync(MarketingSettleRequest request);

    UnaryResult<MarketingSettleResponse> SettleAsync(MarketingSettleRequest request);

    UnaryResult<MarketingCommitResponse> CommitAsync(MarketingCommitRequest request);

    UnaryResult<MarketingCommitResponse> ReleaseAsync(MarketingReleaseRequest request);
}
