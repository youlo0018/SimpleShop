using CommunalService.Domain;
using MediatR;

namespace MerchantPlatformService.Application.Features.Platforms.List;

/// <summary>平台分页查询（后台）。</summary>
/// <param name="Keyword">关键字（模糊匹配）。</param>
/// <param name="Page">页码（从 1 开始）。</param>
/// <param name="PageSize">每页条数。</param>
public record ListPlatformsQuery(string Keyword = "", int Page = 1, int PageSize = 10) : IRequest<ApiResponse>;
