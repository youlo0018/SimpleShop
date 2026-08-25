using MediatR;
using EntityCategory = ProductService.Domain.Entity.Category;
using ProductService.Domain.Entity;
using ProductService.Domain.IRepository;

namespace ProductService.Application.Features.Category.GetCategoryTree;

public class GetCategoryTreeCommandHandler(ICategoryRepository<EntityCategory> repository)
    : IRequestHandler<GetCategoryTreeCommand, object>
{
    public async Task<object> Handle(GetCategoryTreeCommand request, CancellationToken cancellationToken)
    {
        var all = await repository.QueryAsync(c => c.IsActive);
        var roots = all.Where(c => c.ParentId == 0).OrderBy(c => c.Sort).ToList();

        List<object> BuildTree(long parentId)
        {
            return all.Where(c => c.ParentId == parentId)
                .OrderBy(c => c.Sort)
                .Select(c => (object)new { c.Id, c.Name, c.Sort, children = BuildTree(c.Id) })
                .ToList();
        }

        return roots.Select(r => new { r.Id, r.Name, r.Sort, children = BuildTree(r.Id) }).ToList();
    }
}
