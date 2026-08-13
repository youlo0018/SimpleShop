using Microsoft.AspNetCore.Mvc;
using UserService.Domain.Entity;

namespace UserService.Api.Controllers;

public class UserController(IFreeSql freeSql) : BaseController
{
    [HttpGet]
    public IActionResult SyncStructure()
    {
        freeSql.CodeFirst.SyncStructure<User>();
        return Ok();
    }
}