using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using UserService.Application.Common;
using UserService.Domain.IRepository;

namespace UserService.Application.Features.User.ListUsers;

/// <summary>
/// 后台用户分页列表：keyword 模糊匹配用户名/手机号/邮箱；total 与取数同条件查询。
/// 平台管理员看全部用户（用户表不按平台切分，角色绑定在权限中心）。
/// </summary>
public class ListUsersQueryHandler(IUserRepository repository) : IRequestHandler<ListUsersQuery, ApiResponse>
{
    public async Task<ApiResponse> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await repository.QueryPagedAsync(request.Keyword, request.Role, request.Page, request.PageSize);
        return ApiResults.Ok(new
        {
            items = items.Select(user => UserShaper.Shape(user)),
            total,
            page = request.Page,
            pageSize = request.PageSize
        });
    }
}
