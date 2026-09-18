using CommunalService.Domain;
﻿using MediatR;

namespace PaymentService.Application.Features.RefundPayment;

public sealed record RefundPaymentCommand : IRequest<ApiResponse>
{
    /// <summary>业务单号（订单号/退款号，幂等键）。</summary>
    public string BizNo { get; init; } = string.Empty;
    /// <summary>金额（元）。</summary>
    public decimal Amount { get; init; }
    /// <summary>原因/说明。</summary>
    public string Reason { get; init; } = string.Empty;
    /// <summary>明细集合。</summary>
    public List<RefundStockItem> Items { get; init; } = [];
}

public sealed record RefundStockItem
{
    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; init; }
    /// <summary>数量。</summary>
    public int Quantity { get; init; }
}
