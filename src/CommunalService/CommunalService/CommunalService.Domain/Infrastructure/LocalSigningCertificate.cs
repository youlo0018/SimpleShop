using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CommunalService.Domain.Infrastructure;

/// <summary>
/// 本地签名证书：开发环境用于 OpenIddict 非对称签名（RS256）。
/// 首次调用生成自签证书到共享路径（默认 logs/runtime/auth-signing.pfx，已被 gitignore），
/// AuthService 与网关读取同一文件即可完成签发/验签，无需证书库或外部依赖。
/// </summary>
public static class LocalSigningCertificate
{
    private const string DefaultFileName = "auth-signing.pfx";
    private const string DefaultPassword = "SimpleShop.Dev.Cert.2026";

    /// <summary>读取共享签名证书；不存在时生成并落盘（同一进程内只生成一次）。</summary>
    /// <param name="configuration">配置：Auth:SigningCertificatePath / Auth:SigningCertificatePassword。</param>
    /// <returns>可用于签发与验签的证书（含私钥）。</returns>
    public static X509Certificate2 LoadOrCreate(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        // 默认放到用户级共享目录：各服务工作目录不同，相对路径会各自生成不同证书导致验签失败。
        var configured = configuration["Auth:SigningCertificatePath"];
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimpleShop", DefaultFileName)
            : Path.GetFullPath(configured);
        var password = configuration["Auth:SigningCertificatePassword"] ?? DefaultPassword;
        if (File.Exists(path)) return X509CertificateLoader.LoadPkcs12FromFile(path, password);

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=SimpleShop Auth Signing", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5));
        var bytes = certificate.Export(X509ContentType.Pfx, password);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
        return X509CertificateLoader.LoadPkcs12(bytes, password);
    }
}
