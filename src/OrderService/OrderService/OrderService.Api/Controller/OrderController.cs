using Microsoft.AspNetCore.Mvc;
using OrderService.Domin.Entity;
using OrderService.Domin.IRepository;

namespace OrderService.Api.Controller;


public class OrderController(IFreeSql freeSql,IOrderRepository orderRepository) : BaseController
{
    [HttpGet]
    public IActionResult Index()
    {
        freeSql.CodeFirst.SyncStructure<Order>();
        freeSql.Insert<Order>(new Order()
        {
            Id = long.MaxValue,
            OrderNo =  Guid.NewGuid().ToString("N"),
            CustomerNo = Guid.NewGuid().ToString("N"),
            
        });
        return Ok();
    }
}