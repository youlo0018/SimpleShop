# SimpleShop 现代电商业务蓝图

> 目标：以“平台 → 商户 → 商品 → 用户 → 购物车 → 订单 → 支付 → 履约 → 售后 → 结算”为主线，补齐当前系统缺失的业务能力，并明确 Redis 分布式锁、Kafka/RabbitMQ 消息、日志采集和风控治理方案。

## 1. 现状与目标差距

### 1.1 当前已具备

| 能力 | 当前状态 |
| --- | --- |
| 服务分层 | 多数服务具备 Api / Application / Domain / Infrastructure 分层 |
| 公共基础设施 | FreeSql + PostgreSQL、Redis、AgileConfig、Consul、MagicOnion、MediatR、FluentValidation |
| Ocelot 网关 | 已有统一入口、健康检查和基础灰度 Header |
| Auth | OpenIddict、Cookie 登录、API 登录命令 |
| Product | 商品创建、详情、上架、分类树 |
| Customer | 客户创建、查询和 gRPC 示例 |
| MerchantPlatform | 商户创建、查询、审核；平台配置保存与查询 |
| Order | 订单创建、查询、取消的基础模型 |

### 1.2 主要缺口

| 缺口 | 影响 | 本蓝图处理 |
| --- | --- | --- |
| 平台与商户层级不完整 | 无法支撑多平台、多商户经营 | 增加平台注册、商户入驻依赖平台、商户账号与权限 |
| 商品缺少商户归属和库存 | 商品无法交易，也无法防超卖 | 商品绑定 PlatformId/MerchantId，独立库存服务 |
| 无购物车服务 | 用户无法完成浏览到结算链路 | 新建 CartService，Redis 主存储 |
| 下单/支付无分布式锁 | 并发下可能重复下单或超卖 | Redis 锁 + 幂等键 + 数据库唯一约束兜底 |
| 无支付与消息事件 | 支付结果不能驱动订单、库存、通知 | PaymentService + Kafka/RabbitMQ + Outbox |
| 无 PV/操作日志体系 | 无法分析转化、审计风险行为 | 行为日志网关埋点 + 业务日志事件化 |
| 无履约、售后、结算 | 交易闭环断裂 | 补齐发货、售后退款、商户结算 |
| 无营销、搜索、评价、风控 | 运营增长能力不足 | 规划为第二阶段增强域 |

## 2. 总体角色与边界

### 2.1 角色

| 角色 | 说明 |
| --- | --- |
| 平台运营者 | 注册平台、审核商户、配置类目/费率/支付方式、管理全局规则 |
| 平台管理员 | 平台内员工，按角色管理商品审核、订单仲裁、结算和售后 |
| 商户主体 | 依附某个平台入驻，拥有营业执照、结算账户、费率和店铺信息 |
| 商户管理员 | 商户内员工，管理商品、库存、价格、订单履约和售后 |
| 消费者用户 | 登录后浏览商品、加购、下单、支付、收货、评价、申请售后 |
| 开放平台调用方 | 通过 API 中台申请接口权限的外部系统 |
| 客服/风控 | 处理工单、拦截风险订单、管理黑名单和申诉 |

### 2.2 租户与数据归属

- 所有经营数据必须携带 `TenantType`、`PlatformId`。
- 商户数据额外携带 `MerchantId`；商品、库存、订单项必须归属商户。
- 用户属于 C 端会员域，但订单记录购买方和成交快照。
- 后台读写必须校验平台/商户数据范围，禁止仅靠前端隐藏控制权限。

## 3. 核心业务流程

### 3.1 平台注册与初始化

```text
超级管理员创建平台
  -> 平台基本信息、域名、租户编码、联系方式
  -> 初始化平台管理员
  -> 配置类目模板、支付渠道、物流模板、结算周期、佣金默认值
  -> 启用平台并生成 PlatformId
```

关键规则：

1. `platform` 表中租户编码全局唯一。
2. 只有 `Enabled` 平台才允许商户入驻。
3. 平台必须配置默认结算周期和默认佣金费率。
4. 平台禁用前必须校验是否存在未完成订单、待结算款或未完结售后。

### 3.2 商户入驻

```text
商户选择平台
  -> 提交主体资料、行业类目、联系人、结算账户、证照
  -> 平台资质初审
  -> 风控/合规复核
  -> 审核通过
       -> 生成 MerchantId 和 MerchantNo
       -> 创建商户管理员账号
       -> 绑定店铺、类目权限、结算合同和费率
  -> 商户登录并维护商品、库存、运费模板
```

关键规则：

1. 商户必须携带 `PlatformId`，未指定有效平台的申请直接拒绝。
2. 同一平台内社会信用代码唯一；跨平台允许同一主体经营，但要共享风控画像。
3. 商户审核通过后才可创建商品、库存和店铺装修。
4. 商户被禁用时：
   - 已上架商品下架；
   - 购物车标记失效；
   - 不允许新订单；
   - 已有订单继续履约或进入平台介入流程。

### 3.3 商户发布商品

```text
商户登录
  -> 选择类目、品牌、属性、规格
  -> 创建 SPU
  -> 创建 SKU：售价、市场价、库存、编码、图集、重量
  -> 设置运费模板、限购、是否可退
  -> 提交审核
  -> 平台审核通过后上架
  -> 同步搜索索引和缓存
```

关键规则：

1. SPU/SKU 必须绑定 `PlatformId` 与 `MerchantId`。
2. SKU 编码在同一商户下唯一；对外展示使用 SKU ID。
3. 价格调整需要保留版本号，下单使用成交价快照。
4. 商品状态至少包括草稿、待审核、上架、下架、违规封禁。
5. 商品变更后发送 `ProductChanged`，购物车和搜索异步更新。

### 3.4 用户注册登录与浏览

```text
游客访问首页/搜索/商品详情
  -> 记录 PV/UV、曝光、点击
  -> 用户登录
       -> 账号密码、手机验证码、第三方 OAuth
       -> 签发 Access Token / Refresh Token
  -> 浏览商品详情
       -> 展示价格、SKU、库存态、评价摘要、推荐商品
       -> 写入浏览历史和行为日志
```

关键规则：

1. 未登录可以浏览，但加购、收藏、下单必须登录。
2. 商品详情读取缓存，库存实时查询或短 TTL 缓存。
3. 用户只能看到上架且所属商户有效的商品。
4. 所有浏览、点击、搜索行为输出 PV 日志事件。

### 3.5 加入购物车与结算

```text
用户选择 SKU 和数量
  -> AddToCart
       -> 校验登录、商品上架、商户有效、库存充足
       -> Redis Hash 写入购物车项
       -> 发布 CartChanged
用户进入购物车
  -> 查看/修改数量/选中/删除/清空失效商品
  -> 试算商品金额、运费、优惠
用户点击结算
  -> 校验选中项
  -> 冻结当前价格与优惠试算结果
  -> 进入确认订单页
```

购物车存储建议：

| Key | 类型 | 内容 |
| --- | --- | --- |
| `cart:user:{userId}` | Hash | Field=SkuId，Value=数量、选中状态、加入时间 |
| `cart:item:{skuId}` | String/Hash | 商品标题、图片、单价、商户、上架状态 |
| `cart:checked:{userId}` | Set | 已选中 SkuId |
| `cart:version:{userId}` | String | 乐观锁版本，避免并发覆盖 |

### 3.6 创建订单与支付

#### 3.6.1 正向流程

```text
用户提交结算
  -> OrderService.PreCreateToken
       -> 幂等键 + 地址 + 商品快照哈希
用户确认下单
  -> OrderService.CreateOrder
       -> Redis 分布式锁：lock:order:create:{userId}
       -> Redis 分布式锁：lock:stock:{skuId}
       -> 校验商品、商户、价格、限购、地址
       -> InventoryService.LockStock
       -> PromotionService.CalculateDiscount
       -> 创建待支付订单与订单项
       -> 保存商品/价格/优惠/地址快照
       -> 扣减购物车或标记已结算
       -> Outbox 写入 OrderCreated
       -> 释放锁
  -> 返回 orderId 和 paymentToken
用户发起支付
  -> PaymentService.CreatePayment
       -> lock:payment:order:{orderId}
       -> 校验订单待支付且未超时
       -> 创建支付单
  -> 第三方支付/模拟支付回调
       -> 验签
       -> lock:payment:callback:{paymentNo}
       -> 支付单置为成功
       -> Outbox 写入 PaymentSucceeded
OrderService 消费 PaymentSucceeded
  -> 幂等消费
  -> 订单置为已支付
InventoryService 消费 PaymentSucceeded
  -> 锁定库存转扣减库存
FulfillmentService 消费 OrderPaid
  -> 生成发货单
NotificationService 消费事件
  -> 推送支付成功、订单状态变更
```

#### 3.6.2 分布式锁设计

| 场景 | Key | 粒度 | TTL | 说明 |
| --- | --- | --- | --- | --- |
| 创建订单 | `lock:order:create:{userId}` | 用户 | 10s | 防止同用户并发重复下单 |
| 幂等请求 | `idempotent:order:{IdempotencyKey}` | 请求 | 24h | 相同幂等键返回首次结果 |
| 单 SKU 库存 | `lock:stock:{skuId}` | SKU | 5–10s | 防止并发超卖 |
| 批量锁库存 | 按 SkuId 升序逐个获取 | 多 SKU | 15s | 避免死锁 |
| 创建支付单 | `lock:payment:order:{orderId}` | 订单 | 10s | 一个订单同一时间只生成一个有效支付单 |
| 支付回调 | `lock:payment:callback:{paymentNo}` | 支付单 | 10s | 防止重复回调 |
| 取消订单 | `lock:order:cancel:{orderId}` | 订单 | 10s | 与支付回调互斥 |
| 优惠券核销 | `lock:coupon:{couponCode}` | 券 | 10s | 防止一券多用 |

锁实现要求：

1. 使用 `SET NX PX` 或 Redisson 式看门狗；业务完成后释放自己的锁。
2. 锁 Value 使用 RequestId/ThreadId，Lua 脚本校验后再删除。
3. 获取锁失败返回“稍后重试”或进入队列，不允许长时间自旋。
4. 数据库仍需唯一约束和状态机兜底，例如订单号唯一、支付流水唯一、库存扣减条件更新。

#### 3.6.3 订单状态机

| 状态 | 编码建议 | 允许流转 |
| --- | --- | --- |
| 待支付 | 10 | 已支付、已取消、已关闭 |
| 已支付 | 20 | 待发货、退款中、已完成 |
| 待发货 | 30 | 已发货、退款中 |
| 已发货 | 40 | 已签收、退货中 |
| 已签收/已完成 | 50 | 售后中、已关闭 |
| 已取消 | 90 | 终态 |
| 已关闭 | 91 | 终态 |

### 3.7 支付与消息

#### 3.7.1 支付能力

1. 收银台聚合微信、支付宝、余额、组合支付。
2. 支付单与订单分离，一个订单可有多次支付尝试。
3. 回调必须验签、幂等、记录原始报文。
4. 支付超时自动关单。
5. 支持全额退款和多次部分退款。
6. 支付对账：渠道账单、支付单、订单三方核对。

#### 3.7.2 消息选型

| 场景 | 推荐 |
| --- | --- |
| 高吞吐日志、PV/UV、行为分析 | Kafka |
| 业务事件、延迟队列、重试、路由 | RabbitMQ |
| 团队只维护一套中间件 | 可先统一 Kafka 或 RabbitMQ，抽象层隔离 |

系统采用 `IMessagePublisher` 抽象：

```csharp
public interface IMessagePublisher
{
    Task PublishAsync<T>(string topic, string key, T message, CancellationToken cancellationToken);
}
```

第一阶段可使用 RabbitMQ 实现；日志量上来后新增 Kafka Publisher，不改业务代码。

#### 3.7.3 必备 Topic/Exchange

| 名称 | 用途 | 生产者 | 消费者 |
| --- | --- | --- | --- |
| `order.created` | 创建订单 | Order | 营销、通知、风控、分析 |
| `order.cancelled` | 取消/超时关闭 | Order | 库存回补、优惠券释放、通知 |
| `payment.succeeded` | 支付成功 | Payment | Order、Inventory、Fulfillment、通知、结算 |
| `payment.refunded` | 退款成功 | Payment | Order、AfterSale、Inventory、财务 |
| `stock.locked` | 锁定库存 | Inventory | 商品、购物车、监控 |
| `stock.released` | 释放库存 | Inventory | 商品、监控 |
| `stock.deducted` | 扣减库存 | Inventory | 商品、分析 |
| `product.changed` | 商品变更 | Product | 搜索、购物车、缓存 |
| `merchant.status.changed` | 商户状态变化 | MerchantPlatform | Product、Cart、结算 |
| `operation.log` | 业务操作日志 | 全部后台写接口 | Log Consumer |
| `pv.log` | 页面浏览/曝光/点击 | Gateway/BFF | Log Consumer |

#### 3.7.4 消息可靠性

1. 生产端：本地事务先落库，再写 Outbox 表，由后台任务投递。
2. Broker：开启持久化和生产确认。
3. 消费端：
   - 消息表或 Redis SETNX 记录 MessageId；
   - 消费失败进入重试队列；
   - 超过阈值进入死信和人工处理。
4. 所有事件包含：EventId、EventType、OccurredAt、TraceId、PlatformId、MerchantId、UserId、PayloadVersion。

### 3.8 库存

| 能力 | 规则 |
| --- | --- |
| 可售库存 | 总库存 - 锁定库存 - 已售不可退占用量 |
| 下单锁定 | 条件更新 `available >= quantity` 后写入锁定数 |
| 支付扣减 | 支付成功后锁定转扣减 |
| 超时释放 | 延迟消息或定时任务扫描过期订单 |
| 取消回补 | 已锁未扣库存可回补 |
| 售后回补 | 按商品配置决定是否重新可售 |
| 防超卖 | Redis 锁 + 数据库条件更新 + 流水表 |

### 3.9 履约配送

```text
订单已支付
  -> 生成发货单
  -> 商户拣货/打包
  -> 上传包裹和物流单
  -> 物流轨迹订阅
  -> 签收
  -> 订单完成
  -> 触发评价期、积分、结算周期
```

补充能力：

1. 发货单拆分与合并。
2. 电子面单。
3. 部分发货。
4. 签收回传异常处理。
5. 超时未发货预警。

### 3.10 售后退款

| 类型 | 流程 |
| --- | --- |
| 仅退款 | 申请 -> 商户/平台审核 -> 退款 -> 完成 |
| 退货退款 | 申请 -> 审核 -> 用户寄回 -> 商户收货验收 -> 退款 -> 完成 |
| 换货 | 申请 -> 审核 -> 寄回 -> 重新发货 -> 完成 |

关键规则：

1. 售后单引用订单项和成交快照。
2. 一次订单项可多次部分售后，累计金额不能超过实付。
3. 退款成功通过 `PaymentRefunded` 驱动订单和逆向库存。
4. 平台可介入争议售后。

### 3.11 商户结算

```text
订单完成/售后完结
  -> 进入结算周期
  -> 汇总订单金额、佣金、退款、赔付
  -> 生成本期结算单
  -> 平台财务审核
  -> 打款
  -> 更新结算状态
```

结算单字段应包括平台、商户、周期、订单笔数、销售额、退款额、佣金、应付净额、打款凭证和状态。

## 4. 营销、搜索与增长

### 4.1 营销

| 能力 | 关键规则 |
| --- | --- |
| 优惠券 | 券模板、发放量、每人限领、有效期、适用商品范围 |
| 满减/折扣 | 支持商品级、店铺级、平台级；明确叠加顺序 |
| 限时购 | 活动库存独立或共享，开始/结束时间服务端控制 |
| 积分 | 下单、签到、评价产生积分；抵扣比例和退款回收规则清晰 |
| 首购/复购 | 用户标签和活动人群 |

### 4.2 搜索推荐

1. Elasticsearch 索引商品标题、类目、品牌、属性、销量、价格、标签。
2. 商品变更通过 `ProductChanged` 异步同步。
3. 支持关键词、筛选、排序、纠错、分页。
4. 推荐场景：首页推荐、相似商品、看了又看、购物车搭配。

### 4.3 评价与内容

1. 只有已完成订单可评价。
2. 支持文字、图片、评分、追评。
3. 敏感词和图片审核。
4. 商户可回复但不能篡改用户内容。

## 5. 权限、风控与客服

### 5.1 权限

| 层级 | 示例 |
| --- | --- |
| 平台角色 | 平台超管、运营、财务、审核员 |
| 商户角色 | 商户超管、商品运营、订单履约、客服 |
| 功能权限 | 菜单、按钮、API Scope |
| 数据权限 | 只能查看授权平台/商户/门店的数据 |

所有敏感操作写入 `operation.log`，记录操作前后数据摘要。

### 5.2 风控反欺诈

| 场景 | 控制 |
| --- | --- |
| 注册登录 | 设备指纹、频控、异地提醒、验证码 |
| 下单 | 限购、黑名单、异常价格、批量小号识别 |
| 支付 | 支付失败频控、高风险用户人工审核 |
| 营销 | 领券/活动防刷，同一设备/手机号/支付账户限制 |
| 商户 | 资质造假、虚假交易、刷评识别 |

### 5.3 客服与工单

1. 会话来源：订单详情、商品详情、售后单。
2. 工单类型：催发货、改地址、退款、投诉、发票。
3. SLA 计时和升级机制。
4. 平台介入仲裁。

## 6. 日志体系

### 6.1 日志分类

| 类型 | 来源 | 内容示例 | 保存通道 |
| --- | --- | --- | --- |
| 访问日志 | Ocelot/BFF | Method、Path、Status、耗时、AppId、UserAgent | Kafka/RabbitMQ |
| PV 日志 | Web/App/H5 埋点 | Page、ItemId、SpuId、MerchantId、曝光位、停留时长 | Kafka 优先 |
| 点击日志 | 前端埋点 | 点击位置、搜索词、推荐实验组 | Kafka 优先 |
| 业务操作日志 | 后台写接口 | 操作人、动作、对象 ID、前后数据摘要 | RabbitMQ/Kafka |
| 安全日志 | Auth/Permission | 登录成功/失败、权限变更、密钥变更 | Kafka/RabbitMQ |
| 系统日志 | 服务运行时 | Exception、Warning、GC、慢 SQL | OpenTelemetry/文件采集 |
| 消息日志 | Outbox/Consumer | EventId、Topic、投递次数、消费状态 | 数据库 + 监控 |

### 6.2 PV 日志流程

```text
前端 SDK / 网关中间件
  -> 组装 PageViewEvent
       TraceId, RequestId, UserId, DeviceId,
       PlatformId, MerchantId, Page, ItemId,
       Source, ExperimentGroup, OccurredAt
  -> 批量上报 /gateway/logs/pv
  -> Gateway 校验并写入 Kafka pv.log
  -> LogConsumer 批量解析
       -> PostgreSQL 明细/日汇总
       -> ClickHouse/Elasticsearch 分析库
```

### 6.3 业务操作日志流程

```text
后台写接口执行成功
  -> OperationLogAspect/Middleware
       -> OperationType, ObjectType, ObjectId,
          OperatorId, OperatorType,
          BeforeSnapshotHash, AfterSnapshotHash,
          Ip, UserAgent, TraceId
  -> Outbox 或本地队列
  -> operation.log
  -> LogConsumer 落库
```

### 6.4 日志字段规范

公共字段：

```json
{
  "eventId": "uuid",
  "eventType": "pv.page_view",
  "occurredAt": "2026-08-25T12:00:00Z",
  "traceId": "trace-id",
  "requestId": "request-id",
  "platformId": 1001,
  "merchantId": 20001,
  "userId": 300001,
  "deviceId": "device-id",
  "source": "h5|app|mini|admin|openapi",
  "payloadVersion": "v1"
}
```

要求：

1. 手机号、身份证、银行卡、地址必须脱敏。
2. 日志不作为强一致业务依据，重要事实仍以数据库为准。
3. 明细建议保留 30–180 天，汇总数据长期保留。
4. 消费积压、解析失败率、写入延迟必须有告警。

## 7. 服务与数据模型规划

| 服务 | 核心表 |
| --- | --- |
| MerchantPlatformService | platform、platform_admin、merchant、merchant_account、merchant_qualification、settlement_contract、platform_config |
| ProductService | spu、sku、product_detail、category、brand、specification、specification_value、product_review_status |
| InventoryService | inventory、inventory_lock_flow、inventory_deduct_flow |
| CartService | Redis 购物车结构 + cart_snapshot/cart_change_log |
| PromotionService | promotion_activity、coupon_template、user_coupon、promotion_rule、promotion_calc_log |
| OrderService | orders、order_item、order_address_snapshot、order_discount、order_state_log、idempotency_record |
| PaymentService | payment_order、payment_channel、payment_callback_log、refund_order |
| FulfillmentService | shipment、shipment_package、shipment_track |
| AfterSaleService | after_sale_order、after_sale_item、after_sale_log |
| SettlementService | settlement_bill、settlement_order_detail、payout_record |
| NotificationService | notification_template、notification_task、message_contact_history |
| SearchService | product_index、search_suggest、hot_keyword |
| LoggingAnalytics | page_view_log、click_log、operation_log、security_log |
| PermissionService | role、permission、resource、role_permission、data_scope |

## 8. 分阶段实施计划

### 阶段 A：主链路打通（P0）

| 序号 | 任务 | 输出 |
| --- | --- | --- |
| A1 | 平台注册与平台配置完善 | platform/platform_admin 表、平台启用状态 |
| A2 | 商户入驻强依赖平台 | 商户申请必须带有效 PlatformId |
| A3 | 商户账号与数据范围 | 商户管理员只能管理本商户资源 |
| A4 | 商品绑定平台/商户 | SPU/SKU 增加 PlatformId、MerchantId |
| A5 | 商品审核与上架规则 | 商户提交、平台审核 |
| A6 | 用户登录与身份上下文 | Token 中带 UserId、Source、DeviceId |
| A7 | CartService | 加购、修改、选中、合并、删除 |
| A8 | InventoryService | 库存查询、锁定、扣减、释放 |
| A9 | OrderService 增强 | PreCreateToken、幂等键、订单项、快照、状态机 |
| A10 | PaymentService MVP | 支付单、模拟支付、回调幂等 |
| A11 | Outbox + RabbitMQ | order/payment/stock 事件可靠投递 |
| A12 | Redis 锁组件 | 统一 DistributedLock 抽象与 Lua 释放 |

### 阶段 B：闭环治理（P0/P1）

| 序号 | 任务 | 输出 |
| --- | --- | --- |
| B1 | 支付超时关单 | 定时扫描或延迟消息 |
| B2 | 履约服务 | 发货单、物流单、签收 |
| B3 | 售后退款 | 仅退款、退货退款状态机 |
| B4 | 商户结算 | 结算周期、佣金、退款冲抵 |
| B5 | 权限与数据范围 | 平台/商户两级 RBAC |
| B6 | 操作日志 | 后台写接口统一切面 |
| B7 | PV 日志 | 网关/前端埋点 + Kafka |
| B8 | 对账 | 支付渠道、支付单、订单核对 |

### 阶段 C：运营增长（P1/P2）

| 序号 | 任务 | 输出 |
| --- | --- | --- |
| C1 | 营销服务 | 券、满减、限时购 |
| C2 | 搜索服务 | ES 商品索引、筛选排序 |
| C3 | 评价服务 | 订单评价、图片审核、评分统计 |
| C4 | 通知中心 | 短信、站内信、Push、邮件 |
| C5 | 风控 | 设备指纹、频控、黑名单、交易风险评分 |
| C6 | 数据看板 | GMV、转化漏斗、商户排行、库存周转 |

## 9. 验收标准

| 场景 | 验收 |
| --- | --- |
| 平台注册 | 平台创建后获得唯一 PlatformId，只有启用平台可入驻商户 |
| 商户入驻 | 未带平台或平台禁用时入驻失败；审核通过后可发布商品 |
| 商品发布 | 商品必须归属商户，未审核商品不可见不可买 |
| 浏览与加购 | 游客可浏览，登录后可加购；失效商品在购物车可见原因 |
| 下单防重 | 同一幂等键并发请求只生成一个订单 |
| 防超卖 | 100 并发抢 10 个库存，最终成功扣减不超过 10 |
| 支付一致性 | 支付成功后订单、库存、消息三者最终一致 |
| 支付防重 | 同一支付回调重复推送，订单只变更一次 |
| 超时关单 | 待支付超时订单自动取消并回补库存 |
| 日志采集 | PV 和操作日志能从入口到 Kafka/RabbitMQ 再落库 |
| 商户结算 | 完成订单减去退款和佣金后，结算金额可追溯 |
