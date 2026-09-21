using MediatR;

namespace MerchantPlatformService.Application.Features.Merchants.List;

/// <summary>后台商户分页查询（平台账号限本平台，商户账号限本商户，由 Handler 按登录态裁剪）。</summary>
/// <param name="Keyword">关键字：商户名/联系人/联系电话模糊匹配（空字符串不过滤）。</param>
/// <param name="Status">审核状态过滤（null 不过滤）。</param>
/// <param name="PlatformId">平台过滤（0 不过滤；平台账号会被强制回填本平台）。</param>
/// <param name="Page">页码，从 1 开始。</param>
/// <param name="PageSize">每页条数（1-100）。</param>
public sealed record ListMerchantsQuery(string Keyword = "", int? Status = null, long PlatformId = 0, int Page = 1, int PageSize = 10) : IRequest<object>;
