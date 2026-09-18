using System.Text.RegularExpressions;

namespace CommunalService.Domain.Logging;

/// <summary>
/// 日志脱敏器：手机号、证件号、令牌这类数据可以排障，但不能原样落库。
/// </summary>
public static partial class SensitiveDataSanitizer
{
    [GeneratedRegex(@"(?<!\d)1[3-9]\d{9}(?!\d)")]
    /// <summary>手机号匹配（源生成正则，性能优先）。</summary>
    private static partial Regex MobilePhoneRegex();

    [GeneratedRegex(@"(?<!\d)\d{17}[\dXx](?!\d)")]
    /// <summary>身份证号匹配。</summary>
    private static partial Regex IdentityCardRegex();

    [GeneratedRegex(@"(?i)(authorization|token|access[_-]?token|refresh[_-]?token|password|secret)\s*[:=]\s*([^,;\s}""']+)")]
    /// <summary>口令/密钥等敏感字段匹配。</summary>
    private static partial Regex CredentialRegex();

    /// <summary>脱敏入口：手机号/身份证/凭据统一替换，日志落库前调用。</summary>
    public static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var sanitized = CredentialRegex().Replace(value, "$1=***");
        sanitized = MobilePhoneRegex().Replace(sanitized, "***");
        return IdentityCardRegex().Replace(sanitized, "***");
    }
}
