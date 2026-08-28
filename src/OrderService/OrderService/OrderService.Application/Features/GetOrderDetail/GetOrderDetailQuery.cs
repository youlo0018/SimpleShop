using MediatR;

namespace OrderService.Application.Features.GetOrderDetail;

public sealed record GetOrderDetailQuery(long Id, long CustomerId, long PlatformId = 0, long MerchantId = 0) : IRequest<object>;
