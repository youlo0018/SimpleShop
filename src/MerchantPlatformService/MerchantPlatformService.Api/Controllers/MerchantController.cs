using CommunalService.Domain;
using CommunalService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MerchantPlatformService.Application.Features.Merchants.Create;
using MerchantPlatformService.Application.Features.Merchants.SetStatus;
using MerchantPlatformService.Application.Features.Merchants.Update;
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
        // 该控制器为分页直查例外，分页参数在此兜底，避免一次拉取过大结果集。
        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            return Error(BaseApiResponseCode.BadRequest, "分页参数不正确（页码≥1，每页1-100）");
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
            .Page(Math.Max(query.Page, 1), query.PageSize).ToListAsync();
        return Ok(new { items, total, page = query.Page, pageSize = query.PageSize });
    }

    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] CreateMerchantCommand command)
    {
        if (tenant.IsMerchant) return Error(BaseApiResponseCode.Forbidden, "商户账号不能创建商户");
        command = command with { PlatformId = tenant.IsPlatform ? tenant.PlatformId : command.PlatformId };
        return Ok(await mediator.Send(command, CancellationToken.None));
    }

    /// <summary>
    /// 店铺主页公开信息（游客可访问）：只返回已入驻商户的展示字段，不含联系人/佣金等经营数据。
    /// 商城店铺页头部使用；商品列表走 /products/List?merchantId= 单独查询。
    /// </summary>
    [HttpGet]
    public async Task<ApiResponse> Shop([FromQuery] long id)
    {
        if (id <= 0) return Error(BaseApiResponseCode.BadRequest, "商户不能为空");
        var merchant = await freeSql.Select<Merchant>()
            .Where(item => item.Id == id && item.Status == (int)MerchantStatus.Approved)
            .FirstAsync();
        if (merchant is null) return Error(BaseApiResponseCode.NotFound, "店铺不存在或未营业");
        return Ok(new { merchant.Id, merchant.MerchantName, merchant.PlatformId, merchant.Status, merchant.CreatedAt });
    }

    [HttpGet]
    public async Task<ApiResponse> Get([FromQuery] GetMerchantQuery query)
        => Ok(await mediator.Send(query, CancellationToken.None));

    [HttpPost]
    public async Task<ApiResponse> Review([FromBody] MerchantReviewRequest request)
    {
        // 审核结论必须显式给出，避免两个字段都缺省时被当成"拒绝"误处理；其余字段由 ReviewMerchantValidator 校验。
        if (request.Id <= 0) return Error(BaseApiResponseCode.BadRequest, "商户不能为空");
        if (request.Approved is null && request.Status is null)
            return Error(BaseApiResponseCode.BadRequest, "请提交审核结论");
        // 兼容旧前端传状态数字；新前端可以只传 Approved，最终都转为明确审核结论。
        var approved = request.Approved ?? request.Status == (int)MerchantStatus.Approved;
        return Ok(await mediator.Send(new ReviewMerchantCommand { Id = request.Id, Approved = approved, Reason = request.Reason }, CancellationToken.None));
    }

    [HttpPost]
    public Task<ApiResponse> Update([FromBody] UpdateMerchantCommand command)
        => mediator.Send(command, CancellationToken.None);

    [HttpPost]
    public Task<ApiResponse> SetStatus([FromBody] SetMerchantStatusCommand command)
        => mediator.Send(command, CancellationToken.None);
}

public sealed record MerchantReviewRequest(long Id, bool? Approved = null, int? Status = null, string? Reason = null);
public sealed record SetMerchantStatusRequest(long Id, int Status);
