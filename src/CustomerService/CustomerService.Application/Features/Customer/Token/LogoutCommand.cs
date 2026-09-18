using CommunalService.Domain;
using MediatR;

namespace CustomerService.Application.Features.Customer.Token;

/// <summary>客户登出：删除 Redis 会话，令牌立即失效（不依赖前端清本地存储）。</summary>
public record LogoutCommand : IRequest<ApiResponse>;
