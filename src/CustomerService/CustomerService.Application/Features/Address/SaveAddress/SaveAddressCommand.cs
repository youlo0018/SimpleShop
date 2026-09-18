using CommunalService.Domain;
using MediatR;

namespace CustomerService.Application.Features.Address.SaveAddress;

/// <summary>新增/编辑收货地址（Id=0 新增；客户 ID 由登录态注入，不接受请求体伪造）。</summary>
/// <param name="Id">主键。</param>
/// <param name="ReceiverName">收货人。</param>
/// <param name="ReceiverPhone">收货电话。</param>
public record SaveAddressCommand(long Id, string ReceiverName, string ReceiverPhone,
    string Province, string City, string District, string Detail, bool IsDefault) : IRequest<ApiResponse>;
