# SimpleShop EFK 日志链路

## 架构

业务服务只负责把日志发布到 RabbitMQ，不直接等待 Elasticsearch；`LogService` 统一消费并写入 ES，Kibana 负责检索和看板。

```text
API / Gateway
  -> PV / Operation / Exception 日志事件
  -> RabbitMQ Topic Exchange
  -> LogService Consumer
  -> Elasticsearch Index
  -> Kibana Dashboard
```

## 日志类型

| 类型 | Topic | Elasticsearch 索引 | 用途 |
| --- | --- | --- | --- |
| PV / 访问日志 | `pv.log` | `logs-pv-yyyy.MM.dd`（基础映射 `logs-pageview`） | 接口访问、耗时、来源、用户身份 |
| 业务操作日志 | `operation.log` | `logs-operation-yyyy.MM.dd` | 下单等关键动作审计 |
| 异常捕捉日志 | `exception.log` | `logs-exception-yyyy.MM.dd` | 未处理异常、堆栈、TraceId |

公共字段包括 `eventId`、`occurredAt`、`traceId`、`platformId`、`merchantId`、`userId`。

## 埋点位置

- `PageViewLoggingMiddleware`：记录 `/api/*` 请求耗时与身份。
- `ExceptionLoggingMiddleware`：兜底未处理异常，返回统一错误和 `traceId`。
- `IOperationLogger.LogAsync()`：业务代码在关键命令成功后主动记录。

订单创建、支付确认、商品创建、库存锁定/扣减/释放、商户入驻和商户审核已接入 `IOperationLogger`。其他关键动作可以按同样方式注入 `IOperationLogger` 并记录操作类型、对象类型、对象 ID 和摘要。

## 本地启动

1. 启动 Elasticsearch 和 Kibana：

   ```powershell
   docker compose -f deploy/efk/docker-compose.yml up -d
   ```

2. 启动 RabbitMQ。
3. 启动 `LogService.Api`：

   ```powershell
   dotnet run --project src/LogService/LogService.Api
   ```

4. 打开 Kibana：<http://localhost:5601>  
   首次使用时创建 Data View：
   - `logs-pv-*`
   - `logs-operation-*`
   - `logs-exception-*`

## 配置

`LogService.Api/appsettings.json`：

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest",
    "Exchange": "simpleshop.events"
  },
  "Elasticsearch": {
    "Url": "http://localhost:9200"
  }
}
```

Elasticsearch 开启安全认证后，补充 `Username` 和 `Password`。

开发环境查询 API 默认使用：

```json
{
  "LoggingApi": {
    "ApiKey": "local-log-api-key"
  }
}
```

调用 `/api/log/{kind}` 时携带请求头 `X-Api-Key: local-log-api-key`。生产环境必须覆盖该值，并优先在网关或平台权限层再做一次鉴权。

## 可观测性

LogService 暴露：

- `GET /health`：进程健康检查。
- `GET /metrics`：Prometheus 文本指标。
- `logservice_messages_succeeded_total{kind}`：成功写入 Elasticsearch 的日志数。
- `logservice_messages_failed_total{kind}`：进入死信队列的日志数。
- `logservice_elasticsearch_write_seconds{kind}`：Elasticsearch 写入耗时直方图。

## 失败策略

- MQ 不可用：业务服务和 LogService 都不会启动失败，后台持续重连。
- 发布日志失败：中间件只记录 Warning/Error，不影响业务响应。
- Elasticsearch 写入失败：消息 Nack 且进入 `logservice.logs.dlq`，由人工修复或重放。
- 用户侧异常响应只暴露统一文案和 `traceId`，堆栈保存在 `logs-exception`。
