using OrderService.Domain.Entity;

namespace OrderService.Domain.IRepository;

public interface IOrderRepository
{
    public Task<Order> QueryByIdAsync(long id);
    public Order GetById(long id);
    public T GetById<T>(long id);
}