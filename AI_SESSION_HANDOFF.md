# SimpleShop AI 会话交接文档

> 用途：在另一台电脑或新会话中恢复当前开发上下文。  
> 使用方式：新对话中直接说「请阅读 `AI_SESSION_HANDOFF.md`，按里面的进度继续」。

## 当前任务

补齐电商系统的日志能力，采用 **EFK 架构**：

- 应用服务发布日志事件到 RabbitMQ。
- `LogService` 消费 PV、业务操作、异常三类日志并写入 Elasticsearch。
- Kibana 用于展示和排障，LogService 自身提供轻量查询 API。

## 已完成

### 日志埋点与发布

- `PageViewEvent` / `OperationLogEvent` / `ExceptionLogEvent` 已定义。
- `LoggingEventPublisher` 统一发布到 RabbitMQ Topic：
  - `pv.log`
  - `operation.log`
  - `exception.log`
- `MessageEnvelope<TPayload>` 已扩展：
  - `Service`
  - `Environment`
  - `ServiceVersion`
- 发布端统一做敏感信息脱敏：
  - 手机号
  - 身份证号
  - Authorization、Token、Password、Secret 等键值对
- `ExceptionLoggingMiddleware` 已接入异常日志。
- `PageViewLoggingMiddleware` 已接入 PV 日志，并通过 `X-Gateway-PV` 避免网关和下游重复计数。

### LogService

- 项目位置：`src/LogService/LogService.Api`。
- RabbitMQ 主队列：`logservice.logs`。
- 已配置死信交换机与死信队列：
  - Exchange：`logservice.dlx`
  - Queue：`logservice.logs.dlq`
- Elasticsearch 写入已支持：
  - 按天索引：例如 `logs-pv-2026.08.25`
  - 使用 `eventId` 作为 `_id`，重复消费不会产生重复文档
- 已接入 Prometheus 指标：
  - 消费成功数
  - 消费失败数
  - Elasticsearch 写入耗时
- `/metrics` 端点已注册。

### 查询接口

- 路由：`GET /api/log/{kind}`
- `kind` 支持：
  - `pv`
  - `operation`
  - `exception`
- 支持 `traceId`、`platformId`、`merchantId`、`userId` 过滤和分页参数。
- 配置项：`LoggingApi:ApiKey`，请求头使用 `X-Api-Key`。
- Ocelot 网关已有 `/gateway/logs/{everything}` → `/api/log/{everything}`。

### EFK 编排

- Docker Compose 位置：`deploy/efk/docker-compose.yml`。
- 说明文档：`deploy/efk/README.md`。

## 当前阻塞点

以下两个编译问题尚未完全收敛：

1. `Elasticsearch.Clients.Elasticsearch 9.5.0` 中 `Query` 构造方式与当前写法不兼容。  
   文件：`src/LogService/LogService.Api/Elasticsearch/ElasticsearchLogWriter.cs`  
   目标是构造 `TermQuery` 并放入 `BoolQuery.Filter(ICollection<Query>)`。

2. Prometheus 客户端 API 差异导致 `/metrics` 输出方式需要最终确认。  
   文件：`src/LogService/LogService.Api/Program.cs`  
   当前尝试过 `Metrics.BuildText()` 与 `MapPrometheusScrapingEndpoint()`，均与安装版本不完全匹配。

## 下一步

1. 修复上面两个编译错误。
2. 全量验证：

   ```powershell
   dotnet build SimpleShop.slnx --no-restore
   ```

3. 扩展业务审计日志：
   - 支付确认
   - 商品创建
   - 库存调整
   - 商户注册
   - 商户审核
4. 根据需要补充 ILM 索引模板或部署脚本。

## 关键文件

- 日志模型：`src/CommunalService/CommunalService/CommunalService.Domain/Logging/LogEventModels.cs`
- 日志发布器：`src/CommunalService/CommunalService/CommunalService.Domain/Logging/LoggingEventPublisher.cs`
- 脱敏器：`src/CommunalService/CommunalService/CommunalService.Domain/Logging/SensitiveDataSanitizer.cs`
- 异常中间件：`src/CommunalService/CommunalService/CommunalService.Domain/Logging/Middleware/ExceptionLoggingMiddleware.cs`
- PV 中间件：`src/CommunalService/CommunalService/CommunalService.Domain/Logging/Middleware/PageViewLoggingMiddleware.cs`
- 消息信封：`src/CommunalService/CommunalService/CommunalService.Domain/Messaging/MessageEnvelope.cs`
- LogService 入口：`src/LogService/LogService.Api/Program.cs`
- ES 写入器：`src/LogService/LogService.Api/Elasticsearch/ElasticsearchLogWriter.cs`
- MQ 消费者：`src/LogService/LogService.Api/Consumers/LoggingEventConsumer.cs`
- 查询端点：`src/LogService/LogService.Api/Endpoints/LogQueryEndpoint.cs`
- EFK 编排：`deploy/efk/docker-compose.yml`
- 业务蓝图：`ECOMMERCE_BUSINESS_BLUEPRINT.md`
- 项目索引：`PROJECT_INDEX.md`

## 注意事项

- 不要主动 Git commit，除非用户明确要求。
- 不要修改旧项目 `AdminService.Domin` 的拼写问题。
- 本地没有 RabbitMQ 或 Elasticsearch 是预期情况；相关服务必须能降级启动。
- 用户要求中文注释，并且注释要解释业务意图，不要只写机械说明。
- 工具调用保持小步执行，避免输出过大导致中断。
