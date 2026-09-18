using CommunalService.Domain;
using MediatR;

namespace CustomerService.Application.Features.Customer.Token;

/// <summary>刷新客户令牌（临近过期时前端调用）：换发新 jti 并删除旧会话。</summary>
public record RefreshTokenCommand : IRequest<ApiResponse>;
