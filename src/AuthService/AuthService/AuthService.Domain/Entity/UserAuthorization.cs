using OpenIddict.EntityFrameworkCore.Models;

namespace AuthService.Domain.Entity;

public class UserAuthorization : OpenIddictEntityFrameworkCoreAuthorization<Guid, UserApplication, UserToken>
{
    
}