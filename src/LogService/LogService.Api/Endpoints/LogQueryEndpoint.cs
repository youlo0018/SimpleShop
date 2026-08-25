using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using LogService.Api.Elasticsearch;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace LogService.Api.Endpoints;

/// <summary>
/// 日志查询接口：Kibana 负责深度排障，这里给运营/运维提供轻量的 TraceId 和业务范围检索。
/// API Key 只作为网关后的第一道门，生产环境可再叠加平台权限。
/// </summary>
public static class LogQueryEndpoint
{
    public static IEndpointRouteBuilder MapLogQueryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/log/{kind}", async (
            string kind,
            HttpContext context,
            ElasticsearchLogWriter writer,
            IConfiguration configuration) =>
        {
            var apiKey = configuration["LoggingApi:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey) &&
                context.Request.Headers["X-Api-Key"].ToString() != apiKey)
            {
                return Results.Unauthorized();
            }

            if (kind is not ("pv" or "operation" or "exception"))
            {
                return Results.BadRequest(new { message = "日志类型只支持 pv、operation、exception。" });
            }

            return Results.Ok(await writer.QueryAsync(kind, context.Request.Query, context.RequestAborted));
        }).WithTags("Logs");

        return endpoints;
    }
}
