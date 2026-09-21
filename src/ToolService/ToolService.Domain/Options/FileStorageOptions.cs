namespace ToolService.Common.Options;

/// <summary>
/// 文件存储配置（AgileConfig 键：FileStorage:*）：决定使用哪个存储后端与各格式大小上限。
/// 默认本地存储；切换云厂商只需改 AgileConfig，无需改代码/重新发布。
/// </summary>
public sealed class FileStorageOptions
{
    /// <summary>配置节名。</summary>
    public const string SectionName = "FileStorage";

    /// <summary>存储后端：Local / AliyunOss / TencentCos / AzureBlob（默认 Local）。</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>本地存储配置。</summary>
    public LocalStorageOptions Local { get; set; } = new();

    /// <summary>阿里云 OSS 配置。</summary>
    public AliyunOssOptions AliyunOss { get; set; } = new();

    /// <summary>腾讯云 COS 配置。</summary>
    public TencentCosOptions TencentCos { get; set; } = new();

    /// <summary>微软云 Azure Blob 配置。</summary>
    public AzureBlobOptions AzureBlob { get; set; } = new();

    /// <summary>允许的扩展名（小写含点）；不在列表内一律拒绝。</summary>
    public List<string> AllowedExtensions { get; set; } =
    [
        // 图片
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp",
        // 文档
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".md",
        // 音频
        ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a",
        // 视频
        ".mp4", ".mov", ".avi", ".mkv", ".webm"
    ];

    /// <summary>分类大小上限（字节）：image/document/audio/video/default。</summary>
    public Dictionary<string, long> MaxSizeBytes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image"] = 5 * 1024 * 1024,
        ["document"] = 20 * 1024 * 1024,
        ["audio"] = 20 * 1024 * 1024,
        ["video"] = 200 * 1024 * 1024,
        ["default"] = 10 * 1024 * 1024
    };
}

/// <summary>本地存储：文件落盘 + 网关 Content 路由回源读取。</summary>
public sealed class LocalStorageOptions
{
    /// <summary>文件根目录（相对内容根或绝对路径）。</summary>
    public string RootPath { get; set; } = "logs/uploads";

    /// <summary>公开访问基址（默认走网关 /gateway/files/Content）。</summary>
    public string PublicBaseUrl { get; set; } = "http://127.0.0.1:5008/gateway/files/Content";
}

/// <summary>阿里云 OSS 配置。</summary>
public sealed class AliyunOssOptions
{
    /// <summary>地域节点，如 oss-cn-hangzhou.aliyuncs.com。</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Bucket 名称。</summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>AccessKeyId。</summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>AccessKeySecret。</summary>
    public string AccessKeySecret { get; set; } = string.Empty;

    /// <summary>自定义 CDN 域名（可空，空则用 Bucket 默认域名）。</summary>
    public string Domain { get; set; } = string.Empty;
}

/// <summary>腾讯云 COS 配置。</summary>
public sealed class TencentCosOptions
{
    /// <summary>地域，如 ap-guangzhou。</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>Bucket 名称（含 APPID 后缀）。</summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>SecretId。</summary>
    public string SecretId { get; set; } = string.Empty;

    /// <summary>SecretKey。</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>自定义 CDN 域名（可空）。</summary>
    public string Domain { get; set; } = string.Empty;
}

/// <summary>微软云 Azure Blob 配置。</summary>
public sealed class AzureBlobOptions
{
    /// <summary>连接字符串（含 AccountName/AccountKey）。</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>容器名称。</summary>
    public string Container { get; set; } = string.Empty;

    /// <summary>自定义 CDN 域名（可空）。</summary>
    public string Domain { get; set; } = string.Empty;
}
