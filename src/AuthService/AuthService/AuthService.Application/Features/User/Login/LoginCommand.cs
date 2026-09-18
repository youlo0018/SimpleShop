using System.Security.Claims;
using MediatR;
using OpenIddict.Server.AspNetCore;

namespace AuthService.Application.Features.User.Login;

public record LoginCommand : IRequest<(ClaimsPrincipal claimsPrincipal, string authenticationScheme)>
{
    /// <summary>登录名。</summary>
    public string UserName { get; set; }
    /// <summary>密码（仅传输明文，落库为加盐散列）。</summary>
    public string Password { get; set; }
}
