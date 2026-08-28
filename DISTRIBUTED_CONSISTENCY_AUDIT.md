# 分布式一致性审计

> 审计时间：2026-08-26。范围：定时任务、跨服务 HTTP/MQ 调用、多表写入和并发状态变更。

## 1. 已具备的分布式策略

| 链路 | 当前策略 |
| --- | --- |
| 订单创建 | 幂等键内存缓存 + 数据库检查；用户维度分布式锁；先锁库存再落单，同步失败时释放库存。 |
| 订单状态变更 | 支付消费、用户取消、支付超时关单统一竞争 `lock:order:{orderId}`；关键转换使用数据库条件更新兜底。 |
| 支付创建/确认/退款 | 按 BizNo 分布式锁；支付创建和确认支持幂等返回；退款累计金额在锁内复核。 |
| 库存调整 | SKU 维度分布式锁；`stock_flow` 按 BizNo + SKU + 动作防重；条件更新限制可售/锁定数量。 |
| 日志消费 | RabbitMQ 懒连接；Elasticsearch 使用 eventId 幂等写入；失败进入 LogService 死信队列。 |

## 2. 本次修复

### 定时任务独立进程

- 新增 `src/ScheduledService/ScheduledService`。
- 将支付超时关单从 `OrderService.Api` 迁移到独立 Worker。
- 增加全局扫描锁：多实例部署时同一轮只有一个实例扫描。
- 保留订单维度锁和锁后复检：全局锁过期或实例重启时仍不会重复关单。
- API 进程只保留支付成功事件消费者；时间驱动任务与请求驱动服务解耦。

### 订单状态互斥修复

- 用户取消原来无锁且直接整对象更新，现在使用订单锁 + 条件更新。
- 支付消费原锁键是 `lock:order:pay:{id}`，超时关单是 `lock:order:close:{id}`，两者不能互斥；现统一为 `lock:order:{id}`。
- 支付消费改为锁内重新读取订单，并使用 `AwaitPayment && !IsPayment` 条件更新。
- 锁等待超时的支付消息会重新入队，而不是直接丢弃。

### 支付事件补发

支付确认重复调用原来直接命中幂等分支并返回，不会再发布事件；如果首次“更新数据库成功、发事件失败”，下游可能永久收不到事件。现在重复确认会安全补发 `payment.succeeded`，由订单状态和库存流水幂等去重。

## 3. 2026-08-26 后续修复补充

- RabbitMQ 发布端统一改为 camelCase；Order/Inventory 消费者同步读取 `payload`，消除支付成功事件字段大小写漂移。
- `ScheduledService` 补注册独立进程所需的 `IFreeSql`，超时关单和库存释放已单独跑通。
- 订单明细、发货明细改为逐条插入，并把 `ShipmentItem` 加入 Code First 迁移列表；下单、发货不再因批量插入或缺表卡住。
- 管理端与用户小程序已接入 Gateway；可重复执行的 E2E 和冒烟脚本见 `tests/e2e/`。

## 4. 仍需治理的风险

### P0：缺少 Outbox

以下链路仍是“本地写入/外部调用”后再发消息，进程崩溃或 MQ 长时间不可用时无法保证业务落库和事件发出的原子性：

1. `ConfirmPaymentHandler`：`MarkPaidAsync` → 发布 `payment.succeeded`。
2. `CreateOrderHandler`：订单/明细落库 → 发布 `order.created`。
3. `PaymentTimeoutCloseJob`：关单 → 释放库存 → 发布 `order.cancelled`。

建议引入服务内 Outbox 表和中继任务：业务数据与 Outbox 记录在同一本地事务保存，中继任务用分布式锁扫描并投递，消费端继续幂等。

### P0：订单创建前的库存预留孤儿

下单流程先调用 InventoryService 锁定库存，再写订单和明细。如果进程在远程锁定成功后、本地订单落库前崩溃，当前没有订单可触发超时释放，库存可能一直被占用。

建议为库存预留增加 TTL 或建立 `inventory_reservation` 表和后台对账任务；也可以将该步骤纳入 Saga 编排。

### P0：超时关单后的库存释放补偿不足

当前任务会记录释放失败日志，但订单已经关闭，后续查询不再选中它；如果释放调用失败或响应丢失，锁定库存需要人工处理。

建议增加关单补偿状态表，或在订单上维护库存释放状态，并由定时任务重试。InventoryService 的流水幂等已具备，重放是安全的。

### P1：MQ 消费失败会丢消息

`OrderService.PaymentSucceededConsumer` 和 `InventoryService.PaymentSucceededConsumer` 对非锁冲突异常执行 `BasicNack(requeue:false)`，但没有死信队列。坏消息或临时依赖失败可能导致事件丢失。

建议为每个业务消费者配置独立 DLQ、重试计数和人工/自动重放入口；LogService 已实现该模式，可以复用配置方式。

### P1：本地多表写入没有事务边界

以下方法分多次 FreeSql 写入，中间失败会产生半截数据：

| 位置 | 写入序列 |
| --- | --- |
| `OrderRepository.AddAsync` + `AddItemsAsync` | 先写订单，再写订单项。 |
| `ProductService.CreateProductCommandHandler` | 先写商品，再循环写 SKU。 |
| `ShipmentRepository.AddAsync` | 先写发货单，再写发货明细。 |

这不是跨服务事务，但会影响Saga的起点数据完整性。建议封装仓储级工作单元，或将主表/明细写入放入同一数据库事务。

### P2：履约状态锁范围不一致

`ReceiveShipmentHandler` 锁的是 `shipment.Id`，但最终修改的是订单状态。多个包裹并发签收，或签收与未来退款/取消流程并发时，仍可能出现订单状态覆盖。

建议统一改为 `lock:order:{orderId}`，并在锁内重新读取订单和发货单，校验订单仍允许签收。

## 4. 推荐落地顺序

1. 为 Payment/Order 增加共享 Outbox 组件和中继 Worker。
2. 用 Outbox 重写 `payment.succeeded`、`order.created`、`order.cancelled` 发布点。
3. 增加库存预留 TTL 与超时释放补偿查询。
4. 给业务 MQ 消费者补齐 DLQ 和重试策略。
5. 收敛履约锁到订单维度，并为本地多表写入加事务边界。
