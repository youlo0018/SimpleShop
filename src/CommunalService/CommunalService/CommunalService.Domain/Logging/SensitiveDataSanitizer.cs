using System.Text.RegularExpressions;

namespace CommunalService.Domain.Logging;

/// <summary>
/// 日志脱敏器：手机号、证件号、令牌这类数据可以排障，但不能原样落库。
/// </summary>
public static partial class SensitiveDataSanitizer
{
    [GeneratedRegex(@"(?<!\d)1[3-9]\d{9}(?!\d)")]
    private static partial Regex MobilePhoneRegex();

    [GeneratedRegex(@"(?<!\d)\d{17}[\dXx](?!\d)")]
    private static partial Regex IdentityCardRegex();

    [GeneratedRegex(@"(?i)(authorization|token|access[_-]?token|refresh[_-]?token|password|secret)\s*[:=]\s*([^,;\s}""']+)")]
    private static partial Regex CredentialRegex();

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
