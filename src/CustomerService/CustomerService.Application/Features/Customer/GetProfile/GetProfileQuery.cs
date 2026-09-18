using CommunalService.Domain;
using MediatR;

namespace CustomerService.Application.Features.Customer.GetProfile;

/// <summary>客户资料查询（只允许本人，客户 ID 由登录态注入）。</summary>
public record GetProfileQuery : IRequest<ApiResponse>;
