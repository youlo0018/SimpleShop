using CommunalService.Domain;
using MediatR;

namespace CustomerService.Application.Features.Customer.SaveProfile;

/// <summary>客户资料编辑：头像、性别、生日、邮箱、手机号（登录名与平台不可改）。</summary>
public record SaveProfileCommand(string Avatar, int Gender, DateTime? Birth, string Email, string Phone) : IRequest<ApiResponse>;
