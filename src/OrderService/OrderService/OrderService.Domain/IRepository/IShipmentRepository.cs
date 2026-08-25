using OrderService.Domain.Entity;

namespace OrderService.Domain.IRepository;

public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<List<Shipment>> GetByOrderAsync(long orderId, CancellationToken cancellationToken = default);
    Task<bool> AddAsync(Shipment shipment, IReadOnlyCollection<ShipmentItem> items, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Shipment shipment, CancellationToken cancellationToken = default);
    Task<List<ShipmentItem>> GetItemsAsync(long shipmentId, CancellationToken cancellationToken = default);
}
