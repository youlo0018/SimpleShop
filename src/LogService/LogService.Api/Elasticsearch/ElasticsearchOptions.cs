namespace LogService.Api.Elasticsearch;

/// <summary>
/// Elasticsearch 连接配置。默认指向本机单节点，生产环境用配置中心覆盖。
/// </summary>
public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string Url { get; set; } = "http://localhost:9200";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
