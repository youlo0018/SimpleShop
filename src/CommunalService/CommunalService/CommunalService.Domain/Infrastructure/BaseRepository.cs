using System.Linq.Expressions;
using CommunalService.Domain.Entity;
using CommunalService.Domain.Interfaces;

namespace CommunalService.Domain.Infrastructure;

/// <summary>
/// 仓储基类：封装 FreeSql 的常规读写，全部软删除语义（Delete 实际是置 IsDeleted）。
/// 各服务仓储继承它并按需扩展领域查询方法；查询侧注意全局过滤器已排除 IsDeleted=true 的行。
/// </summary>
public class BaseRepository<T>(IFreeSql freeSql) : IBaseRepository<T> where T : BaseEntity
{
    public bool Insert(T entity)
    {
        return freeSql.Insert(entity).ExecuteAffrows() > 0;
    }

    public async Task<bool> InsertAsync(T entity)
    {
        return (await freeSql.Insert(entity).ExecuteAffrowsAsync()) > 0;
    }

    public bool BatchInsert(IEnumerable<T> entities)
    {
        return freeSql.Insert(entities).ExecuteAffrows() > 0;
    }

    public async Task<bool> BatchInsertAsync(IEnumerable<T> entities)
    {
        return await freeSql.Insert(entities).ExecuteAffrowsAsync() > 0;
    }

    public bool Update(T entity)
    {
        return freeSql.Update<T>().SetSource(entity).ExecuteAffrows() > 0;
    }

    public async Task<bool> UpdateAsync(T entity)
    {
        return await freeSql.Update<T>().SetSource(entity).ExecuteAffrowsAsync() > 0;
    }

    public bool UpdateColumns(long id,object obj)
    {
        return freeSql.Update<T>().Where(x=>x.Id==id).UpdateColumns(a=>obj).ExecuteAffrows() > 0;
    }

    public async Task<bool> UpdateColumnsAsync(long id,object obj)
    {
        return await freeSql.Update<T>().Where(x=>x.Id==id).UpdateColumns(a=>obj).ExecuteAffrowsAsync() > 0;
    }

    public bool BatchUpdate(IEnumerable<T> entities)
    {
        return freeSql.Update<T>().SetSource(entities).ExecuteAffrows() > 0;
    }

    public async Task<bool> BatchUpdateAsync(IEnumerable<T> entities)
    {
        return await freeSql.Update<T>().SetSource(entities).ExecuteAffrowsAsync() > 0;
    }

    public bool Delete(T entity)
    {
       return UpdateColumns(entity.Id, new { IsDeleted=true, DeletedAt=DateTime.Now, UpdatedAt=DateTime.Now });
    }

    public async Task<bool> DeleteAsync(T entity)
    {
        return await UpdateColumnsAsync(entity.Id, new { IsDeleted=true, DeletedAt=DateTime.Now, UpdatedAt=DateTime.Now });
    }

    public bool Delete(long id)
    {
        return UpdateColumns(id, new { IsDeleted=true, DeletedAt=DateTime.Now, UpdatedAt=DateTime.Now });
    }

    public async Task<bool> DeleteAsync(long id)
    {
        
        return await UpdateColumnsAsync(id, new { IsDeleted=true, DeletedAt=DateTime.Now, UpdatedAt=DateTime.Now });
    }

    public T GetById(long id)
    {
       return freeSql.Select<T>().Where(x => x.Id == id).ToOne();
    }

    public async Task<T> GetByIdAsync(long id)
    {
        return await freeSql.Select<T>().Where(x => x.Id == id).ToOneAsync();
    }

   
    public List<T> Query(Expression<Func<T, bool>> predicate)
    {
        return  freeSql.Select<T>().Where(predicate).ToList();
    }

    public async Task<List<T>> QueryAsync(Expression<Func<T, bool>> predicate)
    {
        return await freeSql.Select<T>().Where(predicate).ToListAsync();
    }
}