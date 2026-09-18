using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.User.UpdateUserStatus;

/// <summary>后台账号启停：禁用后禁止登录。</summary>
/// <param name="Id">主键。</param>
/// <param name="IsEnabled">是否启用。</param>
public record UpdateUserStatusCommand(long Id, bool IsEnabled) : IRequest<ApiResponse>;
