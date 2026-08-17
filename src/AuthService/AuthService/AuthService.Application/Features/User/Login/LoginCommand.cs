using System.Security.Claims;
using MediatR;
using OpenIddict.Server.AspNetCore;

namespace AuthService.Application.Features.User.Login;

public record LoginCommand : IRequest<(ClaimsPrincipal claimsPrincipal, string authenticationScheme)>
{
    public string UserName { get; set; }
    public string Password { get; set; }
}