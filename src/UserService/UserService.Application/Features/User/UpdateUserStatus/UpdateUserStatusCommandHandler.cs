using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using UserService.Domain.IRepository;

namespace UserService.Application.Features.User.UpdateUserStatus;

/// <summary>
/// 启用/禁用账号：禁用后登录被拒（Login 校验 IsEnabled），已签发的令牌到期前仍有效。
/// </summary>
public class UpdateUserStatusCommandHandler(IUserRepository repository)
    : IRequestHandler<UpdateUserStatusCommand, ApiResponse>
{
    /// <summary>处理入口：启用/禁用账号：禁用后登录被拒（Login 校验 IsEnabled），已签发的令牌到期前仍有效。</summary>
    public async Task<ApiResponse> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await repository.GetByIdAsync(request.Id);
        if (user is null || user.IsDeleted)
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "用户不存在");

        user.IsEnabled = request.IsEnabled;
        user.UpdatedAt = DateTime.Now;
        await repository.UpdateAsync(user);
        return ApiResults.Ok(new { success = true });
    }
}
