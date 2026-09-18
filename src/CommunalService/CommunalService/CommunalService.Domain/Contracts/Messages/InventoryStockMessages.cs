using MessagePack;

namespace CommunalService.Domain.Contracts.Messages;

[MessagePackObject]
public sealed class InventoryStockItem
{
    [Key(0)] public long SkuId { get; set; }
    [Key(1)] public int Quantity { get; set; }
}

[MessagePackObject]
public sealed class InventoryStockRequest
{
    [Key(0)] public string BizNo { get; set; } = string.Empty;
    [Key(1)] public List<InventoryStockItem> Items { get; set; } = [];
}

[MessagePackObject]
public sealed class InventoryStockResponse
{
    [Key(0)] public bool Success { get; set; }
    [Key(1)] public string Message { get; set; } = string.Empty;
}
