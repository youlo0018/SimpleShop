using CommunalService.Domain.Interfaces;
using FileService.Domain.Entity;

namespace FileService.Domain.IRepository;

/// <summary>文件元数据仓储：只做元数据落库与查询，文件内容由存储后端负责。</summary>
public interface IFileRepository : IBaseRepository<StoredFile>
{
    /// <summary>按对象键取元数据（本地存储回源读取内容用）。</summary>
    /// <param name="objectKey">对象键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>元数据；不存在返回 null。</returns>
    Task<StoredFile?> GetByObjectKeyAsync(string objectKey, CancellationToken cancellationToken = default);
}
