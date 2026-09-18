using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using FileService.Common.Services;
using FileService.Common.Options;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.Storage;

/// <summary>微软云 Azure Blob 存储：容器不存在自动创建，返回自定义 CDN 域名或 Blob 默认地址。</summary>
public sealed class AzureBlobFileStorage : IFileStorage
{
    private readonly AzureBlobOptions _options;

    /// <summary>构造：绑定 Azure Blob 配置（ConnectionString/Container/Domain）。</summary>
    public AzureBlobFileStorage(IOptions<FileStorageOptions> options) => _options = options.Value.AzureBlob;

    /// <inheritdoc />
    public string Provider => "AzureBlob";

    /// <inheritdoc />
    public async Task<string> SaveAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var container = new BlobContainerClient(_options.ConnectionString, _options.Container);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blob = container.GetBlobClient(objectKey);
        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
        return BuildUrl(container, objectKey);
    }

    /// <inheritdoc />
    public Task<(Stream Stream, string ContentType)?> OpenAsync(string objectKey, CancellationToken cancellationToken = default)
        => Task.FromResult<(Stream, string)?>(null);

    /// <summary>公开地址：优先自定义域名，否则容器地址。</summary>
    private string BuildUrl(BlobContainerClient container, string objectKey)
    {
        if (!string.IsNullOrWhiteSpace(_options.Domain)) return $"{_options.Domain.TrimEnd('/')}/{objectKey}";
        return $"{container.Uri.ToString().TrimEnd('/')}/{objectKey}";
    }
}
