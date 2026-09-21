using ToolService.Common.Services;
using ToolService.Common.Options;
using Microsoft.Extensions.Options;

namespace ToolService.Infrastructure.Storage;

/// <summary>
/// 本地磁盘存储（默认后端）：文件写入配置目录，访问走网关 Content 路由回源。
/// 对象键由 Handler 生成（分类/日期/GUID），这里额外做目录穿越防护。
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly LocalStorageOptions _options;

    /// <summary>构造：绑定本地存储配置（根目录/公开基址）。</summary>
    public LocalFileStorage(IOptions<FileStorageOptions> options) => _options = options.Value.Local;

    /// <inheritdoc />
    public string Provider => "Local";

    /// <inheritdoc />
    public async Task<string> SaveAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var target = File.Create(path);
        await content.CopyToAsync(target, cancellationToken);
        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{objectKey}";
    }

    /// <inheritdoc />
    public Task<(Stream Stream, string ContentType)?> OpenAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(objectKey);
        if (!File.Exists(path)) return Task.FromResult<(Stream, string)?>(null);
        Stream stream = File.OpenRead(path);
        return Task.FromResult<(Stream, string)?>((stream, "application/octet-stream"));
    }

    /// <summary>对象键 → 绝对路径：拼接根目录并校验结果仍在根目录内（防 ../ 穿越）。</summary>
    private string ResolvePath(string objectKey)
    {
        var root = Path.GetFullPath(_options.RootPath);
        var path = Path.GetFullPath(Path.Combine(root, objectKey.Replace('\\', '/').TrimStart('/')));
        if (!path.StartsWith(root, StringComparison.Ordinal)) throw new InvalidOperationException("非法的对象键");
        return path;
    }
}
