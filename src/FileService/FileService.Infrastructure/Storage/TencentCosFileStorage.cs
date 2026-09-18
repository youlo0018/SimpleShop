
using Microsoft.Extensions.Options;
using COSXML;
using COSXML.Auth;
using COSXML.Model.Object;
using FileService.Common.Services;
using FileService.Common.Options;

namespace FileService.Infrastructure.Storage;

/// <summary>腾讯云 COS 存储：PutObject 写入，返回自定义 CDN 域名或 COS 默认域名。</summary>
public sealed class TencentCosFileStorage : IFileStorage
{
    private readonly TencentCosOptions _options;

    /// <summary>构造：绑定 COS 配置（Region/Bucket/SecretId/SecretKey/Domain）。</summary>
    public TencentCosFileStorage(IOptions<FileStorageOptions> options) => _options = options.Value.TencentCos;

    /// <inheritdoc />
    public string Provider => "TencentCos";

    /// <inheritdoc />
    public Task<string> SaveAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var config = new CosXmlConfig.Builder().SetRegion(_options.Region).Build();
        var credential = new DefaultQCloudCredentialProvider(_options.SecretId, _options.SecretKey, 600);
        var cos = new CosXmlServer(config, credential);
        var request = new PutObjectRequest(_options.Bucket, objectKey, content);
        request.SetRequestHeader("Content-Type", contentType);
        cos.PutObject(request);
        return Task.FromResult(BuildUrl(objectKey));
    }

    /// <inheritdoc />
    public Task<(Stream Stream, string ContentType)?> OpenAsync(string objectKey, CancellationToken cancellationToken = default)
        => Task.FromResult<(Stream, string)?>(null);

    /// <summary>公开地址：优先自定义域名，否则 COS 默认域名。</summary>
    private string BuildUrl(string objectKey)
    {
        if (!string.IsNullOrWhiteSpace(_options.Domain)) return $"{_options.Domain.TrimEnd('/')}/{objectKey}";
        return $"https://{_options.Bucket}.cos.{_options.Region}.myqcloud.com/{objectKey}";
    }
}
