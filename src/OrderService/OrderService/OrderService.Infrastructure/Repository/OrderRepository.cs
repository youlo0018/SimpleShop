using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;

namespace OrderService.Infrastructure.Repository;

public class OrderRepository(IFreeSql freeSql) : IOrderRepository
{

    /// <summary>
    /// 根据主键查询订单表
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Order> QueryByIdAsync(long id)
    {
        return await freeSql.Select<Order>().Where(o => o.Id == id).FirstAsync();
    }
    /// <summary>
    /// 根据主键查询订单表
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>

    public Order GetById(long id)
    {
        return  freeSql.Select<Order>().Where(o => o.Id == id).First();
    }
    /// <summary>
    /// 查询指定字段
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetById<T>(long id)
    {
        return  freeSql.Select<Order>().Where(o => o.Id == id).ToOne<T>();
    }
}