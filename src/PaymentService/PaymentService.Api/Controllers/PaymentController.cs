using CommunalService.Domain;
using MediatR;
using CommunalService.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using CommunalService.Domain.Messaging;
using PaymentService.Application.Features.ConfirmPayment;
using PaymentService.Application.Features.CreatePayment;
using PaymentService.Application.Features.RefundPayment;
using FreeSql;
using PaymentService.Domain.Entity;

namespace PaymentService.Api.Controllers;

public sealed record PaymentListQuery(string Keyword = "", int? Status = null, long UserId = 0, int Page = 1, int PageSize = 10);

public class PaymentController(
    IMediator mediator,
    IFreeSql freeSql,
    TenantContext tenant,
    PaymentService.Domain.IRepository.IPaymentOrderRepository paymentRepository,
    CommunalService.Domain.Messaging.IMessagePublisher messagePublisher) : BaseController
{
    [HttpGet]
    public async Task<ApiResponse> Payments([FromQuery] PaymentListQuery query)
    {
        var selection = freeSql.Select<PaymentOrder>()
            .Where(payment => !payment.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Keyword), payment =>
                payment.PaymentNo.Contains(query.Keyword) || payment.BizNo.Contains(query.Keyword))
            .WhereIf(query.Status.HasValue, payment => payment.Status == query.Status!.Value)
            .WhereIf(query.UserId > 0, payment => payment.UserId == query.UserId);
        if (tenant.IsPlatform) selection = selection.Where(payment => payment.PlatformId == tenant.PlatformId);
        if (tenant.IsMerchant) selection = selection.Where(payment => payment.MerchantId == tenant.MerchantId);
        if (tenant.IsCustomer) selection = selection.Where(payment => payment.UserId == tenant.UserId);
        if (!tenant.HasWildcard && !tenant.IsPlatform && !tenant.IsMerchant && !tenant.IsCustomer)
            return Error(BaseApiResponseCode.Unauthorized, "请先登录");
        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(payment => payment.CreatedAt)
            .Page((Math.Max(query.Page, 1) - 1) * query.PageSize, query.PageSize).ToListAsync();
        return Ok(new { items, total, page = query.Page, pageSize = query.PageSize });
    }

    [HttpGet]
    public async Task<ApiResponse> Refunds([FromQuery] PaymentListQuery query)
    {
        var selection = freeSql.Select<RefundOrder>()
            .Where(refund => !refund.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Keyword), refund =>
                refund.RefundNo.Contains(query.Keyword) || refund.BizNo.Contains(query.Keyword))
            .WhereIf(query.Status.HasValue, refund => refund.Status == query.Status!.Value)
            .WhereIf(query.UserId > 0, refund => refund.UserId == query.UserId);
        if (tenant.IsPlatform) selection = selection.Where(refund => refund.PlatformId == tenant.PlatformId);
        if (tenant.IsMerchant) selection = selection.Where(refund => refund.MerchantId == tenant.MerchantId);
        if (tenant.IsCustomer) selection = selection.Where(refund => refund.UserId == tenant.UserId);
        if (!tenant.HasWildcard && !tenant.IsPlatform && !tenant.IsMerchant && !tenant.IsCustomer)
            return Error(BaseApiResponseCode.Unauthorized, "请先登录");
        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(refund => refund.CreatedAt)
            .Page((Math.Max(query.Page, 1) - 1) * query.PageSize, query.PageSize).ToListAsync();
        return Ok(new { items, total, page = query.Page, pageSize = query.PageSize });
    }

    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] CreatePaymentCommand command)
    {
        if (tenant.UserId <= 0) return Error(BaseApiResponseCode.Unauthorized, "请先登录");

        // 支付单的归属与订单保持一致；这里强制覆盖请求体，避免用户指定他人 userId。
        command = command with { UserId = tenant.UserId };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpPost]
    public async Task<ApiResponse> Confirm([FromBody] ConfirmPaymentCommand command)
    {
        var selection = freeSql.Select<PaymentOrder>().Where(payment => payment.BizNo == command.BizNo && !payment.IsDeleted);
        if (tenant.IsCustomer) selection = selection.Where(payment => payment.UserId == tenant.UserId);
        if (!await selection.AnyAsync()) return Error(BaseApiResponseCode.Forbidden, "无权确认该支付单");
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpPost]
    public async Task<ApiResponse> Refund([FromBody] RefundPaymentCommand command)
    {
        var selection = freeSql.Select<PaymentOrder>().Where(payment => payment.BizNo == command.BizNo && !payment.IsDeleted)
            .WhereIf(tenant.IsPlatform, payment => payment.PlatformId == tenant.PlatformId)
            .WhereIf(tenant.IsMerchant, payment => payment.MerchantId == tenant.MerchantId)
            .WhereIf(tenant.IsCustomer, payment => payment.UserId == tenant.UserId);
        if (!await selection.AnyAsync()) return Error(BaseApiResponseCode.Forbidden, "无权操作该支付单");
        var result = await mediator.Send(command, CancellationToken.None);
        var successProperty = result?.GetType().GetProperty("success");
        if (result is not null && successProperty?.GetValue(result) is bool success && !success)
        {
            var messageProperty = result.GetType().GetProperty("message");
            var message = messageProperty?.GetValue(result)?.ToString() ?? "退款申请失败";
            return Error(BaseApiResponseCode.BadRequest, message);
        }
        return result is null ? Error(BaseApiResponseCode.BadRequest, "退款申请失败") : Ok(result);
    }

    // 路由使用绝对路径，避免控制器基类路由和当前方法名共同拼接出双重 RefundDetail。
    [HttpGet("~/api/Payment/RefundDetail")]
    public async Task<ApiResponse> RefundDetail([FromQuery] long id)
    {
        var selection = freeSql.Select<RefundOrder>().Where(refund => refund.Id == id && !refund.IsDeleted);
        scope(selection);
        var refund = await selection.FirstAsync();
        if (refund is null) return Error(BaseApiResponseCode.NotFound, "退款单不存在");

        return Ok(new
        {
            refund,
            items = await freeSql.Select<RefundOrderItem>().Where(item => item.RefundId == id).ToListAsync(),
            allRefunds = await freeSql.Select<RefundOrder>()
                .Where(item => item.BizNo == refund.BizNo && !item.IsDeleted)
                .OrderByDescending(item => item.CreatedAt).ToListAsync()
        });
    }

    [HttpPost]
    public async Task<ApiResponse> ApproveRefund([FromBody] RefundDecisionRequest request)
    {
        var selection = freeSql.Select<RefundOrder>().Where(refund => refund.Id == request.Id && !refund.IsDeleted);
        scope(selection);
        var refund = await selection.FirstAsync();
        if (refund is null) return Error(BaseApiResponseCode.NotFound, "退款单不存在");
        if (!await CanDecideRefundAsync(refund)) return Error(BaseApiResponseCode.Forbidden, "无权审批该退款单");
        if (refund.Status != 10) return Error(BaseApiResponseCode.BadRequest, "当前退款状态不可审批");

        var updated = await paymentRepository.MarkRefundedAsync(refund.RefundNo);
        if (!updated) return Error(BaseApiResponseCode.BadRequest, "退款状态更新失败");

        refund.Status = 20;
        refund.RefundedAt = DateTime.Now;
        var refundItems = await freeSql.Select<RefundOrderItem>().Where(item => item.RefundId == refund.Id).ToListAsync();
        var isAllRefund = (await paymentRepository.GetRefundedAmountAsync(refund.PaymentId)) >= 
            (await freeSql.Select<PaymentOrder>().Where(payment => payment.Id == refund.PaymentId).FirstAsync()).Amount;

        await messagePublisher.PublishAsync("payment.refunded", refund.RefundNo,
            new MessageEnvelope<object>(Guid.NewGuid(), "payment.refunded", DateTimeOffset.UtcNow, refund.RefundNo,
                refund.PlatformId, refund.MerchantId, refund.UserId, 1,
                new { bizNo = refund.BizNo, refundNo = refund.RefundNo, amount = refund.Amount, isAllRefund,
                    stockItems = refundItems.Select(item => new { item.SkuId, item.Quantity }) }));
        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<ApiResponse> RejectRefund([FromBody] RefundDecisionRequest request)
    {
        var selection = freeSql.Select<RefundOrder>().Where(refund => refund.Id == request.Id && !refund.IsDeleted);
        scope(selection);
        var refund = await selection.FirstAsync();
        if (refund is null) return Error(BaseApiResponseCode.NotFound, "退款单不存在");
        if (!await CanDecideRefundAsync(refund)) return Error(BaseApiResponseCode.Forbidden, "无权审批该退款单");
        if (refund.Status != 10) return Error(BaseApiResponseCode.BadRequest, "当前退款状态不可审批");

        var updated = await freeSql.Update<RefundOrder>().Where(item => item.Id == refund.Id)
            .Set(item => item.Status, 90)
            .Set(item => item.Reason, string.IsNullOrWhiteSpace(request.Reason) ? "审核不通过" : request.Reason)
            .ExecuteAffrowsAsync() > 0;
        return updated ? Ok(new { success = true }) : Error(BaseApiResponseCode.BadRequest, "退款状态更新失败");
    }

    private void scope(ISelect<RefundOrder> selection)
    {
        if (tenant.IsPlatform) selection.Where(refund => refund.PlatformId == tenant.PlatformId);
        if (tenant.IsMerchant) selection.Where(refund => refund.MerchantId == tenant.MerchantId);
        if (tenant.IsCustomer) selection.Where(refund => refund.UserId == tenant.UserId);
    }

    private async Task<bool> CanDecideRefundAsync(RefundOrder refund)
        => tenant.HasWildcard || tenant.IsPlatform || (tenant.IsMerchant && refund.MerchantId == tenant.MerchantId);
}

public sealed record RefundDecisionRequest(long Id, string? Reason = null);
