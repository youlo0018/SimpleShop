using CommunalService.Domain;
using MediatR;

namespace CustomerService.Application.Features.Address.ListAddresses;

/// <summary>客户地址簿查询（只允许本人，客户 ID 由登录态注入）。</summary>
public record ListAddressesQuery : IRequest<ApiResponse>;
