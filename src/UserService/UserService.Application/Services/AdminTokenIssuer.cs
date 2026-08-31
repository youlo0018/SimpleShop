using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CommunalService.Domain.Contracts.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UserService.Domain.Entity;

namespace UserService.Application.Services;

/// <summary>
/// 后台/商城统一 JWT 签发；租户声明必须进入 token，网关才能向下游传递可信租户上下文。
/// </summary>
public sealed class AdminTokenIssuer(IConfiguration configuration)
{
    private const string DefaultTokenSecret = "SimpleShop.Dev.Token.Secret.2026";

    public string CreateToken(User user, AuthorizationResponse authorization)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role),
            new("tenant_type", authorization.TenantType),
            new("platform_id", authorization.PlatformId.ToString()),
            new("merchant_id", authorization.MerchantId.ToString())
        };
        claims.AddRange(authorization.Permissions.Select(permission => new Claim("permission", permission)));
        claims.AddRange(authorization.Roles.Select(role => new Claim("role", role)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            configuration["Auth:TokenSecret"] ?? DefaultTokenSecret));
        var token = new JwtSecurityToken(
            issuer: "SimpleShop.UserService",
            audience: "SimpleShop",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
