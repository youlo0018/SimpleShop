using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Features.Category.GetCategoryTree;
using ProductService.Application.Features.Product.CreateProduct;
using ProductService.Application.Features.Product.GetProductDetail;
using ProductService.Application.Features.Product.PublishProduct;
using FreeSql;
using ProductService.Domain.Entity;

namespace ProductService.Api.Controllers;

public sealed record ProductListQuery(
    string Keyword = "", long CategoryId = 0, long MerchantId = 0,
    int? Status = null, int Page = 1, int PageSize = 10);
public sealed record CreateCategoryRequest(string Name, long ParentId = 0, int Sort = 0);
public sealed record SaveProductRequest(long Id, long CategoryId, string Name, string MainImage, string Description,
    List<CreateSkuItem> Skus);

public class ProductController(IMediator mediator, IFreeSql freeSql, TenantContext tenant) : BaseController
{
    [HttpGet]
    public async Task<ApiResponse> List([FromQuery] ProductListQuery query)
    {
        var selection = freeSql.Select<Product>()
            .Where(product => !product.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Keyword), product => product.Name.Contains(query.Keyword))
            .WhereIf(query.CategoryId > 0, product => product.CategoryId == query.CategoryId)
            .WhereIf(query.MerchantId > 0, product => product.MerchantId == query.MerchantId)
            .WhereIf(tenant.IsPlatform, product => product.PlatformId == tenant.PlatformId)
            .WhereIf(tenant.IsMerchant, product => product.MerchantId == tenant.MerchantId)
            .WhereIf(query.Status.HasValue, product => product.Status == query.Status!.Value);

        var total = await selection.CountAsync();
        var products = await selection
            .OrderByDescending(product => product.CreatedAt)
            .Page((Math.Max(query.Page, 1) - 1) * query.PageSize, query.PageSize)
            .ToListAsync();
        var ids = products.Select(product => product.Id).ToArray();
        var skus = await freeSql.Select<Sku>().Where(sku => ids.Contains(sku.ProductId)).ToListAsync();

        return Ok(new
        {
            items = products.Select(product => new
            {
                product.Id,
                product.Name,
                product.MainImage,
                product.Description,
                product.CategoryId,
                product.MerchantId,
                product.Status,
                product.ReviewStatus,
                skus = skus.Where(sku => sku.ProductId == product.Id)
            }),
            total,
            page = query.Page,
            pageSize = query.PageSize
        });
    }

    [HttpGet]
    public async Task<ApiResponse> AdminDetail([FromQuery] long id)
    {
        var product = await freeSql.Select<Product>()
            .Where(item => item.Id == id && !item.IsDeleted)
            .WhereIf(tenant.IsPlatform, item => item.PlatformId == tenant.PlatformId)
            .WhereIf(tenant.IsMerchant, item => item.MerchantId == tenant.MerchantId)
            .FirstAsync();
        if (product is null) return Error(BaseApiResponseCode.NotFound, "商品不存在");
        return Ok(new { product, skus = await freeSql.Select<Sku>().Where(sku => sku.ProductId == id).ToListAsync() });
    }

    [HttpPost]
    public async Task<ApiResponse> Update([FromBody] SaveProductRequest request)
    {
        var product = await freeSql.Select<Product>().Where(item => item.Id == request.Id && !item.IsDeleted)
            .WhereIf(tenant.IsPlatform, item => item.PlatformId == tenant.PlatformId)
            .WhereIf(tenant.IsMerchant, item => item.MerchantId == tenant.MerchantId)
            .FirstAsync();
        if (product is null) return Error(BaseApiResponseCode.NotFound, "商品不存在");

        product.Name = request.Name;
        product.MainImage = request.MainImage;
        product.Description = request.Description;
        product.CategoryId = request.CategoryId;
        await freeSql.Update<Product>().SetSource(product).ExecuteAffrowsAsync();

        foreach (var sku in request.Skus)
        {
            if (sku.Stock <= 0 || sku.Price <= 0) continue;
            var existing = await freeSql.Select<Sku>().Where(item => item.Id > 0 && item.SkuCode == sku.SkuCode).FirstAsync();
            if (existing is null)
            {
                await freeSql.Insert(new Sku
                {
                    PlatformId = product.PlatformId,
                    MerchantId = product.MerchantId,
                    ProductId = product.Id,
                    SkuCode = sku.SkuCode,
                    Price = sku.Price,
                    OriginalPrice = sku.OriginalPrice,
                    Stock = sku.Stock,
                    Image = sku.Image ?? product.MainImage,
                    SpecName = sku.SpecName ?? string.Empty,
                    SpecValue = sku.SpecValue ?? string.Empty
                }).ExecuteAffrowsAsync();
                continue;
            }

            existing.ProductId = product.Id;
            existing.PlatformId = product.PlatformId;
            existing.MerchantId = product.MerchantId;
            existing.Price = sku.Price;
            existing.OriginalPrice = sku.OriginalPrice;
            existing.Stock = sku.Stock;
            existing.Image = string.IsNullOrWhiteSpace(sku.Image) ? product.MainImage : sku.Image;
            existing.SpecName = sku.SpecName ?? string.Empty;
            existing.SpecValue = sku.SpecValue ?? string.Empty;
            await freeSql.Update<Sku>().SetSource(existing).ExecuteAffrowsAsync();
        }

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<ApiResponse> OffShelf([FromBody] PublishProductCommand command)
    {
        var product = await freeSql.Select<Product>().Where(item => item.Id == command.Id && !item.IsDeleted)
            .WhereIf(tenant.IsPlatform, item => item.PlatformId == tenant.PlatformId)
            .WhereIf(tenant.IsMerchant, item => item.MerchantId == tenant.MerchantId)
            .FirstAsync();
        if (product is null) return Error(BaseApiResponseCode.Forbidden, "无权操作该商品");

        product.Status = 2;
        await freeSql.Update<Product>().SetSource(product).ExecuteAffrowsAsync();
        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<ApiResponse> CreateProduct([FromBody] CreateProductCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) return Error(BaseApiResponseCode.BadRequest, "商品名称必填");
        if (string.IsNullOrWhiteSpace(command.MainImage)) return Error(BaseApiResponseCode.BadRequest, "商品主图必填");
        if (command.Skus is null || command.Skus.Count == 0) return Error(BaseApiResponseCode.BadRequest, "至少配置一个SKU");
        if (command.Skus.Any(sku => sku.Price <= 0)) return Error(BaseApiResponseCode.BadRequest, "SKU售价必须大于0");
        if (command.Skus.Any(sku => sku.Stock <= 0)) return Error(BaseApiResponseCode.BadRequest, "SKU库存必须大于0");
        if (command.Skus.Any(sku => string.IsNullOrWhiteSpace(sku.SkuCode))) return Error(BaseApiResponseCode.BadRequest, "SKU编码必填");
        if (command.Skus.GroupBy(sku => sku.SkuCode.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            return Error(BaseApiResponseCode.BadRequest, "SKU编码不能重复");

        command.PlatformId = tenant.IsPlatform ? tenant.PlatformId : command.PlatformId;
        command.MerchantId = tenant.IsMerchant ? tenant.MerchantId : command.MerchantId;
        if (command.MerchantId <= 0) return Error(BaseApiResponseCode.BadRequest, "商户必选");
        if (command.PlatformId <= 0) return Error(BaseApiResponseCode.BadRequest, "平台归属缺失");
        var category = await freeSql.Select<Category>().Where(item => item.Id == command.CategoryId && item.IsActive && !item.IsDeleted).FirstAsync();
        if (category is null) return Error(BaseApiResponseCode.BadRequest, "分类不存在或已停用");
        if (await freeSql.Select<Sku>().AnyAsync(item => command.Skus.Select(sku => sku.SkuCode).Contains(item.SkuCode)))
            return Error(BaseApiResponseCode.BadRequest, "SKU编码已存在");
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
        var selection = freeSql.Select<Product>().Where(product => product.Id == command.Id && !product.IsDeleted)
            .WhereIf(tenant.IsPlatform, product => product.PlatformId == tenant.PlatformId)
            .WhereIf(tenant.IsMerchant, product => product.MerchantId == tenant.MerchantId);
        var product = await selection.FirstAsync();
        if (product is null) return Error(BaseApiResponseCode.Forbidden, "无权操作该商品");
        command.MerchantId = product.MerchantId;
        var data = await mediator.Send(command);
        return Ok(data);
    }

    [HttpGet]
    public async Task<ApiResponse> GetCategoryTree([FromQuery] GetCategoryTreeCommand command)
    {
        var data = await mediator.Send(command);
        return Ok(data);
    }

    [HttpPost]
    public async Task<ApiResponse> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Error(BaseApiResponseCode.BadRequest, "分类名称不能为空");

        long parentId = request.ParentId;
        int depth = 0;
        while (parentId > 0)
        {
            depth++;
            var parent = await freeSql.Select<Category>().Where(item => item.Id == parentId).FirstAsync();
            if (parent is null || parent.IsDeleted)
                return Error(BaseApiResponseCode.BadRequest, "父级分类不存在");
            // 两层父级允许第三级；第三次循环说明会创建第四级。
            if (depth > 2)
                return Error(BaseApiResponseCode.BadRequest, "分类不能超过3级");
            parentId = parent.ParentId;
        }

        var category = new Category { Name = request.Name, ParentId = request.ParentId, Sort = request.Sort };
        await freeSql.Insert(category).ExecuteAffrowsAsync();
        return Ok(category);
    }

    [HttpPut("{id}")]
    public async Task<ApiResponse> UpdateCategory([FromRoute] long id, [FromBody] CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Error(BaseApiResponseCode.BadRequest, "分类名称不能为空");
        var category = await freeSql.Select<Category>().Where(item => item.Id == id && !item.IsDeleted).FirstAsync();
        if (category is null) return Error(BaseApiResponseCode.NotFound, "分类不存在");
        if (request.ParentId > 0 && await IsDescendantAsync(id, request.ParentId))
            return Error(BaseApiResponseCode.BadRequest, "父级分类不能是当前分类的子孙分类");

        long checkedParentId = request.ParentId;
        int depth = 0;
        while (checkedParentId > 0)
        {
            depth++;
            var parent = await freeSql.Select<Category>().Where(item => item.Id == checkedParentId).FirstAsync();
            if (parent is null || parent.IsDeleted || parent.Id == id)
                return Error(BaseApiResponseCode.BadRequest, "父级分类无效或形成循环");
            if (depth > 2) return Error(BaseApiResponseCode.BadRequest, "分类不能超过3级");
            checkedParentId = parent.ParentId;
        }

        category.Name = request.Name; category.ParentId = request.ParentId; category.Sort = request.Sort;
        await freeSql.Update<Category>().SetSource(category).ExecuteAffrowsAsync();
        return Ok(new { success = true });
    }

    private async Task<bool> IsDescendantAsync(long ancestorId, long categoryId)
    {
        var parentId = categoryId;
        for (var depth = 0; depth < 4 && parentId > 0; depth++)
        {
            if (parentId == ancestorId) return true;
            parentId = await freeSql.Select<Category>().Where(item => item.Id == parentId).FirstAsync(item => item.ParentId);
        }
        return false;
    }

    [HttpPost]
    public async Task<ApiResponse> DisableCategory([FromBody] SetCategoryStateRequest request)
    {
        var updated = await freeSql.Update<Category>().Where(item => item.Id == request.Id && !item.IsDeleted)
            .Set(item => item.IsActive, request.IsActive).ExecuteAffrowsAsync() > 0;
        return updated ? Ok(new { success = true }) : Error(BaseApiResponseCode.NotFound, "分类不存在");
    }

    [HttpDelete("{id}")]
    public async Task<ApiResponse> DeleteCategory([FromRoute] long id)
    {
        if (!await freeSql.Select<Category>().AnyAsync(item => item.Id == id && !item.IsDeleted))
            return Error(BaseApiResponseCode.NotFound, "分类不存在");
        if (await freeSql.Select<Category>().AnyAsync(item => item.ParentId == id && !item.IsDeleted))
            return Error(BaseApiResponseCode.BadRequest, "请先删除或移走子分类");
        if (await freeSql.Select<Product>().AnyAsync(item => item.CategoryId == id && !item.IsDeleted))
            return Error(BaseApiResponseCode.BadRequest, "分类下仍有商品，不能删除");

        await freeSql.Update<Category>().Where(item => item.Id == id)
            .Set(item => item.IsDeleted, true).Set(item => item.DeletedAt, DateTime.Now).ExecuteAffrowsAsync();
        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<ApiResponse> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0) return Error(BaseApiResponseCode.BadRequest, "请选择文件");
        if (file.Length > 2 * 1024 * 1024) return Error(BaseApiResponseCode.BadRequest, "图片不能超过2MB");
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Error(BaseApiResponseCode.BadRequest, "仅支持上传图片");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var uploaded = new UploadedFile { ContentType = file.ContentType, FileName = file.FileName, Bytes = stream.ToArray() };
        await freeSql.Insert(uploaded).ExecuteAffrowsAsync();
        return Ok(new { url = $"/gateway/products/File/{uploaded.Id}" });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> File([FromRoute] long id)
    {
        var uploaded = await freeSql.Select<UploadedFile>().Where(item => item.Id == id && !item.IsDeleted).FirstAsync();
        if (uploaded is null) return NotFound();
        return File(uploaded.Bytes, uploaded.ContentType);
    }
}

public sealed record SetCategoryStateRequest(long Id, bool IsActive);
