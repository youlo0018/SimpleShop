namespace ToolService.Common.Services;

/// <summary>
/// 文件存储后端抽象：本地磁盘与各云对象存储实现同一接口，由配置（FileStorage:Provider）选择。
/// 所有实现都必须保证对象键唯一、内容不可覆盖（对象键带 GUID），并提供可公开访问的地址。
/// </summary>
public interface IFileStorage
{
    /// <summary>后端标识：Local/AliyunOss/TencentCos/AzureBlob（写入元数据便于审计与迁移）。</summary>
    string Provider { get; }

    /// <summary>保存文件并返回公开访问地址。</summary>
    /// <param name="objectKey">对象键（由 Handler 生成，含日期与 GUID）。</param>
    /// <param name="content">文件内容流（调用方保证可读）。</param>
    /// <param name="contentType">内容类型（MIME）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>公开访问地址（本地为网关地址，云为对象存储/自定义域名）。</returns>
    Task<string> SaveAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>读取文件内容（仅本地存储需要回源；云存储返回 null 由 URL 直接访问）。</summary>
    /// <param name="objectKey">对象键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>内容流与 MIME；不存在返回 null。</returns>
    Task<(Stream Stream, string ContentType)?> OpenAsync(string objectKey, CancellationToken cancellationToken = default);
}
