using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Features.Category.GetCategoryTree;
using ProductService.Application.Features.Product.CreateProduct;
using ProductService.Application.Features.Product.GetProductDetail;
using ProductService.Application.Features.Product.PublishProduct;

namespace ProductService.Api.Controllers;

public class ProductController(IMediator mediator) : BaseController
{
    [HttpPost]
    public async Task<ApiResponse> CreateProduct([FromBody] CreateProductCommand command)
    {
        var data = await mediator.Send(command);
        return Ok(data);
    }

    [HttpGet]
    public async Task<ApiResponse> GetProductDetail([FromQuery] GetProductDetailCommand command)
    {
        var data = await mediator.Send(command);
        return Ok(data);
    }

    [HttpPost]
    public async Task<ApiResponse> PublishProduct([FromBody] PublishProductCommand command)
    {
        var data = await mediator.Send(command);
        return Ok(data);
    }

    [HttpGet]
    public async Task<ApiResponse> GetCategoryTree([FromQuery] GetCategoryTreeCommand command)
    {
        var data = await mediator.Send(command);
        return Ok(data);
    }
}
