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
    /// <summary>
    /// 结算预览：纯计算、不落库不占券，返回逐商品优惠与可用券/活动列表；失败时 Success=false。
    /// </summary>
    /// <param name="request">结算请求；OrderNo 可空，UserId 用于加载可用券。</param>
    UnaryResult<MarketingPreviewResponse> PreviewAsync(MarketingSettleRequest request);

    /// <summary>
    /// 下单结算：计算优惠并**原子占用**所选券（受 SelectedUserCouponIds 控制勾选范围），
    /// OrderNo 必填；任一券占用失败会整单回退并返回失败。不写记录，记录由 CommitAsync 完成。
    /// </summary>
    /// <param name="request">结算请求；OrderNo 必填，Items 至少一行。</param>
    UnaryResult<MarketingSettleResponse> SettleAsync(MarketingSettleRequest request);

    /// <summary>
    /// 落账：订单落库后提交结算结果，写活动参与记录与用券记录（按订单号幂等，重复提交不重复写）。
    /// </summary>
    /// <param name="request">包含 OrderId/OrderNo/UserId/PlatformId 与完整结算结果。</param>
    UnaryResult<MarketingCommitResponse> CommitAsync(MarketingCommitRequest request);

    /// <summary>
    /// 回退：释放该订单占用的用户券（订单创建失败、主动取消、支付超时关单时调用；可重复调用）。
    /// </summary>
    /// <param name="request">包含订单号。</param>
    UnaryResult<MarketingCommitResponse> ReleaseAsync(MarketingReleaseRequest request);
}
