using CommunalService.Domain;
using MediatR;

namespace CustomerService.Application.Features.Address.DeleteAddress;

/// <summary>删除收货地址（软删除，只允许本人）。</summary>
/// <param name="Id">主键。</param>
public record DeleteAddressCommand(long Id) : IRequest<ApiResponse>;
