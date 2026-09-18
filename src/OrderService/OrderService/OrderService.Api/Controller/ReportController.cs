using CommunalService.Domain;
using FreeSql;
using Microsoft.AspNetCore.Mvc;
using OrderService.Domain.Entity;

namespace OrderService.Api.Controller;

/// <summary>
/// 工作台报表：按租户聚合交易数据，返回当日核心指标和用于回归拟合的趋势点。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ReportController(IFreeSql freeSql, TenantContext tenant) : BaseController
{
    [HttpGet]
    /// <summary>查询数据：Get（过滤条件与返回语义见参数与调用方约定）。</summary>
    public async Task<ApiResponse> Get([FromQuery] string granularity = "day")
    {
        var today = DateTime.Today;
        var isMonthly = string.Equals(granularity, "month", StringComparison.OrdinalIgnoreCase);
        var trendStart = isMonthly ? new DateTime(today.Year - 1, today.Month, 1) : today.AddDays(-29);

        var orders = await freeSql.Select<Order>().Where(order => !order.IsDeleted && order.CreatedAt >= trendStart)
            .WhereIf(tenant.IsPlatform, order => order.PlatformId == tenant.PlatformId)
            .WhereIf(tenant.IsMerchant, order => order.MerchantId == tenant.MerchantId)
            .ToListAsync();
        var orderIds = orders.Select(order => order.Id).ToArray();
        var items = orderIds.Length == 0 ? [] : await freeSql.Select<OrderItem>().Where(item => orderIds.Contains(item.OrderId)).ToListAsync();

        Func<DateTime, string> keyOf = isMonthly
            ? date => new DateTime(date.Year, date.Month, 1).ToString("yyyy-MM")
            : date => date.ToString("yyyy-MM-dd");
        var orderTimeMap = orders.ToDictionary(order => order.Id, order => order.CreatedAt);
        var reportGroups = orders.GroupBy(order => keyOf(order.CreatedAt)).ToDictionary(group => group.Key,
            group => new { Orders = group.Count(), Gmv = group.Where(item => item.IsPayment).Sum(item => item.TotalPrice),
                Users = group.Select(item => item.CustomerId).Distinct().Count() });
        var itemGroups = items.GroupBy(item => keyOf(orderTimeMap.GetValueOrDefault(item.OrderId)))
            .ToDictionary(group => group.Key, group => group.Sum(row => row.Quantity));

        var pointCount = isMonthly ? 12 : 30;
        var trend = Enumerable.Range(0, pointCount).Select(offset =>
        {
            var label = isMonthly ? trendStart.AddMonths(offset).ToString("yyyy-MM") : trendStart.AddDays(offset).ToString("yyyy-MM-dd");
            var @group = reportGroups.TryGetValue(label, out var matchedGroup) ? matchedGroup : null;
            return new ReportPoint
            {
                Date = label,
                DailyActiveUsers = group?.Users ?? 0,
                DailyOrders = group?.Orders ?? 0,
                Gmv = Math.Round(group?.Gmv ?? 0, 2),
                SoldItems = itemGroups.GetValueOrDefault(label)
            };
        }).ToList();

        var currentPeriodStart = isMonthly ? new DateTime(today.Year, today.Month, 1) : today;
        var currentOrders = orders.Where(order => order.CreatedAt >= currentPeriodStart).ToList();
        var currentItemIds = currentOrders.Select(order => order.Id).ToArray();
        return Ok(new
        {
            granularity = isMonthly ? "month" : "day",
            summary = new ReportPoint
            {
                Date = keyOf(currentPeriodStart),
                DailyActiveUsers = currentOrders.Select(order => order.CustomerId).Distinct().Count(),
                DailyOrders = currentOrders.Count,
                Gmv = Math.Round(currentOrders.Where(order => order.IsPayment).Sum(order => order.TotalPrice), 2),
                SoldItems = currentItemIds.Length == 0 ? 0 : items.Where(item => currentItemIds.Contains(item.OrderId)).Sum(item => item.Quantity)
            },
            trend
        });
    }

    private sealed class ReportPoint
    {
        /// <summary>统计日期（yyyy-MM-dd）。</summary>
        public string Date { get; set; } = string.Empty;
        /// <summary>日活用户数。</summary>
        public int DailyActiveUsers { get; set; }
        /// <summary>日订单数。</summary>
        public int DailyOrders { get; set; }
        /// <summary>成交总额（元）。</summary>
        public decimal Gmv { get; set; }
        /// <summary>售出件数。</summary>
        public int SoldItems { get; set; }
    }
}
