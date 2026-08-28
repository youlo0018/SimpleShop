using CommunalService.Domain;
using CommunalService.Domain.Enums;
using FreeSql;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using MerchantPlatformService.Application.Features.Platforms.Create;
using MerchantPlatformService.Application.Features.Platforms.Get;
using MerchantPlatformService.Domain.Entity;

namespace MerchantPlatformService.Api.Controllers;

public class PlatformController(IMediator mediator, IFreeSql freeSql, TenantContext tenant) : BaseController
{
    [HttpGet]
    public async Task<ApiResponse> List([FromQuery] string keyword = "", int page = 1, int pageSize = 10)
    {
        var selection = freeSql.Select<Platform>()
            .Where(platform => !platform.IsDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(keyword), platform =>
                platform.PlatformCode.Contains(keyword) || platform.PlatformName.Contains(keyword));
        if (tenant.IsPlatform) selection = selection.Where(platform => platform.Id == tenant.PlatformId);
        if (tenant.IsMerchant)
        {
            var platformId = await freeSql.Select<Merchant>()
                .Where(merchant => merchant.Id == tenant.MerchantId && !merchant.IsDeleted)
                .FirstAsync(merchant => merchant.PlatformId);
            selection = selection.Where(platform => platform.Id == platformId);
        }

        var total = await selection.CountAsync();
        var items = await selection.OrderByDescending(platform => platform.CreatedAt)
            .Page((Math.Max(page, 1) - 1) * pageSize, pageSize)
            .ToListAsync();
        return Ok(new { items, total, page, pageSize });
    }

    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] CreatePlatformCommand command)
        => Ok(await mediator.Send(command, CancellationToken.None));

    [HttpPut("{id}")]
    public async Task<ApiResponse> Edit([FromRoute] long id, [FromBody] SavePlatformRequest request)
    {
        if (ValidatePlatform(request) is { } invalid) return invalid;
        var platform = await freeSql.Select<Platform>().Where(item => item.Id == id && !item.IsDeleted).FirstAsync();
        if (platform is null) return Error(BaseApiResponseCode.NotFound, "平台不存在");
        platform.PlatformCode = request.PlatformCode;
        platform.PlatformName = request.PlatformName;
        platform.ContactEmail = request.ContactEmail;
        platform.DefaultCommissionRate = request.DefaultCommissionRate;
        await freeSql.Update<Platform>().SetSource(platform).ExecuteAffrowsAsync();
        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<ApiResponse> SetEnabled([FromBody] SetPlatformEnabledRequest request)
    {
        var updated = await freeSql.Update<Platform>().Where(item => item.Id == request.Id && !item.IsDeleted)
            .Set(item => item.IsEnabled, request.IsEnabled).ExecuteAffrowsAsync() > 0;
        return updated ? Ok(new { success = true }) : Error(BaseApiResponseCode.NotFound, "平台不存在");
    }

    [HttpGet]
    public async Task<ApiResponse> Get([FromQuery] GetPlatformQuery query)
        => Ok(await mediator.Send(query, CancellationToken.None));

    private static ApiResponse? ValidatePlatform(SavePlatformRequest request)
    {
        if (!Regex.IsMatch(request.PlatformCode, "^[a-zA-Z][a-zA-Z0-9_-]{2,31}$"))
            return new ApiResponse { Code = 400, Message = "平台编码格式不正确", Data = new { errors = new { platformCode = new[] { "平台编码格式不正确" } } } };
        if (string.IsNullOrWhiteSpace(request.PlatformName) || request.PlatformName.Length > 64)
            return new ApiResponse { Code = 400, Message = "平台名称必须为2-64个字符", Data = new { errors = new { platformName = new[] { "平台名称必须为2-64个字符" } } } };
        if (!Regex.IsMatch(request.ContactEmail, "^[^\\s@]+@[^\\s@]+\\.[^\\s@]{2,}$"))
            return new ApiResponse { Code = 400, Message = "邮箱格式不正确", Data = new { errors = new { contactEmail = new[] { "邮箱格式不正确" } } } };
        if (request.DefaultCommissionRate is < 0 or > 100)
            return new ApiResponse { Code = 400, Message = "佣金率必须在0-100之间", Data = new { errors = new { defaultCommissionRate = new[] { "佣金率必须在0-100之间" } } } };
        return null;
    }
}

public sealed record SavePlatformRequest(string PlatformCode, string PlatformName, string ContactEmail, decimal DefaultCommissionRate);
public sealed record SetPlatformEnabledRequest(long Id, bool IsEnabled);
