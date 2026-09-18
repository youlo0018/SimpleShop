using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Features.Category.CreateCategory;
using ProductService.Application.Features.Category.DeleteCategory;
using ProductService.Application.Features.Category.DisableCategory;
using ProductService.Application.Features.Category.GetCategoryTree;
using ProductService.Application.Features.Category.UpdateCategory;
using ProductService.Application.Features.Product.CreateProduct;
using ProductService.Application.Features.Product.GetAdminProduct;
using ProductService.Application.Features.Product.GetProductDetail;
using ProductService.Application.Features.Product.ListProducts;
using ProductService.Application.Features.Product.OffShelfProduct;
using ProductService.Application.Features.Product.PublishProduct;
using ProductService.Application.Features.Product.SaveProduct;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Api.Controllers;

/// <summary>
/// 商品/分类入口：控制器只做协议转换；列表与租户过滤、SKU upsert、分类树校验都在 Application/Infrastructure。
/// 文件上传保留在 API 层（IFormFile 流属于协议职责），落库经仓储完成。
/// </summary>
public class ProductController(
    IMediator mediator,
    IProductAdminRepository adminRepository,
    IFreeSql freeSql,
    TenantContext tenant) : BaseController
{
    [HttpGet]
    /// <summary>内部处理：List。</summary>
    public Task<ApiResponse> List([FromQuery] ListProductsQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpGet]
    /// <summary>内部处理：AdminDetail。</summary>
    public Task<ApiResponse> AdminDetail([FromQuery] GetAdminProductQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    /// <summary>写操作：Update（副作用与幂等键见调用方约定）。</summary>
    public Task<ApiResponse> Update([FromBody] SaveProductCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    /// <summary>内部处理：OffShelf。</summary>
    public Task<ApiResponse> OffShelf([FromBody] OffShelfProductCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    /// <summary>写操作：CreateProduct（副作用与幂等键见调用方约定）。</summary>
    public Task<ApiResponse> CreateProduct([FromBody] CreateProductCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    /// <summary>查询数据：GetProductDetail（过滤条件与返回语义见参数与调用方约定）。</summary>
    public async Task<ApiResponse> GetProductDetail([FromQuery] GetProductDetailCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPost]
    /// <summary>辅助处理：PublishProduct。</summary>
    public Task<ApiResponse> PublishProduct([FromBody] PublishProductCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    /// <summary>查询数据：GetCategoryTree（过滤条件与返回语义见参数与调用方约定）。</summary>
    public async Task<ApiResponse> GetCategoryTree([FromQuery] GetCategoryTreeCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPost]
    /// <summary>写操作：CreateCategory（副作用与幂等键见调用方约定）。</summary>
    public Task<ApiResponse> CreateCategory([FromBody] CreateCategoryCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPut("{id}")]
    /// <summary>写操作：UpdateCategory（副作用与幂等键见调用方约定）。</summary>
    public Task<ApiResponse> UpdateCategory([FromRoute] long id, [FromBody] UpdateCategoryCommand command)
        => mediator.Send(command with { Id = id }, CancellationToken.None);

    [HttpPost]
    /// <summary>内部处理：DisableCategory。</summary>
    public Task<ApiResponse> DisableCategory([FromBody] DisableCategoryCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpDelete("{id}")]
    /// <summary>写操作：DeleteCategory（副作用与幂等键见调用方约定）。</summary>
    public Task<ApiResponse> DeleteCategory([FromRoute] long id)
        => mediator.Send(new DeleteCategoryCommand(id), CancellationToken.None);

    // 统一上传已迁移到 FileService（/gateway/files/Upload）；这里只保留历史图片读取，兼容存量数据。
    [HttpGet("{id}")]
    /// <summary>内部处理：File。</summary>
    public async Task<IActionResult> File([FromRoute] long id)
    {
        var uploaded = await freeSql.Select<UploadedFile>().Where(item => item.Id == id && !item.IsDeleted).FirstAsync();
        if (uploaded is null) return NotFound();
        return File(uploaded.Bytes, uploaded.ContentType);
    }
}
