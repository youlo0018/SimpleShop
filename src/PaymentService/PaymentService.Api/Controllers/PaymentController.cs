using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Features.ConfirmPayment;
using PaymentService.Application.Features.CreatePayment;
using PaymentService.Application.Features.RefundPayment;

namespace PaymentService.Api.Controllers;

public class PaymentController(IMediator mediator) : BaseController
{
    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] CreatePaymentCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPost]
    public async Task<ApiResponse> Confirm([FromBody] ConfirmPaymentCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPost]
    public async Task<ApiResponse> Refund([FromBody] RefundPaymentCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));
}


