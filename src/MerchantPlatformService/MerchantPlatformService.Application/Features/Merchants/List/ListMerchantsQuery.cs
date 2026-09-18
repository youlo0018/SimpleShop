using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.List;

/// <summary>后台商户分页查询（平台账号限本平台，商户账号限本商户，由 Handler 按登录态裁剪）。</summary>
public sealed record ListMerchantsQuery : IRequest<object>
{
    /// <summary>关键字：商户名/联系人/联系电话模糊匹配。</summary>
    public string Keyword { get; init; } = string.Empty;

    /// <summary>审核状态过滤（null 不过滤）。</summary>
    public int? Status { get; init; }

    /// <summary>平台过滤（0 不过滤；平台账号会被强制回填本平台）。</summary>
    public long PlatformId { get; init; }

    /// <summary>页码，从 1 开始。</summary>
    public int Page { get; init; } = 1;

    /// <summary>每页条数（1-100）。</summary>
    public int PageSize { get; init; } = 10;
}
