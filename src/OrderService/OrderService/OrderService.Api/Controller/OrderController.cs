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
        return Ok();
    }
}