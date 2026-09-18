using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CustomerService.Domain.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CustomerService.Application.Services;

/// <summary>
/// 客户 JWT 签发：租户固定 customer、权限为空；与后台令牌使用同一密钥与受众，
/// 网关按 issuer 区分（SimpleShop.CustomerService）后转为 X-Claim-* 头。
/// </summary>
public sealed class CustomerTokenIssuer(IConfiguration configuration)
{
    private const string DefaultTokenSecret = "SimpleShop.Dev.Token.Secret.2026";

    /// <summary>为客户签发 12 小时有效的 HMAC JWT。</summary>
    public string CreateToken(Customer customer, string jti)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new(ClaimTypes.Name, customer.CustomerName),
            new(ClaimTypes.Role, "customer"),
            new("tenant_type", "customer"),
            new("platform_id", customer.PlatformId.ToString()),
            new("merchant_id", "0"),
            new("jti", jti)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            configuration["Auth:TokenSecret"] ?? DefaultTokenSecret));
        var token = new JwtSecurityToken(
            issuer: "SimpleShop.CustomerService",
            audience: "SimpleShop",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
