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