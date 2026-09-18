using AuthService.Application.Features.User.Login;
using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AuthService.Api.Controllers;

/// <summary>
/// 后台认证入口：OpenIddict 令牌端点（password flow）。
/// 令牌由 AuthService 签发（issuer=SimpleShop.AuthService），网关验签后转 X-Claim-* 头。
/// </summary>
public class AuthController(IMediator mediator) : BaseController
{
    /// <summary>
    /// 令牌端点（POST /gateway/auth/Token，表单编码）：grant_type=password 时校验账号并签发访问令牌。
    /// 其他授权类型未启用（后台使用密码流 + 12 小时访问令牌）。
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Token()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if (request is null)
            return BadRequest(new { error = OpenIddictConstants.Errors.InvalidRequest });

        if (request.IsPasswordGrantType())
        {
            try
            {
                var result = await mediator.Send(new LoginCommand
                {
                    UserName = request.Username ?? string.Empty,
                    Password = request.Password ?? string.Empty
                });
                return SignIn(result.claimsPrincipal, result.authenticationScheme);
            }
            catch (UnauthorizedAccessException exception)
            {
                return BadRequest(new
                {
                    error = OpenIddictConstants.Errors.InvalidGrant,
                    error_description = exception.Message
                });
            }
        }

        return BadRequest(new
        {
            error = OpenIddictConstants.Errors.UnsupportedGrantType,
            error_description = "仅支持 password 授权类型"
        });
    }
}
