using System.Security.Claims;
using CommunalService.Domain;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthService.Api.Controllers;

public class AccountController : BaseController
{
    // GET
    /// <summary>
    /// 登录端点（供 OpenIddict 重定向）
    /// </summary>
    [HttpGet()]
    public async Task<IActionResult> Login(string returnUrl)
    {
        // 为了测试，直接创建一个模拟用户
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "testuser")
            // 可以添加角色等
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // 登录并重定向回 returnUrl（OpenIddict 会传递）
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return LocalRedirect(returnUrl ?? "/");
    }

    /// <summary>
    /// 登出端点（可选）
    /// </summary>
    [HttpGet()]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }
    [HttpGet]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request == null)
            return BadRequest();

        if (!User.Identity?.IsAuthenticated == true)
        {
            return Challenge(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        // 构建 ClaimsIdentity，必须包含 subject (ClaimTypes.NameIdentifier)
        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            ClaimTypes.Name,
            ClaimTypes.Role);

        // 必需：添加 subject claim
        // 添加 OpenIddict 标准 subject 声明
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, "1"));
// 同时保留 NameIdentifier（可选）
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "1"));
        identity.AddClaim(new Claim(ClaimTypes.Name, "testuser"));
        // 可选的 scope 声明
        identity.AddClaim(new Claim("scope", "api1 api2"));

        // 如果需要，可以添加其他 claims，如角色
        // identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));

        var principal = new ClaimsPrincipal(identity);

        // 返回 SignIn，OpenIddict 会自动生成授权码并重定向
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}