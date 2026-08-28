using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MerchantPlatformService.Application.Features.Merchants.Create;
using MerchantPlatformService.Application.Features.Merchants.Get;
using MerchantPlatformService.Application.Features.Merchants.Review;
using FreeSql;
using MerchantPlatformService.Domain.Entity;
using MerchantPlatformService.Domain.Enums;
using System.Text.RegularExpressions;

namespace MerchantPlatformService.Api.Controllers;

public sealed record MerchantListQuery(string Keyword = "", int? Status = null, long PlatformId = 0, int Page = 1, int PageSize = 10);
public sealed record SaveMerchantRequest(long Id, long PlatformId, string MerchantName, string ContactName,
    string ContactPhone, string ContactEmail, decimal CommissionRate);

public class MerchantController(IMediator mediator, IFreeSql freeSql, TenantContext tenant) : BaseController
{
    [HttpGet]
    public async Task<ApiResponse> List([FromQuery] MerchantListQuery query)
    {
        var selection = freeSql.Select<Merchant>()
            .Where(merchant => !merchant.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Keyword), merchant =>
                merchant.MerchantName.Contains(query.Keyword) || merchant.ContactName.Contains(query.Keyword) || merchant.ContactPhone.Contains(query.Keyword))
            .WhereIf(query.Status.HasValue, merchant => merchant.Status == query.Status!.Value)
            .WhereIf(query.PlatformId > 0, merchant => merchant.PlatformId == query.PlatformId);
        if (tenant.IsPlatform) selection = selection.Where(merchant => merchant.PlatformId == tenant.PlatformId);
        if (tenant.IsMerchant) selection = selection.Where(merchant => merchant.Id == tenant.MerchantId && merchant.PlatformId == tenant.PlatformId);
        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(merchant => merchant.CreatedAt)
            .Page((Math.Max(query.Page, 1) - 1) * query.PageSize, query.PageSize).ToListAsync();
        return Ok(new { items, total, page = query.Page, pageSize = query.PageSize });
    }

    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] CreateMerchantCommand command)
    {
        if (tenant.IsMerchant) return Error(BaseApiResponseCode.Forbidden, "商户账号不能创建商户");
        command = command with { PlatformId = tenant.IsPlatform ? tenant.PlatformId : command.PlatformId };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpGet]
    public async Task<ApiResponse> Get([FromQuery] GetMerchantQuery query)
        => Ok(await mediator.Send(query, CancellationToken.None));

    [HttpPost]
    public async Task<ApiResponse> Review([FromBody] MerchantReviewRequest request)
    {
        var selection = freeSql.Select<Merchant>().Where(merchant => merchant.Id == request.Id && !merchant.IsDeleted)
            .WhereIf(tenant.IsPlatform, merchant => merchant.PlatformId == tenant.PlatformId)
            .WhereIf(tenant.IsMerchant, merchant => merchant.Id == tenant.MerchantId);
        if (!await selection.AnyAsync()) return Error(BaseApiResponseCode.Forbidden, "无权操作该商户");

        // 兼容旧前端传状态数字；新前端可以只传 Approved，最终都转为明确审核结论。
        var approved = request.Approved ?? request.Status == (int)MerchantStatus.Approved;
        var command = new ReviewMerchantCommand { Id = request.Id, Approved = approved, Reason = request.Reason };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    [HttpPost]
    public async Task<ApiResponse> Update([FromBody] SaveMerchantRequest request)
    {
        if (ValidateMerchant(request) is { } invalid) return invalid;
        var merchant = await freeSql.Select<Merchant>().Where(item => item.Id == request.Id && !item.IsDeleted)
            .WhereIf(tenant.IsPlatform, item => item.PlatformId == tenant.PlatformId).FirstAsync();
        if (merchant is null) return Error(BaseApiResponseCode.NotFound, "商户不存在");
        merchant.MerchantName = request.MerchantName;
        merchant.ContactName = request.ContactName;
        merchant.ContactPhone = request.ContactPhone;
        merchant.ContactEmail = request.ContactEmail;
        merchant.CommissionRate = request.CommissionRate;
        await freeSql.Update<Merchant>().SetSource(merchant).ExecuteAffrowsAsync();
        return Ok(new { success = true });
    }

    private static ApiResponse? ValidateMerchant(SaveMerchantRequest request)
    {
        if (request.PlatformId <= 0)
            return new ApiResponse { Code = 400, Message = "必须选择有效平台", Data = new { errors = new { platformId = new[] { "必须选择有效平台" } } } };
        if (string.IsNullOrWhiteSpace(request.MerchantName) || request.MerchantName.Length > 64)
            return new ApiResponse { Code = 400, Message = "商户名称必须为2-64个字符", Data = new { errors = new { merchantName = new[] { "商户名称必须为2-64个字符" } } } };
        if (string.IsNullOrWhiteSpace(request.ContactName) || request.ContactName.Length > 32)
            return new ApiResponse { Code = 400, Message = "联系人不能超过32个字符", Data = new { errors = new { contactName = new[] { "联系人不能超过32个字符" } } } };
        if (!Regex.IsMatch(request.ContactPhone, "^1[3-9]\\d{9}$"))
            return new ApiResponse { Code = 400, Message = "手机号格式不正确", Data = new { errors = new { contactPhone = new[] { "手机号格式不正确" } } } };
        if (!Regex.IsMatch(request.ContactEmail, "^[^\\s@]+@[^\\s@]+\\.[^\\s@]{2,}$"))
            return new ApiResponse { Code = 400, Message = "邮箱格式不正确", Data = new { errors = new { contactEmail = new[] { "邮箱格式不正确" } } } };
        if (request.CommissionRate is < 0 or > 100)
            return new ApiResponse { Code = 400, Message = "佣金率必须在0-100之间", Data = new { errors = new { commissionRate = new[] { "佣金率必须在0-100之间" } } } };
        return null;
    }

    [HttpPost]
    public async Task<ApiResponse> SetStatus([FromBody] SetMerchantStatusRequest request)
    {
        var updated = await freeSql.Update<Merchant>().Where(item => item.Id == request.Id && !item.IsDeleted)
            .WhereIf(tenant.IsPlatform, item => item.PlatformId == tenant.PlatformId)
            .Set(item => item.Status, request.Status).ExecuteAffrowsAsync() > 0;
        return updated ? Ok(new { success = true }) : Error(BaseApiResponseCode.NotFound, "商户不存在");
    }
}

public sealed record MerchantReviewRequest(long Id, bool? Approved = null, int? Status = null, string? Reason = null);
public sealed record SetMerchantStatusRequest(long Id, int Status);
