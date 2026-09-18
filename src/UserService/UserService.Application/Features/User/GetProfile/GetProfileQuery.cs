using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.User.GetProfile;

/// <summary>后台账号资料查询（wildcard 可查任意 id，否则强制本人）。</summary>
/// <param name="Id">主键。</param>
public record GetProfileQuery(long Id = 0) : IRequest<ApiResponse>;
