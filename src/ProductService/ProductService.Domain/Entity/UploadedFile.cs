using System.ComponentModel;
using CommunalService.Domain.Entity;
using FreeSql.DataAnnotations;

namespace ProductService.Domain.Entity;

/// <summary>
/// 商品图片落库存元数据；对外只暴露生成的文件 URL，避免前端拿到内部路径。
/// </summary>
[Table(Name = "uploaded_file")]
public sealed class UploadedFile : BaseEntity
{
    [Column(StringLength = 80), Description("内容类型")] public string ContentType { get; set; } = string.Empty;
    [Column(StringLength = 255), Description("文件名")] public string FileName { get; set; } = string.Empty;
    [Column(DbType = "bytea"), Description("文件字节")] public byte[] Bytes { get; set; } = [];
}
