using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using UserService.Application.Common;
using UserService.Domain.IRepository;

namespace UserService.Application.Features.User.GetProfile;

/// <summary>
/// 用户资料：wildcard（后台）可查任意 id，否则强制查当前登录人——防止越权拉取他人资料。
/// </summary>
public class GetProfileQueryHandler(IUserRepository repository, TenantContext tenant)
    : IRequestHandler<GetProfileQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        if (!tenant.HasWildcard && (tenant.UserId <= 0 || (!tenant.IsCustomer && !tenant.IsPlatform && !tenant.IsMerchant)))
            return ApiResults.Fail(BaseApiResponseCode.Unauthorized, "请先登录");

        var scopedUserId = tenant.HasWildcard ? request.Id : tenant.UserId;
        var user = await repository.GetByIdAsync(scopedUserId);
        if (user is null || user.IsDeleted)
            return ApiResults.Fail(BaseApiResponseCode.NotFound, "用户不存在");
        return ApiResults.Ok(UserShaper.Shape(user));
    }
}
