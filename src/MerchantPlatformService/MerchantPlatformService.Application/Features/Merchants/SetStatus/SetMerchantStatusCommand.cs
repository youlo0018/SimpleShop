using CommunalService.Domain;
using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.SetStatus;

/// <summary>商户状态变更（启用/停用，影响下单校验）。</summary>
/// <param name="Id">主键。</param>
/// <param name="Status">状态过滤。</param>
public record SetMerchantStatusCommand(long Id, int Status) : IRequest<ApiResponse>;
