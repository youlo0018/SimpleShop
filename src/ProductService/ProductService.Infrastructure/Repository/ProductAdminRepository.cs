using FreeSql;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Infrastructure.Repository;

public class ProductAdminRepository(IFreeSql freeSql) : IProductAdminRepository
{
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

    public Task<List<Sku>> ListSkusByProductIdsAsync(long[] productIds, CancellationToken cancellationToken = default)
        => freeSql.Select<Sku>().Where(sku => productIds.Contains(sku.ProductId)).ToListAsync();

    public Task<Product?> GetProductInScopeAsync(long id, long? platformId, long? merchantScopeId, CancellationToken cancellationToken = default)
        => freeSql.Select<Product>()
            .Where(item => item.Id == id && !item.IsDeleted)
            .WhereIf(platformId.HasValue, item => item.PlatformId == platformId!.Value)
            .WhereIf(merchantScopeId.HasValue, item => item.MerchantId == merchantScopeId!.Value)
            .FirstAsync()!;

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

    public Task<Category?> GetCategoryAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<Category>().Where(item => item.Id == id).FirstAsync()!;

    public async Task<bool> InsertCategoryAsync(Category category, CancellationToken cancellationToken = default)
        => await freeSql.Insert(category).ExecuteAffrowsAsync(cancellationToken) > 0;

    public async Task<bool> UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default)
        => await freeSql.Update<Category>().SetSource(category).ExecuteAffrowsAsync(cancellationToken) > 0;

    public async Task<long?> GetParentIdAsync(long id, CancellationToken cancellationToken = default)
        => await freeSql.Select<Category>().Where(item => item.Id == id).FirstAsync(item => (long?)item.ParentId);

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

    public Task<bool> CategoryHasChildrenAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<Category>().AnyAsync(item => item.ParentId == id && !item.IsDeleted);

    public Task<bool> CategoryHasProductsAsync(long id, CancellationToken cancellationToken = default)
        => freeSql.Select<Product>().AnyAsync(item => item.CategoryId == id && !item.IsDeleted);

    public async Task<bool> SetCategoryActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default)
        => await freeSql.Update<Category>().Where(item => item.Id == id && !item.IsDeleted)
            .Set(item => item.IsActive, isActive).ExecuteAffrowsAsync(cancellationToken) > 0;

    public async Task<bool> SoftDeleteCategoryAsync(long id, CancellationToken cancellationToken = default)
        => await freeSql.Update<Category>().Where(item => item.Id == id)
            .Set(item => item.IsDeleted, true).Set(item => item.DeletedAt, DateTime.Now)
            .ExecuteAffrowsAsync(cancellationToken) > 0;

    public Task<bool> SkuCodesExistAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
        => freeSql.Select<Sku>().AnyAsync(item => codes.Contains(item.SkuCode));
}
