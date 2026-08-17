using OpenIddict.EntityFrameworkCore.Models;

namespace AuthService.Domain.Entity;

public class UserApplication:  OpenIddictEntityFrameworkCoreApplication<Guid,UserAuthorization,UserToken>
{
    public string? ClientType { get; set; }
    
}