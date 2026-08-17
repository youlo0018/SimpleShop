using CommunalService.Domain.Contracts.Messages;
using MagicOnion;

namespace CommunalService.Domain.Contracts.Services;

public interface IUserService: IService<IUserService>
{
    UnaryResult<LoginResponse> LoginAsync(LoginRequest request);
}