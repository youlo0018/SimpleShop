using FreeSql;
using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;

namespace OrderService.Infrastructure.Repository;

/// <summary>
/// 发货单仓储：把包裹和包裹明细一起维护，避免出现“有单无明细”的半截数据。
/// </summary>
public class ShipmentRepository(IFreeSql freeSql) : IShipmentRepository
{
    public Task<Shipment?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<Shipment>().Where(shipment => shipment.Id == id).FirstAsync(cancellationToken);

    public Task<List<Shipment>> GetByOrderAsync(long orderId, CancellationToken cancellationToken = default)
        => freeSql.Select<Shipment>().Where(shipment => shipment.OrderId == orderId).ToListAsync(cancellationToken);

    public async Task<bool> AddAsync(Shipment shipment, IReadOnlyCollection<ShipmentItem> items, CancellationToken cancellationToken = default)
    {
        var added = await freeSql.Insert(shipment).ExecuteAffrowsAsync(cancellationToken) > 0;
        if (!added || items.Count == 0)
        {
            return added;
        }

        foreach (var item in items)
        {
            item.ShipmentId = shipment.Id;
        }

        // 包裹明细逐条落库，避免批量插入行为差异导致请求卡住或部分成功难以判断。
        var affectedRows = 0;
        foreach (var item in items)
        {
            affectedRows += await freeSql.Insert(item).ExecuteAffrowsAsync(cancellationToken) > 0 ? 1 : 0;
        }

        return affectedRows == items.Count;
    }

    public async Task<bool> UpdateAsync(Shipment shipment, CancellationToken cancellationToken = default)
        => await freeSql.Update<Shipment>().SetSource(shipment).ExecuteAffrowsAsync(cancellationToken) > 0;

    public Task<List<ShipmentItem>> GetItemsAsync(long shipmentId, CancellationToken cancellationToken = default)
        => freeSql.Select<ShipmentItem>().Where(item => item.ShipmentId == shipmentId).ToListAsync(cancellationToken);
}
