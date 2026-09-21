using CommunalService.Domain.Infrastructure;
using ToolService.Domain.Entity;
using ToolService.Domain.IRepository;
using FreeSql;

namespace ToolService.Infrastructure.Repository;

/// <summary>文件元数据仓储实现（FreeSql，表 stored_file）。</summary>
public sealed class FileRepository(IFreeSql freeSql) : BaseRepository<StoredFile>(freeSql), IFileRepository
{
    /// <inheritdoc />
    public Task<StoredFile?> GetByObjectKeyAsync(string objectKey, CancellationToken cancellationToken = default)
        => freeSql.Select<StoredFile>().Where(item => item.ObjectKey == objectKey && !item.IsDeleted).FirstAsync(cancellationToken);
}
