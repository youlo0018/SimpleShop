using AuthService.Application.Features.User.Login;
using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

public class AuthController(IMediator mediator) : BaseController
{
    private readonly IMediator _mediator = mediator;
    [HttpPost]
    public async Task<ApiResponse> Login([FromBody] LoginCommand command)
    {
      var result= await _mediator.Send(command);
      var token=SignIn(result.claimsPrincipal, result.authenticationScheme);
      return Ok(token);
    }
    
}