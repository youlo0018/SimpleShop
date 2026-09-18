using CommunalService.Domain;
using MediatR;

namespace PaymentService.Application.Features.CreatePayment;

public sealed record CreatePaymentCommand : IRequest<ApiResponse>
{
    /// <summary>业务单号（订单号/退款号，幂等键）。</summary>
    public string BizNo { get; init; } = string.Empty;
    /// <summary>平台 ID。</summary>
    public long PlatformId { get; init; }
    /// <summary>商户 ID。</summary>
    public long MerchantId { get; init; }
    /// <summary>用户 ID（网关登录态注入）。</summary>
    public long UserId { get; init; }
    /// <summary>金额（元）。</summary>
    public decimal Amount { get; init; }
    /// <summary>明细集合。</summary>
    public List<PaymentStockItem> Items { get; init; } = [];
}

public sealed record PaymentStockItem
{
    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; init; }
    /// <summary>数量。</summary>
    public int Quantity { get; init; }
}
