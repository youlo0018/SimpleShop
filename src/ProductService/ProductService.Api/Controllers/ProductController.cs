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
    public Task<ApiResponse> List([FromQuery] ListProductsQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> AdminDetail([FromQuery] GetAdminProductQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> Update([FromBody] SaveProductCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> OffShelf([FromBody] OffShelfProductCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> CreateProduct([FromBody] CreateProductCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    public async Task<ApiResponse> GetProductDetail([FromQuery] GetProductDetailCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPost]
    public Task<ApiResponse> PublishProduct([FromBody] PublishProductCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpGet]
    public async Task<ApiResponse> GetCategoryTree([FromQuery] GetCategoryTreeCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPost]
    public Task<ApiResponse> CreateCategory([FromBody] CreateCategoryCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPut("{id}")]
    public Task<ApiResponse> UpdateCategory([FromRoute] long id, [FromBody] UpdateCategoryCommand command)
        => mediator.Send(command with { Id = id }, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> DisableCategory([FromBody] DisableCategoryCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpDelete("{id}")]
    public Task<ApiResponse> DeleteCategory([FromRoute] long id)
        => mediator.Send(new DeleteCategoryCommand(id), CancellationToken.None);

    // 图片上传属 Api 层协议职责例外：仅接受白名单扩展名，且以文件头魔数判定真实类型，防止伪造 Content-Type/扩展名。
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    [HttpPost]
    public async Task<ApiResponse> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0) return Error(BaseApiResponseCode.BadRequest, "请选择文件");
        if (file.Length > 2 * 1024 * 1024) return Error(BaseApiResponseCode.BadRequest, "图片不能超过2MB");
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Error(BaseApiResponseCode.BadRequest, "仅支持上传图片");

        var fileName = Path.GetFileName(file.FileName ?? string.Empty);
        if (!AllowedImageExtensions.Contains(Path.GetExtension(fileName)))
            return Error(BaseApiResponseCode.BadRequest, "仅支持 jpg/jpeg/png/gif/webp 图片");
        if (fileName.Length > 255)
            return Error(BaseApiResponseCode.BadRequest, "文件名不能超过255个字符");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var bytes = stream.ToArray();
        var contentType = DetectImageContentType(bytes);
        if (contentType is null)
            return Error(BaseApiResponseCode.BadRequest, "图片内容无效");

        var uploaded = new UploadedFile { ContentType = contentType, FileName = fileName, Bytes = bytes };
        await freeSql.Insert(uploaded).ExecuteAffrowsAsync();
        return Ok(new { url = $"/gateway/products/File/{uploaded.Id}" });
    }

    private static string? DetectImageContentType(byte[] bytes)
    {
        if (bytes.Length < 12) return null;
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return "image/jpeg";
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return "image/png";
        if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38) return "image/gif";
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) return "image/webp";
        return null;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> File([FromRoute] long id)
    {
        var uploaded = await freeSql.Select<UploadedFile>().Where(item => item.Id == id && !item.IsDeleted).FirstAsync();
        if (uploaded is null) return NotFound();
        return File(uploaded.Bytes, uploaded.ContentType);
    }
}
