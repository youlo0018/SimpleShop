using System.Security.Cryptography;

namespace CommunalService.Application.Common;

public static class Tools
{
    public static byte[] GenerateSalt(int lengthInBytes = 16)
    {
        var salt = new byte[lengthInBytes];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        return salt;
    }
}