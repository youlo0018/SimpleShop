using ToolService.Application.Services;
using ToolService.Common.Services;
using ToolService.Domain.IRepository;
using MediatR;

namespace ToolService.Application.Features.GetFile;

/// <summary>本地文件回源：校验对象键存在且元数据登记过，再交给存储读取（防目录穿越）。</summary>
public sealed class GetFileHandler(IFileRepository repository, IFileStorage storage)
    : IRequestHandler<GetFileQuery, (Stream Stream, string ContentType)?>
{
    /// <summary>读取文件；未登记或文件缺失返回 null（控制器转 404）。</summary>
    public async Task<(Stream Stream, string ContentType)?> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ObjectKey)) return null;
        var meta = await repository.GetByObjectKeyAsync(request.ObjectKey, cancellationToken);
        if (meta is null) return null;
        var opened = await storage.OpenAsync(meta.ObjectKey, cancellationToken);
        return opened is null ? null : (opened.Value.Stream, meta.ContentType);
    }
}
