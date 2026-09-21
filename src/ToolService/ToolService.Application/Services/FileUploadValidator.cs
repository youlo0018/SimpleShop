using ToolService.Common.Options;

namespace ToolService.Application.Services;

/// <summary>
/// 上传校验：扩展名白名单 → 分类大小上限 → 文件头魔数（防伪造扩展名）。
/// 允许格式与大小上限全部来自配置（AgileConfig 可热更），不写死在代码里。
/// </summary>
public sealed class FileUploadValidator(FileStorageOptions options)
{
    private static readonly Dictionary<string, string[]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = ["FFD8FF"], [".jpeg"] = ["FFD8FF"], [".png"] = ["89504E47"], [".gif"] = ["47494638"],
        [".bmp"] = ["424D"], [".webp"] = ["52494646"], [".pdf"] = ["25504446"], [".docx"] = ["504B0304"],
        [".xlsx"] = ["504B0304"], [".pptx"] = ["504B0304"], [".doc"] = ["D0CF11E0"], [".xls"] = ["D0CF11E0"],
        [".ppt"] = ["D0CF11E0"], [".mp3"] = ["494433", "FFFB", "FFF3"], [".mp4"] = ["66747970"],
        [".mov"] = ["66747970"], [".m4a"] = ["66747970"], [".flac"] = ["664C6143"], [".ogg"] = ["4F676753"],
        [".wav"] = ["52494646"], [".webm"] = ["1A45DFA3"], [".mkv"] = ["1A45DFA3"], [".avi"] = ["52494646"]
    };

    private static readonly Dictionary<string, string> Categories = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image", [".jpeg"] = "image", [".png"] = "image", [".gif"] = "image", [".webp"] = "image", [".bmp"] = "image",
        [".pdf"] = "document", [".doc"] = "document", [".docx"] = "document", [".xls"] = "document", [".xlsx"] = "document",
        [".ppt"] = "document", [".pptx"] = "document", [".txt"] = "document", [".csv"] = "document", [".md"] = "document",
        [".mp3"] = "audio", [".wav"] = "audio", [".flac"] = "audio", [".aac"] = "audio", [".ogg"] = "audio", [".m4a"] = "audio",
        [".mp4"] = "video", [".mov"] = "video", [".avi"] = "video", [".mkv"] = "video", [".webm"] = "video"
    };

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".gif"] = "image/gif",
        [".webp"] = "image/webp", [".bmp"] = "image/bmp", [".pdf"] = "application/pdf",
        [".txt"] = "text/plain", [".csv"] = "text/csv", [".md"] = "text/markdown",
        [".mp3"] = "audio/mpeg", [".wav"] = "audio/wav", [".flac"] = "audio/flac", [".aac"] = "audio/aac",
        [".ogg"] = "audio/ogg", [".m4a"] = "audio/mp4",
        [".mp4"] = "video/mp4", [".mov"] = "video/quicktime", [".avi"] = "video/x-msvideo",
        [".mkv"] = "video/x-matroska", [".webm"] = "video/webm"
    };

    /// <summary>校验并返回分类/MIME；失败返回可直接展示的原因。</summary>
    /// <param name="fileName">原始文件名（取扩展名）。</param>
    /// <param name="size">文件大小（字节）。</param>
    /// <param name="head">文件头前 16 字节（可为空，为空跳过魔数校验）。</param>
    /// <returns>(是否通过, 分类, MIME, 失败原因)。</returns>
    public (bool Ok, string Category, string ContentType, string Error) Validate(string fileName, long size, byte[]? head)
    {
        var extension = Path.GetExtension(Path.GetFileName(fileName ?? string.Empty)).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !options.AllowedExtensions.Contains(extension))
            return (false, string.Empty, string.Empty, "不支持的文件格式");

        var category = Categories.GetValueOrDefault(extension, "default");
        var limit = options.MaxSizeBytes.TryGetValue(category, out var configured)
            ? configured
            : options.MaxSizeBytes.GetValueOrDefault("default", 10 * 1024 * 1024);
        if (size <= 0) return (false, category, string.Empty, "文件不能为空");
        if (size > limit) return (false, category, string.Empty, $"文件不能超过{limit / 1024 / 1024}MB");

        if (head is { Length: > 0 } && Signatures.TryGetValue(extension, out var signatures))
        {
            var hex = Convert.ToHexString(head);
            // webp/wav/avi 是 RIFF 容器，mp4/mov 是 ftyp，需在头部任意位置匹配，其余按前缀匹配。
            var matched = extension is ".webp" or ".wav" or ".avi" or ".mp4" or ".mov" or ".m4a"
                ? signatures.Any(signature => hex.Contains(signature, StringComparison.OrdinalIgnoreCase))
                : signatures.Any(signature => hex.StartsWith(signature, StringComparison.OrdinalIgnoreCase));
            if (!matched) return (false, category, string.Empty, "文件内容与格式不符");
        }

        return (true, category, ContentTypes.GetValueOrDefault(extension, "application/octet-stream"), string.Empty);
    }
}
