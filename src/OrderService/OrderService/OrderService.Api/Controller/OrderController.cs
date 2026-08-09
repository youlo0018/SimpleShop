using Microsoft.AspNetCore.Mvc;
using OrderService.Domin.Entity;
using OrderService.Domin.IRepository;
using YukeTools;

namespace OrderService.Api.Controller;


public class OrderController(IFreeSql freeSql,IOrderRepository orderRepository) : BaseController
{
    [HttpGet]
    public IActionResult Index()
    {
        freeSql.CodeFirst.SyncStructure<Order>();
        freeSql.Insert<Order>(new Order()
        {
            Id = 1,
            OrderNo =  Guid.NewGuid().ToString("N"),
            CustomerNo = Guid.NewGuid().ToString("N"),
            
            
        }).ExecuteAffrows();
        return Ok();
    }
    [HttpGet]
    public IActionResult Query(long id)
    {
        var order=orderRepository.GetById(id);
       //var order = orderRepository.GetById<TestOrder>(1);
        return Ok(order.ToJson());
    }

}