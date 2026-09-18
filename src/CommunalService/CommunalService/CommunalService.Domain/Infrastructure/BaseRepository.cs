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
    /// <summary>写操作：Insert（副作用与幂等键见调用方约定）。</summary>
    public bool Insert(T entity)
    {
        return freeSql.Insert(entity).ExecuteAffrows() > 0;
    }

    /// <summary>写入/新增：InsertAsync。</summary>
    public async Task<bool> InsertAsync(T entity)
    {
        return (await freeSql.Insert(entity).ExecuteAffrowsAsync()) > 0;
    }

    /// <summary>内部处理：BatchInsert。</summary>
    public bool BatchInsert(IEnumerable<T> entities)
    {
        return freeSql.Insert(entities).ExecuteAffrows() > 0;
    }

    /// <summary>内部处理：BatchInsertAsync。</summary>
    public async Task<bool> BatchInsertAsync(IEnumerable<T> entities)
    {
        return await freeSql.Insert(entities).ExecuteAffrowsAsync() > 0;
    }

    /// <summary>写操作：Update（副作用与幂等键见调用方约定）。</summary>
    public bool Update(T entity)
    {
        return freeSql.Update<T>().SetSource(entity).ExecuteAffrows() > 0;
    }

    /// <summary>更新：UpdateAsync。</summary>
    public async Task<bool> UpdateAsync(T entity)
    {
        return await freeSql.Update<T>().SetSource(entity).ExecuteAffrowsAsync() > 0;
    }

    /// <summary>
    /// 按对象属性名更新指定行的部分列（软删除/启停等字段级更新入口）。
    /// 必须用 SetDto：UpdateColumns(a =&gt; obj) 对捕获的匿名对象无法解析出列，会生成空 SET 静默不更新。
    /// </summary>
    public bool UpdateColumns(long id, object obj)
    {
        return freeSql.Update<T>().Where(x => x.Id == id).SetDto(obj).ExecuteAffrows() > 0;
    }

    /// <summary>UpdateColumns 的异步版本；实现说明见同步方法。</summary>
    public async Task<bool> UpdateColumnsAsync(long id, object obj)
    {
        return await freeSql.Update<T>().Where(x => x.Id == id).SetDto(obj).ExecuteAffrowsAsync() > 0;
    }

    /// <summary>内部处理：BatchUpdate。</summary>
    public bool BatchUpdate(IEnumerable<T> entities)
    {
        return freeSql.Update<T>().SetSource(entities).ExecuteAffrows() > 0;
    }

    /// <summary>内部处理：BatchUpdateAsync。</summary>
    public async Task<bool> BatchUpdateAsync(IEnumerable<T> entities)
    {
        return await freeSql.Update<T>().SetSource(entities).ExecuteAffrowsAsync() > 0;
    }

    /// <summary>写操作：Delete（副作用与幂等键见调用方约定）。</summary>
    public bool Delete(T entity)
    {
       return UpdateColumns(entity.Id, new { IsDeleted=true, DeletedAt=DateTime.Now, UpdatedAt=DateTime.Now });
    }

    /// <summary>删除：DeleteAsync。</summary>
    public async Task<bool> DeleteAsync(T entity)
    {
        return await UpdateColumnsAsync(entity.Id, new { IsDeleted=true, DeletedAt=DateTime.Now, UpdatedAt=DateTime.Now });
    }

    /// <summary>写操作：Delete（副作用与幂等键见调用方约定）。</summary>
    public bool Delete(long id)
    {
        return UpdateColumns(id, new { IsDeleted=true, DeletedAt=DateTime.Now, UpdatedAt=DateTime.Now });
    }

    /// <summary>删除：DeleteAsync。</summary>
    public async Task<bool> DeleteAsync(long id)
    {
        
        return await UpdateColumnsAsync(id, new { IsDeleted=true, DeletedAt=DateTime.Now, UpdatedAt=DateTime.Now });
    }

    /// <summary>查询数据：GetById（过滤条件与返回语义见参数与调用方约定）。</summary>
    public T GetById(long id)
    {
       return freeSql.Select<T>().Where(x => x.Id == id).ToOne();
    }

    /// <summary>查询：GetByIdAsync。</summary>
    public async Task<T> GetByIdAsync(long id)
    {
        return await freeSql.Select<T>().Where(x => x.Id == id).ToOneAsync();
    }

   
    /// <summary>查询数据：Query（过滤条件与返回语义见参数与调用方约定）。</summary>
    public List<T> Query(Expression<Func<T, bool>> predicate)
    {
        return  freeSql.Select<T>().Where(predicate).ToList();
    }

    /// <summary>查询：QueryAsync。</summary>
    public async Task<List<T>> QueryAsync(Expression<Func<T, bool>> predicate)
    {
        return await freeSql.Select<T>().Where(predicate).ToListAsync();
    }
}
