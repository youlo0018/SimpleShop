using CommunalService.Domain;
using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using CommunalService.Domain.Infrastructure.Consul;
using Consul;
using Grpc.Net.Client;
using MagicOnion.Client;
using Microsoft.AspNetCore.Mvc;
using Mysqlx;
using OrderService.Domain.Entity;
using OrderService.Domain.IRepository;
using YukeTools;




namespace OrderService.Api.Controller;


public class OrderController(IFreeSql freeSql,IOrderRepository orderRepository,IServiceDiscovery consul) : BaseController
{
    [HttpGet]
    public async Task<ApiResponse> Index()
    {
        
        var address = await consul.GetPollingAddressAsync("CustomerService");
        var channel = GrpcChannel.ForAddress($"http://{address.IP}:5001"); // 实际通过 Consul 获取地址
        // 2. 使用 MagicOnionClient 创建客户端代理
        var client = MagicOnionClient.Create<ICustomerService>(channel);

// 3. 调用服务方法
        var result = await client.GetCustomerAsync(new GetCustomerRequest() { Id = 13665058326389765 });
        Console.WriteLine($"Result: {result}"); //
        return Ok(result);
    }
    [HttpGet]
    public ApiResponse Query(long id)
    {
        var order=orderRepository.GetById(id);
       //var order = orderRepository.GetById<TestOrder>(1);
        return Ok(order.ToJson());
    }

}