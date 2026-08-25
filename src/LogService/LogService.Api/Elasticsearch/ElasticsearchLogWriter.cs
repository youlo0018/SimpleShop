using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using LogService.Api.Models;
using Microsoft.Extensions.Options;

namespace LogService.Api.Elasticsearch;

/// <summary>
/// Elasticsearch 写入器：按天建索引、用 EventId 做 _id，重复消费只会覆盖同一现场。
/// ILM 策略由部署脚本统一声明；这里专注把每条日志写到正确的当天索引。
/// </summary>
public sealed class ElasticsearchLogWriter(
    IOptions<ElasticsearchOptions> options,
    ILogger<ElasticsearchLogWriter> logger)
{
    private readonly ElasticsearchClient _client = CreateClient(options.Value);

    public async Task WriteAsync(byte[] body, string logKind, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement.Clone();

            switch (logKind)
            {
                case "pv":
                    await IndexAsync(MapToPageView(root), "pageview", cancellationToken);
                    break;
                case "operation":
                    await IndexAsync(MapToOperation(root), "operation", cancellationToken);
                    break;
                default:
                    await IndexAsync(MapToException(root), "exception", cancellationToken);
                    break;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "写入 Elasticsearch 失败，日志类型：{LogKind}。", logKind);
            throw;
        }
    }

    public async Task<object> QueryAsync(string logKind, IQueryCollection query, CancellationToken cancellationToken)
    {
        var indexPattern = $"logs-{logKind}-*";
        var pageIndex = Math.Max(1, ParseInt(query["page"], 1));
        var pageSize = Math.Clamp(ParseInt(query["pageSize"], 20), 1, 200);
        var filters = BuildQueries(query);

        var response = await _client.SearchAsync<JsonElement>(request => request
            .Indices(new[] { indexPattern })
            .Query(queryDescriptor => queryDescriptor.Bool(boolQuery => boolQuery.Filter(filters)))
            .Size(pageSize)
            .From((pageIndex - 1) * pageSize), cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException($"Elasticsearch 查询失败：{response.DebugInformation}");
        }

        return new
        {
            page = pageIndex,
            pageSize,
            total = response.Total,
            items = response.Documents.ToList()
        };
    }

    private static List<Query> BuildQueries(IQueryCollection query)
    {
        var filters = new List<Query>();

        void AddTerm(string field, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                filters.Add(Query.Term(new TermQuery { Field = field, Value = value }));
            }
        }

        AddTerm("traceId", query["traceId"]);
        AddTerm("platformId", query["platformId"]);
        AddTerm("merchantId", query["merchantId"]);
        AddTerm("userId", query["userId"]);

        return filters;
    }

    private static PageViewLog MapToPageView(JsonElement root) => new()
    {
        EventId = root.GetProperty("eventId").GetGuid(),
        OccurredAt = root.GetProperty("occurredAt").GetDateTimeOffset(),
        TraceId = GetString(root, "traceId"),
        Service = GetString(root, "service"),
        Environment = GetString(root, "environment"),
        ServiceVersion = GetString(root, "serviceVersion"),
        PlatformId = GetInt64(root, "platformId"),
        MerchantId = GetInt64(root, "merchantId"),
        UserId = GetInt64(root, "userId"),
        Page = GetString(root.GetProperty("payload"), "page"),
        ItemId = GetInt64(root.GetProperty("payload"), "itemId"),
        SkuId = GetInt64(root.GetProperty("payload"), "skuId"),
        Source = GetString(root.GetProperty("payload"), "source"),
        StayMilliseconds = root.GetProperty("payload").TryGetProperty("stayMilliseconds", out var stay) ? stay.GetInt32() : 0
    };

    private static OperationLog MapToOperation(JsonElement root) => new()
    {
        EventId = root.GetProperty("eventId").GetGuid(),
        OccurredAt = root.GetProperty("occurredAt").GetDateTimeOffset(),
        TraceId = GetString(root, "traceId"),
        Service = GetString(root, "service"),
        Environment = GetString(root, "environment"),
        ServiceVersion = GetString(root, "serviceVersion"),
        PlatformId = GetInt64(root, "platformId"),
        MerchantId = GetInt64(root, "merchantId"),
        OperatorId = GetInt64(root, "userId"),
        OperationType = GetString(root.GetProperty("payload"), "operationType"),
        ObjectType = GetString(root.GetProperty("payload"), "objectType"),
        ObjectId = GetString(root.GetProperty("payload"), "objectId"),
        OperatorType = GetString(root.GetProperty("payload"), "operatorType"),
        Summary = GetString(root.GetProperty("payload"), "summary"),
        Ip = GetString(root.GetProperty("payload"), "ip")
    };

    private static ExceptionLog MapToException(JsonElement root) => new()
    {
        EventId = root.GetProperty("eventId").GetGuid(),
        OccurredAt = root.GetProperty("occurredAt").GetDateTimeOffset(),
        TraceId = GetString(root, "traceId"),
        Service = GetString(root, "service"),
        Environment = GetString(root, "environment"),
        ServiceVersion = GetString(root, "serviceVersion"),
        PlatformId = GetInt64(root, "platformId"),
        MerchantId = GetInt64(root, "merchantId"),
        UserId = GetInt64(root, "userId"),
        Path = GetString(root.GetProperty("payload"), "path"),
        Method = GetString(root.GetProperty("payload"), "method"),
        StatusCode = root.GetProperty("payload").TryGetProperty("statusCode", out var status) ? status.GetInt32() : 500,
        ExceptionType = GetString(root.GetProperty("payload"), "exceptionType"),
        Message = GetString(root.GetProperty("payload"), "message"),
        StackTrace = GetString(root.GetProperty("payload"), "stackTrace")
    };

    private static string GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static long GetInt64(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt64() : 0;

    private static int ParseInt(string? value, int defaultValue)
        => int.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static ElasticsearchClient CreateClient(ElasticsearchOptions settings)
    {
        var configuration = new ElasticsearchClientSettings(new Uri(settings.Url))
            .DefaultMappingFor<PageViewLog>(mapping => mapping.IndexName("logs-pageview"))
            .DefaultMappingFor<OperationLog>(mapping => mapping.IndexName("logs-operation"))
            .DefaultMappingFor<ExceptionLog>(mapping => mapping.IndexName("logs-exception"));

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            configuration.Authentication(new BasicAuthentication(settings.Username, settings.Password));
        }

        return new ElasticsearchClient(configuration);
    }

    private async Task IndexAsync<TDocument>(TDocument document, string kind, CancellationToken cancellationToken)
    {
        var documentType = typeof(TDocument);
        var eventId = (Guid)(documentType.GetProperty("EventId")?.GetValue(document) ?? Guid.Empty);
        var occurredAt = (DateTimeOffset)(documentType.GetProperty("OccurredAt")?.GetValue(document) ?? DateTimeOffset.UtcNow);
        var indexName = $"logs-{kind}-{occurredAt.UtcDateTime:yyyy.MM.dd}";

        // 用事件 ID 作为文档 ID，MQ 重投或服务重放时不会生成重复日志。
        var response = await _client.IndexAsync(
            document,
            request => request
                .Index(indexName)
                .Id(eventId.ToString("D")),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException($"Elasticsearch 写入失败：{response.DebugInformation}");
        }
    }
}
