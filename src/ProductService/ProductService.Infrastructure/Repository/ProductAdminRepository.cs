using FreeSql;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Infrastructure.Repository;

/// <summary>商品后台仓储实现：SPU/SKU 分页与按租户范围取数、商品+SKU 同事务更新、分类树校验。</summary>
    public class ProductAdminRepository(IFreeSql freeSql) : IProductAdminRepository
{
    /// <summary>后台商品分页查询：关键字/状态/租户过滤，返回页数据与总数。</summary>
    public async Task<(List<Product> Items, long Total)> QueryProductsPagedAsync(
        string keyword, long categoryId, long merchantId, long? platformId, long? merchantScopeId,
        int? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var selection = freeSql.Select<Product>()
            .Where(product => !product.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), product => product.Name.Contains(keyword))
            .WhereIf(categoryId > 0, product => product.CategoryId == categoryId)
            .WhereIf(merchantId > 0, product => product.MerchantId == merchantId)
            .WhereIf(platformId.HasValue, product => product.PlatformId == platformId!.Value)
            .WhereIf(merchantScopeId.HasValue, product => product.MerchantId == merchantScopeId!.Value)
            .WhereIf(status.HasValue, product => product.Status == status!.Value);
        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(product => product.CreatedAt)
            .Page(Math.Max(page, 1), pageSize).ToListAsync();
        return (items, total);
    }

    /// <summary>查询列表（分页语义由实现约定）：ListSkusByProductIdsAsync。</summary>
    /// <summary>批量取商品 SKU（列表拼装用，空集合不发 SQL）。</summary>
    public Task<List<Sku>> ListSkusByProductIdsAsync(long[] productIds, CancellationToken cancellationToken = default)
        => freeSql.Select<Sku>().Where(sku => productIds.Contains(sku.ProductId)).ToListAsync();

    /// <summary>查询：GetProductInScopeAsync。</summary>
    /// <summary>按租户范围取商品：平台限本平台、商户限本商户，越权返回 null。</summary>
    public Task<Product?> GetProductInScopeAsync(long id, long? platformId, long? merchantScopeId, CancellationToken cancellationToken = default)
        => freeSql.Select<Product>()
            .Where(item => item.Id == id && !item.IsDeleted)
            .WhereIf(platformId.HasValue, item => item.PlatformId == platformId!.Value)
            .WhereIf(merchantScopeId.HasValue, item => item.MerchantId == merchantScopeId!.Value)
            .FirstAsync()!;

    /// <summary>更新：UpdateProductWithSkusAsync。</summary>
    /// <summary>同事务更新商品与 SKU（编辑商品一致性要求）。</summary>
    public async Task<bool> UpdateProductWithSkusAsync(Product product,
        IReadOnlyList<(string SkuCode, decimal Price, decimal OriginalPrice, int Stock, string? Image, string? SpecName, string? SpecValue)> skus,
        CancellationToken cancellationToken = default)
    {
        await freeSql.Update<Product>().SetSource(product).ExecuteAffrowsAsync(cancellationToken);
        foreach (var sku in skus)
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
                }).ExecuteAffrowsAsync(cancellationToken);
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
            await freeSql.Update<Sku>().SetSource(existing).ExecuteAffrowsAsync(cancellationToken);
        }

        return true;
    }

    /// <summary>查询：GetCategoryAsync。</summary>
    /// <summary>按主键取分类；不存在返回 null。</summary>
    public Task<Category?> GetCategoryAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<Category>().Where(item => item.Id == id).FirstAsync()!;

    /// <summary>写入/新增：InsertCategoryAsync。</summary>
    /// <summary>新增分类（层级由 Handler 校验 ≤3 级）。</summary>
    public async Task<bool> InsertCategoryAsync(Category category, CancellationToken cancellationToken = default)
        => await freeSql.Insert(category).ExecuteAffrowsAsync(cancellationToken) > 0;

    /// <summary>更新：UpdateCategoryAsync。</summary>
    /// <summary>更新分类。</summary>
    public async Task<bool> UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default)
        => await freeSql.Update<Category>().SetSource(category).ExecuteAffrowsAsync(cancellationToken) > 0;

    /// <summary>查询：GetParentIdAsync。</summary>
    /// <summary>取分类父级 ID（顶级返回 null）。</summary>
    public async Task<long?> GetParentIdAsync(long id, CancellationToken cancellationToken = default)
        => await freeSql.Select<Category>().Where(item => item.Id == id).FirstAsync(item => (long?)item.ParentId);

    /// <summary>判断 categoryId 是否为 ancestorId 的后代（防分类环）。</summary>
    public async Task<bool> IsDescendantAsync(long ancestorId, long categoryId, CancellationToken cancellationToken = default)
    {
        var parentId = categoryId;
        for (var depth = 0; depth < 4 && parentId > 0; depth++)
        {
            if (parentId == ancestorId) return true;
            parentId = await freeSql.Select<Category>().Where(item => item.Id == parentId).FirstAsync(item => item.ParentId);
        }
        return false;
    }

    /// <summary>分类是否存在子分类（有子级禁止删除）。</summary>
    public Task<bool> CategoryHasChildrenAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<Category>().AnyAsync(item => item.ParentId == id && !item.IsDeleted);

    /// <summary>分类下是否存在商品（有商品禁止删除）。</summary>
    public Task<bool> CategoryHasProductsAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<Product>().AnyAsync(item => item.CategoryId == id && !item.IsDeleted);

    /// <summary>更新：SetCategoryActiveAsync。</summary>
    /// <summary>启用/停用分类。</summary>
    public async Task<bool> SetCategoryActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default)
        => await freeSql.Update<Category>().Where(item => item.Id == id && !item.IsDeleted)
            .Set(item => item.IsActive, isActive).ExecuteAffrowsAsync(cancellationToken) > 0;

    /// <summary>软删除分类（前置校验由 Handler 完成）。</summary>
    public async Task<bool> SoftDeleteCategoryAsync(long id, CancellationToken cancellationToken = default)
        => await freeSql.Update<Category>().Where(item => item.Id == id)
            .Set(item => item.IsDeleted, true).Set(item => item.DeletedAt, DateTime.Now)
            .ExecuteAffrowsAsync(cancellationToken) > 0;

    /// <summary>SKU 编码是否已存在（商品内唯一校验）。</summary>
    public Task<bool> SkuCodesExistAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
        => freeSql.Select<Sku>().AnyAsync(item => codes.Contains(item.SkuCode));
}
