using MediatR;
using EntityCategory = ProductService.Domain.Entity.Category;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Category.GetCategoryTree;

/// <summary>分类树：返回启用分类的层级结构。</summary>
public class GetCategoryTreeCommandHandler(ICategoryRepository<EntityCategory> repository)
    : IRequestHandler<GetCategoryTreeCommand, object>
{
    /// <summary>处理入口：分类树：返回启用分类的层级结构。</summary>
    public async Task<object> Handle(GetCategoryTreeCommand request, CancellationToken cancellationToken)
    {
        // 后台分类管理必须能编辑停用节点；商城列表接口另行按启用状态过滤。
        var all = await repository.QueryAsync(c => !c.IsDeleted);
        var roots = all.Where(c => c.ParentId == 0).OrderBy(c => c.Sort).ToList();

        List<object> BuildTree(long parentId, int depth)
        {
            if (depth >= 3) return [];

            return all.Where(c => c.ParentId == parentId)
                .OrderBy(c => c.Sort)
                .Select(c => (object)new { c.Id, c.Name, c.ParentId, c.Sort, c.IsActive, children = BuildTree(c.Id, depth + 1) })
                .ToList();
        }

        return roots.Select(r => (object)new { r.Id, r.Name, r.ParentId, r.Sort, r.IsActive, children = BuildTree(r.Id, 1) }).ToList();
    }
}
