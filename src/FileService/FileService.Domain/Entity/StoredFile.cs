using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace FileService.Domain.Entity;

/// <summary>
/// 已上传文件元数据：文件内容由存储后端（本地/云对象存储）保存，本表只记录索引与公开地址。
/// 所有业务（商品图/装修图/头像/后续附件）统一走 FileService，便于切换存储与审计。
/// </summary>
[Table(Name = "stored_file")]
[Index("idx_stored_file_created", nameof(CreatedAt))]
public sealed class StoredFile : BaseEntity
{
    /// <summary>原始文件名（仅展示用，已去除路径）。</summary>
    [Column(StringLength = 255), Description("原始文件名")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>扩展名（小写，含点，如 .png）。</summary>
    [Column(StringLength = 16), Description("扩展名")]
    public string Extension { get; set; } = string.Empty;

    /// <summary>内容类型（MIME，服务端按扩展名/文件头判定）。</summary>
    [Column(StringLength = 128), Description("内容类型")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>文件大小（字节）。</summary>
    [Description("文件大小（字节）")]
    public long Size { get; set; }

    /// <summary>文件分类：image/document/audio/video（大小限制按分类配置）。</summary>
    [Column(StringLength = 16), Description("文件分类")]
    public string Category { get; set; } = string.Empty;

    /// <summary>存储后端：Local/AliyunOss/TencentCos/AzureBlob（由 AgileConfig 决定）。</summary>
    [Column(StringLength = 16), Description("存储后端")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>对象键（本地为相对路径，云为对象 Key），用于定位/删除文件。</summary>
    [Column(StringLength = 512), Description("对象键")]
    public string ObjectKey { get; set; } = string.Empty;

    /// <summary>公开访问地址（本地为网关 Content 路由，云为对象存储域名）。</summary>
    [Column(StringLength = 512), Description("公开访问地址")]
    public string Url { get; set; } = string.Empty;
}
