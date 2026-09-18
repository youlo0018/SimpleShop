using System.Security.Cryptography;
using System.Text;

namespace CustomerService.Application.Common;

/// <summary>口令散列：SHA256(password + salt)；盐值随机生成并随客户落库（与后台账号算法保持一致）。</summary>
public static class PasswordHasher
{
    /// <summary>生成 16 字节随机盐（Hex）。</summary>
    public static string NewSalt() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    /// <summary>散列口令：SHA256(password + salt) 的 Hex。</summary>
    public static string Hash(string password, string salt)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
}
