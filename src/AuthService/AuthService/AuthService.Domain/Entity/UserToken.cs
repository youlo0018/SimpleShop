using OpenIddict.EntityFrameworkCore.Models;

namespace AuthService.Domain.Entity;

public class UserToken : OpenIddictEntityFrameworkCoreToken<Guid,UserApplication, UserAuthorization>
{
    
}