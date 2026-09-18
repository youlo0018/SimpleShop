using CommunalService.Domain;
using MediatR;

namespace CustomerService.Application.Features.Customer.Login;

/// <summary>客户登录（C 端唯一登录入口，账号按平台隔离）。</summary>
/// <param name="UserName">登录名。</param>
/// <param name="Password">密码（落库散列）。</param>
/// <param name="PlatformId">平台 ID。</param>
public record LoginCommand(string UserName, string Password, long PlatformId) : IRequest<ApiResponse>;
