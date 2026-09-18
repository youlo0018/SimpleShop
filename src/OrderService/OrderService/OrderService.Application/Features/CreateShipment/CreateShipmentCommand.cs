using CommunalService.Domain;
﻿using MediatR;

namespace OrderService.Application.Features.CreateShipment;

public sealed record ShipmentItemInput
{
    /// <summary>订单明细 ID。</summary>
    public long OrderItemId { get; init; }
    /// <summary>商品 SKU ID。</summary>
    public long SkuId { get; init; }
    /// <summary>数量。</summary>
    public int Quantity { get; init; }
}

public sealed record CreateShipmentCommand : IRequest<ApiResponse>
{
    /// <summary>订单 ID。</summary>
    public long OrderId { get; init; }
    /// <summary>平台 ID。</summary>
    public long PlatformId { get; init; }
    /// <summary>商户 ID。</summary>
    public long MerchantId { get; init; }
    /// <summary>物流公司。</summary>
    public string LogisticsCompany { get; init; } = string.Empty;
    /// <summary>物流单号。</summary>
    public string TrackingNo { get; init; } = string.Empty;
    /// <summary>明细集合。</summary>
    public List<ShipmentItemInput> Items { get; init; } = [];
}
