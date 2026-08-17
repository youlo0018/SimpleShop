namespace CommunalService.Domain.Contracts.Messages;

public class LoginRequest
{
    public string UserName { get; set; }
    public string Password { get; set; }
}