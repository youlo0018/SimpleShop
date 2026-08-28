using CommunalService.Domain.Contracts.Messages;
using CommunalService.Domain.Contracts.Services;
using FreeSql;
using MagicOnion;
using MagicOnion.Server;
using UserService.Domain.Entity;

namespace UserService.Application.GrpcServices;

public sealed class UserGrpcService(IFreeSql freeSql) : ServiceBase<IUserService>, IUserService
{
    public async UnaryResult<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await freeSql.Select<User>()
            .Where(item => item.UserName == request.UserName && !item.IsDeleted)
            .FirstAsync();

        if (user is null || !user.IsEnabled || Hash(request.Password, user.Salt) != user.pwd)
        {
            return new LoginResponse();
        }

        return new LoginResponse { Id = user.Id, UserName = user.UserName };
    }

    private static string Hash(string password, string salt)
    {
        // 与 UserController 的本机登录保持同一种口令派生方式，避免双入口认证结果不一致。
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password + salt)));
    }
}
