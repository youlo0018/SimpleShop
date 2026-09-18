namespace LogService.Api.Elasticsearch;

/// <summary>
/// Elasticsearch 连接配置。默认指向本机单节点，生产环境用配置中心覆盖。
/// </summary>
public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    /// <summary>Elasticsearch 地址。</summary>
    public string Url { get; set; } = "http://localhost:9200";

    /// <summary>ES 用户名。</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>密码（仅传输明文，落库为加盐散列）。</summary>
    public string Password { get; set; } = string.Empty;
}
