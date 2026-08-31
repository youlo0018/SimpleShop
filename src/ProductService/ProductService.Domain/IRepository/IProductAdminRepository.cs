using ProductService.Domain.Entity;

namespace ProductService.Domain.IRepository;

/// <summary>
/// 后台商品/分类管理仓储：组合查询与批量写入收口在 Infrastructure，Application 层不依赖 IFreeSql。
/// </summary>
public interface IProductAdminRepository
{
    Task<(List<Product> Items, long Total)> QueryProductsPagedAsync(
        string keyword, long categoryId, long merchantId, long? platformId, long? merchantScopeId,
        int? status, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<List<Sku>> ListSkusByProductIdsAsync(long[] productIds, CancellationToken cancellationToken = default);

    Task<Product?> GetProductInScopeAsync(long id, long? platformId, long? merchantScopeId, CancellationToken cancellationToken = default);

    /// <summary>更新商品主表并按编码 Upsert SKU（价格/库存非法的 SKU 跳过）。</summary>
    Task<bool> UpdateProductWithSkusAsync(Product product,
        IReadOnlyList<(string SkuCode, decimal Price, decimal OriginalPrice, int Stock, string? Image, string? SpecName, string? SpecValue)> skus,
        CancellationToken cancellationToken = default);

    Task<Category?> GetCategoryAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> InsertCategoryAsync(Category category, CancellationToken cancellationToken = default);
    Task<bool> UpdateCategoryAsync(Category category, CancellationToken cancellationToken = default);
    Task<long?> GetParentIdAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> IsDescendantAsync(long ancestorId, long categoryId, CancellationToken cancellationToken = default);
    Task<bool> CategoryHasChildrenAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> CategoryHasProductsAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> SetCategoryActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteCategoryAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> SkuCodesExistAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);
}
