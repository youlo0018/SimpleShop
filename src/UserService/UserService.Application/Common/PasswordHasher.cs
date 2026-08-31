using System.Security.Cryptography;
using System.Text;

namespace UserService.Application.Common;

/// <summary>
/// 口令散列：SHA256(password + salt)；盐值由 NewSalt 随机生成，落库保存。
/// </summary>
public static class PasswordHasher
{
    public static string NewSalt() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    public static string Hash(string password, string salt)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
}
