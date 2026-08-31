using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Features.ConfirmPayment;
using PaymentService.Application.Features.CreatePayment;
using PaymentService.Application.Features.Payment.ApproveRefund;
using PaymentService.Application.Features.Payment.ListPayments;
using PaymentService.Application.Features.Payment.ListRefunds;
using PaymentService.Application.Features.Payment.RefundDetail;
using PaymentService.Application.Features.Payment.RejectRefund;
using PaymentService.Application.Features.RefundPayment;

namespace PaymentService.Api.Controllers;

/// <summary>
/// 支付/退款入口：控制器只做协议转换，业务规则（归属校验、状态机、事件发布）在 Application Handler。
/// </summary>
public class PaymentController(IMediator mediator) : BaseController
{
    [HttpGet]
    public Task<ApiResponse> Payments([FromQuery] ListPaymentsQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpGet]
    public Task<ApiResponse> Refunds([FromQuery] ListRefundsQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpGet("~/api/Payment/RefundDetail")]
    public Task<ApiResponse> RefundDetail([FromQuery] RefundDetailQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> Create([FromBody] CreatePaymentCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> Confirm([FromBody] ConfirmPaymentCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> Refund([FromBody] RefundPaymentCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> ApproveRefund([FromBody] ApproveRefundCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> RejectRefund([FromBody] RejectRefundCommand command)
        => mediator.Send(command, CancellationToken.None);
}
