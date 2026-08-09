using System.Linq.Expressions;
using CommunalService.Domain.Entity;

namespace CommunalService.Domain.Interfaces;

public interface IBaseRepository<T> where T:BaseEntity
{
    /// <summary>
    /// 插入实体
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool Insert(T entity);
    /// <summary>
    /// 插入实体 (异步)
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public Task<bool> InsertAsync(T entity);
    /// <summary>
    /// 批量插入实体 
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public bool BatchInsert(IEnumerable<T> entities);
    /// <summary>
    /// 批量插入实体 (异步)
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public Task<bool> BatchInsertAsync(IEnumerable<T> entities);
    /// <summary>
    /// 更新实体 全量更新不推荐使用
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool Update(T entity);
    /// <summary>
    /// 更新实体(异步) 全量更新不推荐使用
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public Task<bool> UpdateAsync(T entity);

    /// <summary>
    /// 更新实体 只更新传入的字段
    /// </summary>
    /// <param name="id"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool UpdateColumns(long id, object obj);
    /// <summary>
    /// 更新实体 (异步) 只更新传入的字段
    /// </summary>
    /// <param name="id"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public Task<bool> UpdateColumnsAsync(long id, object obj);
    /// <summary>
    /// 批量更新实体 
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public bool BatchUpdate(IEnumerable<T> entities);
    /// <summary>
    /// 批量更新实体(异步)
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    public Task<bool> BatchUpdateAsync(IEnumerable<T> entities);
    /// <summary>
    /// 根据实体删除 实际是更新IsDelete属性不会物理删除
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public bool Delete(T entity);
    /// <summary>
    /// 根据实体删除 (异步) 实际是更新IsDelete属性不会物理删除
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public Task<bool> DeleteAsync(T entity);
    /// <summary>
    /// 根据id删除 实际是更新IsDelete属性不会物理删除
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool Delete(long id);
    /// <summary>
    /// 根据id删除 (异步) 实际是更新IsDelete属性不会物理删除
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<bool> DeleteAsync(long id);
    /// <summary>
    /// 根据id查询实体
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public T GetById(long id);
    /// <summary>
    /// 根据id查询实体 (异步)
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<T> GetByIdAsync(long id);
    /// <summary>
    /// 根据条件查询实体 
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public List<T> Query(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// 根据条件查询实体 (异步)
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public  Task<List<T>> QueryAsync(Expression<Func<T, bool>> predicate);
}