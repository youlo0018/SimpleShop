using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.User.ListUsers;

/// <summary>后台账号分页查询：关键字模糊匹配用户名/手机号/邮箱。</summary>
/// <param name="Keyword">关键字（模糊匹配）。</param>
/// <param name="Page">页码（从 1 开始）。</param>
/// <param name="PageSize">每页条数。</param>
/// <param name="Role">角色编码（权限中心）。</param>
public record ListUsersQuery(string Keyword = "", int Page = 1, int PageSize = 10) : IRequest<ApiResponse>;
