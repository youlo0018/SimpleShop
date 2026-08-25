using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CommunalService.Domain;
using OrderService.Domain.IRepository;

namespace OrderService.Infrastructure.ExternalServices;

/// <summary>
/// 库存服务的“联络员”：下单前先锁库存，失败或关单后负责释放库存。
/// </summary>
public sealed class InventoryClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> LockAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken)
    {
        return await SendAsync("api/Stock/Lock", orderNo, items, cancellationToken);
    }

    public async Task<bool> ReleaseAsync(string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken)
    {
        return await SendAsync("api/Stock/Release", orderNo, items, cancellationToken);
    }

    private async Task<bool> SendAsync(string url, string orderNo, IReadOnlyCollection<OrderStockRequestItem> items, CancellationToken cancellationToken)
    {
        var payload = new
        {
            bizNo = orderNo,
            items = items.Select(item => new { item.SkuId, item.Quantity })
        };

        var response = await httpClient.PostAsync(
            url,
            new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken);
        return result?.Code == 200 && result.Data is not null;
    }
}
