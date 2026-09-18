using CommunalService.Domain;
using CustomerService.Domain.Enums;
using MediatR;

namespace CustomerService.Application.Features.Customer.Register;

/// <summary>
/// 客户注册（C 端唯一注册入口，账号按平台隔离）：
/// 必须选择平台、同意用户协议，并声明注册来源（小程序默认 1）。
/// </summary>
public record RegisterCommand(
    string UserName,
    string Password,
    string Email,
    string Phone,
    long PlatformId,
    bool AgreedAgreement,
    int RegisterSource = (int)RegisterSource.MiniApp) : IRequest<ApiResponse>;
