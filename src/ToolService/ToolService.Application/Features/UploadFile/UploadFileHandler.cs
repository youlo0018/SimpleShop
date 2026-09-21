using CommunalService.Domain;
using CommunalService.Domain.Enums;
using ToolService.Common.Options;
using ToolService.Application.Services;
using ToolService.Common.Services;
using ToolService.Domain.Entity;
using ToolService.Domain.IRepository;
using MediatR;

namespace ToolService.Application.Features.UploadFile;

/// <summary>
/// 统一上传：校验（白名单/大小/魔数）→ 按配置选择存储后端写入 → 落元数据。
/// 返回绝对访问地址，前端（后台/小程序/后续端）直接展示，无需再拼接网关域名。
/// </summary>
public sealed class UploadFileHandler(
    IFileRepository repository,
    FileUploadValidator validator,
    IFileStorage storage) : IRequestHandler<UploadFileCommand, ApiResponse>
{
    /// <summary>处理上传：失败返回 400 与可直接展示的原因。</summary>
    public async Task<ApiResponse> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        var file = request.File;
        if (file is null || file.Length == 0)
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "请选择文件");

        // 先读文件头用于魔数校验，再回到起点供存储写入（避免二次读盘）。
        var head = new byte[16];
        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        var read = await buffer.ReadAsync(head, cancellationToken);
        buffer.Position = 0;

        var (ok, category, contentType, error) = validator.Validate(file.FileName, file.Length, head[..read]);
        if (!ok) return ApiResults.Fail(BaseApiResponseCode.BadRequest, error);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var objectKey = $"{category}/{DateTime.Now:yyyyMMdd}/{Guid.NewGuid():N}{extension}";
        var url = await storage.SaveAsync(objectKey, buffer, contentType, cancellationToken);

        var entity = new StoredFile
        {
            FileName = Path.GetFileName(file.FileName),
            Extension = extension,
            ContentType = contentType,
            Size = file.Length,
            Category = category,
            Provider = storage.Provider,
            ObjectKey = objectKey,
            Url = url
        };
        await repository.InsertAsync(entity);

        return ApiResults.Ok(new
        {
            id = entity.Id,
            url = entity.Url,
            name = entity.FileName,
            size = entity.Size,
            contentType = entity.ContentType,
            category = entity.Category
        });
    }
}
