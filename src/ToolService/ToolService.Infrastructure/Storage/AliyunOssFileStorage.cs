using Aliyun.OSS;
using ToolService.Common.Services;
using ToolService.Common.Options;
using Microsoft.Extensions.Options;

namespace ToolService.Infrastructure.Storage;

/// <summary>阿里云 OSS 存储：PutObject 写入，返回自定义 CDN 域名或 Bucket 默认域名。</summary>
public sealed class AliyunOssFileStorage : IFileStorage
{
    private readonly AliyunOssOptions _options;

    /// <summary>构造：绑定 OSS 配置（Endpoint/Bucket/AK/Domain）。</summary>
    public AliyunOssFileStorage(IOptions<FileStorageOptions> options) => _options = options.Value.AliyunOss;

    /// <inheritdoc />
    public string Provider => "AliyunOss";

    /// <inheritdoc />
    public async Task<string> SaveAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var client = new OssClient(_options.Endpoint, _options.AccessKeyId, _options.AccessKeySecret);
        await Task.Run(() => client.PutObject(_options.Bucket, objectKey, content, new ObjectMetadata { ContentType = contentType }), cancellationToken);
        return BuildUrl(objectKey);
    }

    /// <inheritdoc />
    public Task<(Stream Stream, string ContentType)?> OpenAsync(string objectKey, CancellationToken cancellationToken = default)
        => Task.FromResult<(Stream, string)?>(null);

    /// <summary>公开地址：优先自定义域名，否则 Bucket 默认域名。</summary>
    private string BuildUrl(string objectKey)
    {
        if (!string.IsNullOrWhiteSpace(_options.Domain)) return $"{_options.Domain.TrimEnd('/')}/{objectKey}";
        var endpoint = _options.Endpoint.Replace("https://", string.Empty).Replace("http://", string.Empty).TrimEnd('/');
        return $"https://{_options.Bucket}.{endpoint}/{objectKey}";
    }
}
