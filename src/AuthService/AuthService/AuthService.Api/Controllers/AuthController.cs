using AuthService.Application.Features.User.Login;
using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Api.Controllers;

public class AuthController(IMediator mediator) : BaseController
{
    private readonly IMediator _mediator = mediator;
    [HttpPost]
    public async Task<ApiResponse> Login([FromBody] LoginCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            var principal = result.claimsPrincipal;
            return Ok(new
            {
                authenticated = true,
                userId = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!),
                userName = principal.FindFirstValue(ClaimTypes.Name)
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Error(BaseApiResponseCode.Unauthorized, exception.Message);
        }
    }
    
}
