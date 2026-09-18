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
    /// <summary>内部处理：Payments。</summary>
    public Task<ApiResponse> Payments([FromQuery] ListPaymentsQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpGet]
    /// <summary>内部处理：Refunds。</summary>
    public Task<ApiResponse> Refunds([FromQuery] ListRefundsQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpGet("~/api/Payment/RefundDetail")]
    /// <summary>内部处理：RefundDetail。</summary>
    public Task<ApiResponse> RefundDetail([FromQuery] RefundDetailQuery query)
        => mediator.Send(query, CancellationToken.None);

    [HttpPost]
    /// <summary>写操作：Create（副作用与幂等键见调用方约定）。</summary>
    public Task<ApiResponse> Create([FromBody] CreatePaymentCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    /// <summary>内部处理：Confirm。</summary>
    public Task<ApiResponse> Confirm([FromBody] ConfirmPaymentCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    /// <summary>内部处理：Refund。</summary>
    public Task<ApiResponse> Refund([FromBody] RefundPaymentCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    /// <summary>内部处理：ApproveRefund。</summary>
    public Task<ApiResponse> ApproveRefund([FromBody] ApproveRefundCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    /// <summary>内部处理：RejectRefund。</summary>
    public Task<ApiResponse> RejectRefund([FromBody] RejectRefundCommand command)
        => mediator.Send(command, CancellationToken.None);
}
