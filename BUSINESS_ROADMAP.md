# SimpleShop 业务完善与架构演进计划

## 1. 目标

将现有服务骨架演进为一套可运营的 B2C 电商系统：

- 面向消费者提供完整购物链路：注册登录、浏览商品、加购、下单、支付、履约、售后、评价、营销。
- 面向商家/运营提供商品、库存、订单、营销、结算、内容管理能力。
- 使用 Ocelot 作为统一 API 网关，收敛认证、路由、限流、协议转换与灰度分流。
- 建立 API 中台，统一接口发布、版本管理、权限申请、文档和调用方治理。
- 建立可观测的灰度发布机制，按用户、设备、渠道、地域或比例逐步放量。

## 2. 总体业务域设计

> 下表是 2026-08-26 的实现快照；“演进重点”仍代表后续方向。

| 域 | 服务 | 核心职责 | 当前状态 | 演进重点 |
| --- | --- | --- | --- | --- |
| 身份认证 | AuthService | 注册、登录、OpenIddict 授权、Token、刷新令牌、多端会话、风险登录 | 较完整 | 补齐手机号/邮箱验证码、第三方登录、密码策略、MFA、设备管理和异常登录检测 |
| 用户会员 | UserService | 账号资料、收货地址、实名信息、等级成长值、积分、标签、偏好设置 | 骨架（仅结构同步） | 拆分“账号身份”与“会员资产”，避免用户表膨胀 |
| 商品 | ProductService | 平台/商户归属、SPU/SKU、类目、品牌、规格、价格、商品详情、审核/上下架 | MVP | 补价格版本、前台展示快照和搜索读模型 |
| 库存 | InventoryService | SKU 库存锁定、扣减、释放、流水防重和支付成功消费 | MVP | 扩展仓库维度库存、安全库存、取消/超时补偿的可观测性 |
| 购物车 | CartService | Redis 加购、查询、移除 | MVP | 补合并、选中、优惠试算入口和失效商品提示 |
| 订单交易 | OrderService | 幂等下单、订单/明细查询、取消、发货、签收、支付结果消费、超时关单 | MVP | 完善拆单、完整状态机校验、快照和压测 |
| 支付 | PaymentService | 支付单创建、模拟确认、幂等回调、退款记录、支付成功事件 | MVP | 接入真实渠道、验签、对账和 Outbox |
| 营销 | PromotionService | 券、满减、折扣、限时购、拼团、赠品、叠加规则 | 缺失 | 新建服务；营销试算独立于下单主链路 |
| 履约配送 | FulfillmentService | 发货单、仓库分配、物流商适配、轨迹订阅、签收 | 缺失 | 新建服务；先支持单一仓和标准快递模板 |
| 售后 | AfterSaleService | 仅退款、退货退款、换货、售后单状态机、客服审核、逆向库存 | 缺失 | 与订单解耦，但引用不可变订单快照 |
| 客户 CRM | CustomerService | 外部客户档案、来源、联系方式、客户查询 gRPC | 较完整 | 明确与 C 端会员边界，避免重复建模 |
| 权限 | PermissionService | 后台角色、资源权限、数据权限、API 权限点 | 占位 | 服务后台 Admin/BFF 和 API 中台的授权中心 |
| 中台管理 | AdminService + AdminBff | 运营后台聚合接口、审批流、操作审计、组织管理 | 占位 | Admin 不直连业务库，统一走服务 API 或事件 |
| 平台支撑 | CommunalService | 配置、日志、缓存、ID、Consul、gRPC 基础包 | 可复用 | 抽出消息总线、Outbox、分布式锁、幂等、审计和可观测组件 |

## 3. 推荐架构

```text
Web App / Mobile / Mini Program
        |
        v
+----------------------+
| Ocelot API Gateway   |
| - AuthN/AuthZ        |
| - Route              |
| - Rate Limit         |
| - Gray Release       |
| - Protocol Proxy     |
+----------------------+
        |
        +--> ShoppingBff      --> Cart / Product / Promotion / Order
        +--> AdminBff         --> Product / Order / Promotion / Permission
        +--> OpenApiBff       --> Partner API / API 中台发布接口
        |
        +--> Auth / User / Customer / Inventory / Payment / Fulfillment

BFF --> Service REST/gRPC
Service --> PostgreSQL / Redis / RabbitMQ or Kafka
Service --> Elasticsearch（商品搜索）/ OpenTelemetry Collector
```

### 分层原则

- 网关只做横切能力，不写电商业务规则。
- BFF 按终端体验聚合接口，不直接访问其他服务的数据库。
- 业务服务拥有自己的数据库；跨服务写入使用命令、事件和 Saga。
- 查询侧允许 CQRS：搜索、报表、详情页使用读模型。
- 所有对外 HTTP/gRPC 接口必须经 API 中台登记后发布。

## 4. API 中台设计

### 4.1 能力

1. **API 资产目录**
   - 登记服务名、系统域、接口名、用途、负责人、敏感级别、SLA。
   - 区分 `内部 RPC`、`BFF 接口`、`开放平台 API`。
2. **版本与生命周期**
   - 状态流转：`Draft -> Review -> Published -> Deprecated -> Retired`。
   - URL 采用 `/api/v{major}/{resource}`；破坏性变更新建 major 版本。
   - 同一 major 只允许向后兼容新增字段和可选参数。
3. **文档与契约**
   - REST 提供 OpenAPI，MagicOnion/gRPC 提供 proto/接口元数据。
   - 发布时生成调用示例、错误码表、字段说明和变更记录。
4. **应用与授权**
   - 调用方创建 AppId，申请接口套餐和数据范围。
   - 支持 OAuth2 Client Credentials、用户委托授权和签名模式。
   - 网关校验 AppId、Token、Scope、IP 白名单和限流配额。
5. **发布流程**
   - 提交接口变更 → 自动 Diff/OpenAPI 校验 → 安全合规审核 → 测试环境验证 → 灰度发布 → 全量发布。
6. **运行治理**
   - 记录调用方、QPS、成功率、P95/P99、错误码分布。
   - 支持熔断降级、Mock、沙箱环境、订阅通知和配额告警。

### 4.2 数据模型

| 表 | 说明 |
| --- | --- |
| api_definition | API 元数据、路径、方法、所属服务、负责人、敏感级别 |
| api_version | 版本号、OpenAPI/proto 快照、兼容性结论、状态 |
| api_release | 环境、灰度策略、发布时间、发布人、审批单 |
| api_app | 调用方应用、密钥指纹、状态、配额 |
| api_grant | AppId、API 版本、Scope、有效期、数据范围 |
| api_route_policy | 网关路由、集群、权重、Header/Cookie 条件 |
| api_change_log | 字段级 Diff、兼容性检查、审批意见 |
| api_metric_daily | 调用量、成功数、失败码、耗时分布 |

## 5. Ocelot 网关设计

### 5.1 路由规划

| 对外路径 | 下游服务 | 说明 |
| --- | --- | --- |
| `/api/v1/auth/*` | AuthService | 登录、授权、Token |
| `/api/v1/users/*` | UserService | 会员资料、地址、偏好 |
| `/api/v1/products/*` | ProductService | 商品详情、类目、品牌 |
| `/api/v1/search/*` | SearchService 或 Product 读模型 | 搜索、筛选、推荐 |
| `/api/v1/carts/*` | CartService | 购物车 |
| `/api/v1/inventories/*` | InventoryService | 库存校验与预占查询 |
| `/api/v1/orders/*` | OrderService | 下单、取消、订单查询 |
| `/api/v1/payments/*` | PaymentService | 收银台、支付回调 |
| `/api/v1/promotions/*` | PromotionService | 券、活动、试算 |
| `/admin-api/v1/*` | AdminBff | 运营后台 |
| `/open-api/v1/*` | OpenApiBff | 合作方接口 |

### 5.2 网关横切策略

- **认证**：网关校验 OpenIddict JWT/Introspection，移除外部传入的内部身份 Header 后注入可信用户上下文。
- **授权**：公开接口、登录接口、后台权限点和开放平台 Scope 分别配置。
- **限流**：匿名 IP 限流、登录用户 QPS 限流、AppId 配额、核心写接口单独阈值。
- **熔断**：对下游 P99、5xx 和连接失败设置熔断，返回稳定错误结构。
- **幂等**：下单、支付、退款、售后提交要求客户端传 `Idempotency-Key`，网关透传到服务。
- **审计**：记录请求 ID、Trace ID、AppId、用户 ID、路由版本、命中灰度组。
- **响应规范**：统一业务码、消息、Trace ID、服务版本和时间戳。

## 6. 灰度发布逻辑

### 6.1 三种灰度对象

1. **接口/路由灰度**
   - 新接口先只放给白名单租户或内部账号。
   - 同一对外路径配置多个下游集群。
2. **服务实例灰度**
   - 新版本服务部署为 `stable` 和 `canary` 实例组。
   - Consul 中实例带 `version`、`lane`、`build-id` 元数据。
3. **数据/功能开关**
   - 功能开关控制新链路是否启用。
   - 写模型切换必须保证新旧结构兼容和可回滚。

### 6.2 分流条件优先级

建议从上到下匹配：

1. 强制白名单/黑名单：`user_id`、`device_id`、`app_id`、`tenant_id`。
2. 指定实验组：Cookie/Header `X-Experiment-Lane`。
3. 渠道、平台、地域、门店等业务维度。
4. 用户尾号或一致性哈希百分比放量。
5. 默认进入 `stable` 组。

### 6.3 请求标识与传递

网关入口解析并生成：

```text
X-Request-Id:       全局请求 ID
X-User-Id:          已认证用户 ID
X-Device-Id:        设备 ID
X-Tenant-Id:        租户/店铺 ID
X-Gray-Group:       stable / canary / experimental-xx
X-Gray-Ruleset:     命中的规则集版本
```

规则：

- 外部只能携带实验意愿 Header，不能直接指定最终灰度组。
- 网关完成鉴权和条件计算后覆盖安全相关 Header。
- HTTP 透传 Header；gRPC 通过 metadata 透传。
- 同一会话内购物车、下单、支付尽量粘住同一版本，减少跨版本语义差异。

### 6.4 Ocelot 灰度实现方式

短期方案：

- 自定义 Ocelot `DelegatingHandler` 或中间件计算 `X-Gray-Group`。
- 在 Consul 注册时写入 `version=stable|canary` 元数据。
- 自定义 downstream selector 或负载均衡器按灰度组过滤 Consul 实例。
- 配置中心保存灰度规则，网关热更新规则集。

长期方案：

- 引入独立流量决策服务，输出用户命中的实验/版本。
- 网关缓存决策结果，避免每次访问规则存储。
- 决策结果写入访问日志和 Trace attribute。

### 6.5 放量流程与回滚

| 阶段 | 流量 | 准入条件 | 回滚动作 |
| --- | --- | --- | --- |
| 内部验证 | 指定账号 100% | 冒烟用例通过、核心埋点正常 | 关闭规则 |
| 小流量 | 0.1%–1% | 成功率下降 <0.5%，P95 增幅 <20% | 切回 stable |
| 扩量 | 5% → 10% → 25% | 错误率、业务转化、资损指标达标 | 降低权重或黑名单异常用户 |
| 半量 | 50% | 数据对比无显著差异，队列无积压 | 一键清空 canary 权重 |
| 全量 | 100% | 观察一个完整业务周期 | 保留 canary 配置便于热修 |

核心监控：

- 技术指标：QPS、错误率、HTTP/gRPC 状态码、P95/P99、CPU、内存、GC、连接池。
- 业务指标：曝光、加购率、下单率、支付成功率、库存扣减成功率、退款率、GMV、客单价。
- 数据一致性：订单-支付状态、订单-库存流水、Outbox 未发送数量、补偿任务失败数。

## 7. 事件与核心流程

### 7.1 下单主流程

```text
Cart Checkout
  -> OrderService.CreateOrder
       -> 风控/限购校验
       -> PromotionService.Calculate
       -> InventoryService.LockStock
       -> 创建待支付订单 + Outbox(OrderCreated)
  -> PaymentService.CreatePayment
  -> 渠道回调
       -> PaymentService.VerifyAndMarkPaid
       -> Publish(PaymentSucceeded)
       -> OrderService.MarkPaid
       -> InventoryService.DeductLockedStock
       -> FulfillmentService.CreateShipment
```

补偿：

- 支付超时：`OrderCancelled -> InventoryService.ReleaseStock -> PromotionService.ReleaseCoupon`。
- 支付成功但订单更新失败：支付侧保持已支付事实，触发重试/人工工单。
- 发货失败：不回滚支付，进入履约异常处理。

### 7.2 必备事件

| 事件 | 生产者 | 主要消费者 |
| --- | --- | --- |
| UserRegistered | AuthService/UserService | CRM、风控、营销 |
| ProductPublished / ProductOfflined | ProductService | 搜索、购物车、推荐 |
| PriceChanged | ProductService | 购物车、营销、缓存失效 |
| StockChanged / StockLocked / StockReleased | InventoryService | 商品、购物车、监控 |
| OrderCreated / OrderCancelled / OrderPaid | OrderService | 库存、营销、履约、分析 |
| PaymentSucceeded / PaymentRefunded | PaymentService | 订单、财务、通知 |
| ShipmentDelivered | FulfillmentService | 订单、评价、积分 |
| CouponReceived / CouponUsed / CouponReleased | PromotionService | 会员、订单、分析 |
| AfterSaleApproved / RefundSucceeded | AfterSaleService | 订单、库存、财务 |

## 8. 数据与一致性原则

- 每个微服务独享数据库，禁止跨服务 Join 业务表。
- 跨服务查询优先使用 API 聚合、本地只读快照或事件驱动物化视图。
- 订单保存商品标题、图片、单价、优惠、收货地址等成交快照。
- 支付回调、库存扣减、优惠券核销必须以业务单号 + 幂等键去重。
- 本地事务只保护本服务聚合；跨服务使用 Outbox + 消息总线 + 幂等消费。
- 高风险操作记录操作前/后数据和审计人。

## 9. 分阶段计划表

> 优先级：P0 = 主链路必需；P1 = 商业化必备；P2 = 体验增强；P3 = 平台化增强。

### 当前进度快照（2026-08-26）

- 已完成：根 `SimpleShop.slnx` 和 `build.ps1`；RabbitMQ 消息抽象；Redis 分布式锁；Ocelot 基础路由、请求 ID、灰度 Header 和 PV 日志；Cart、Inventory、Order、Payment 主链路 MVP；订单发货/签收、退款记录、支付超时关单；EFK 日志消费、按天索引、死信队列和 Log API。
- 部分完成：商品平台/商户归属与审核状态；MerchantPlatform 的平台、商户和配置管理；共享异常/PV/操作日志埋点，并已覆盖交易、库存和商户关键动作。
- 未完成或需要加强：Central Package Management；Outbox；OpenTelemetry 全链路 Trace；集成测试骨架；`Domin -> Domain` 等结构清理；网关 Consul 动态发现、认证校验、限流、熔断和 Swagger 聚合；营销试算；独立履约/售后服务；权限/Admin/API 中台。

### 阶段 0：基线修复与技术底座（1.5 周）

| 序号 | 任务 | 优先级 | 交付物 | 验收标准 |
| --- | --- | --- | --- | --- |
| 0.1 | 统一根解决方案与构建脚本 | P0 | `SimpleShop.sln`、Directory.Build.props、build.ps1 | 一条命令 restore/build/test |
| 0.2 | 统一公共包版本 | P0 | Central Package Management | 无主要依赖版本漂移 |
| 0.3 | 抽出消息总线抽象 | P0 | `CommunalService.Messaging` | 支持 RabbitMQ/Kafka 实现，消费幂等 |
| 0.4 | 实现 Outbox 与后台投递 | P0 | Outbox 表、Dispatcher、重试策略 | 本地事务与发消息一致 |
| 0.5 | 接入 OpenTelemetry | P0 | Trace、Metrics、日志关联 | Trace ID 贯穿网关和服务 |
| 0.6 | 建立集成测试骨架 | P0 | Testcontainers/WebApplicationFactory | PostgreSQL/Redis 可在测试中启动 |
| 0.7 | 修正命名与项目结构 | P1 | `Domin -> Domain`，清理 Class1 | 项目命名一致 |

### 阶段 1：Ocelot 网关基础（1 周）

| 序号 | 任务 | 优先级 | 交付物 | 验收标准 |
| --- | --- | --- | --- | --- |
| 1.1 | 新建 `Gateway/Ocelot.ApiGateway` | P0 | Ocelot 项目 | 可代理现有 Auth/Product/Customer |
| 1.2 | 接入 Consul 服务发现 | P0 | 动态下游配置 | 服务实例变化无需改网关配置 |
| 1.3 | 配置 Swagger/OpenAPI 聚合 | P1 | 文档入口 | 按服务和版本查看 API |
| 1.4 | 接入 OpenIddict Token 校验 | P0 | 认证中间件 | 无效 Token 不能访问受保护路由 |
| 1.5 | 统一请求 ID 与响应格式 | P0 | Trace 中间件 | 日志可按请求串联合并 |
| 1.6 | 匿名/用户/AppId 限流 | P1 | 限流策略 | 核心接口有明确阈值 |
| 1.7 | 熔断降级与健康检查 | P1 | Polly/QoS 配置 | 下游异常返回稳定降级响应 |

### 阶段 2：交易主链路 MVP（3 周）

| 序号 | 任务 | 优先级 | 交付物 | 验收标准 |
| --- | --- | --- | --- | --- |
| 2.1 | 完善 Product 读模型 | P0 | 详情接口、缓存、状态机 | 仅上架商品可被购买 |
| 2.2 | 新建 InventoryService | P0 | 库存表、锁定、扣减、回补 | 并发下单不超卖 |
| 2.3 | 新建 CartService | P0 | Redis 购物车、选中、合并、失效 | 多端登录可合并购物车 |
| 2.4 | 完善 OrderService | P0 | 订单聚合、状态机、下单、取消、超时关单 | 订单状态不可跳变 |
| 2.5 | 最小营销试算 | P1 | 单券/满减接口 | 下单金额可复现 |
| 2.6 | 新建 PaymentService | P0 | 支付单、模拟渠道、回调验签、幂等 | 支付成功驱动订单支付 |
| 2.7 | 打通下单 Saga | P0 | 试算、锁库存、创建订单、支付、扣库存 | 正向/取消场景测试通过 |
| 2.8 | 建立订单快照 | P0 | 商品/价格/地址快照 | 后续商品变更不影响历史订单 |
| 2.9 | 交易压测 | P0 | k6/NBometer 用例 | 达成目标 TPS 且无超卖 |

### 阶段 3：会员、履约与后台（2.5 周）

| 序号 | 任务 | 优先级 | 交付物 | 验收标准 |
| --- | --- | --- | --- | --- |
| 3.1 | 完善 UserService | P0 | 资料、地址簿、默认地址、账号注销 | 地址快照可进入订单 |
| 3.2 | 明确 Customer 定位 | P1 | 与 User 的关系模型 | 不重复维护同一自然人身份数据 |
| 3.3 | 新建 FulfillmentService | P0 | 发货单、简单仓配、物流轨迹 | 已支付订单可发货签收 |
| 3.4 | 新建 AfterSaleService | P1 | 仅退款/退货退款状态机 | 退款成功驱动订单和财务 |
| 3.5 | 完善 PermissionService | P0 | 角色、菜单/API 权限、数据范围 | 后台越权访问被拒绝 |
| 3.6 | 建设 AdminBff | P1 | 商品、库存、订单、售后管理接口 | Admin 不直连业务数据库 |
| 3.7 | 操作审计 | P1 | 审计日志与查询 | 敏感操作可追溯 |

### 阶段 4：API 中台（2 周）

| 序号 | 任务 | 优先级 | 交付物 | 验收标准 |
| --- | --- | --- | --- | --- |
| 4.1 | 建 API 中台服务 | P0 | API 定义、版本、App、授权表 | 所有开放接口有资产记录 |
| 4.2 | OpenAPI/proto 导入与 Diff | P0 | 契约解析器 | 破坏性变更自动标记 |
| 4.3 | 发布审批流 | P0 | Draft/Review/Published/Deprecated | 未经审批不能发布生产 |
| 4.4 | AppId 与 Scope 授权 | P0 | 应用凭证、接口授权、配额 | 无授权调用返回标准错误 |
| 4.5 | OpenApiBff | P1 | 合作方聚合接口 | 外部只访问中台登记过的接口 |
| 4.6 | 调用量与质量报表 | P2 | 网关日志聚合看板 | 可查 QPS、成功率、P95/P99 |
| 4.7 | Mock 与沙箱 | P2 | 测试环境、固定样例数据 | 调用方联调不影响生产 |

### 阶段 5：灰度发布体系（2 周）

| 序号 | 任务 | 优先级 | 交付物 | 验收标准 |
| --- | --- | --- | --- | --- |
| 5.1 | 定义灰度元数据 | P0 | Consul metadata、镜像标签、部署清单 | 每个实例有 version/lane/build-id |
| 5.2 | 灰度规则模型 | P0 | 规则表/配置、黑白名单、百分比、哈希种子 | 规则可版本化 |
| 5.3 | 网关灰度中间件 | P0 | 解析身份、命中规则、写入 X-Gray-Group | 相同用户稳定命中同一组 |
| 5.4 | Consul 实例选择器 | P0 | 按 version/lane 过滤实例 | canary 只接收灰度请求 |
| 5.5 | 管理端灰度控制台 | P1 | 设置规则、权重、暂停、一键回滚 | 生产变更可快速切回 |
| 5.6 | 灰度指标看板 | P0 | stable vs canary 对比 | 技术与业务指标分版本可见 |
| 5.7 | 自动分析与护栏 | P2 | 错误率/延迟/资融护栏 | 超阈值自动停量或告警 |
| 5.8 | 数据库迁移灰度 | P0 | Expand-Migrate-Contract 流程 | 新旧代码兼容同一 schema |

### 阶段 6：商业化增强（持续）

| 序号 | 任务 | 优先级 | 交付物 | 验收标准 |
| --- | --- | --- | --- | --- |
| 6.1 | 搜索与推荐 | P1 | Elasticsearch、索引同步、排序实验 | 支持关键词、筛选、纠错 |
| 6.2 | 营销平台化 | P1 | 券模板、活动、叠层规则、防刷 | 营销预算可控 |
| 6.3 | 结算与财务 | P1 | 平台账、商家账、佣金、对账单 | 订单/支付/退款三方对平 |
| 6.4 | 评价与内容 | P2 | 订单评价、图片审核、问大家 | 只有已完成订单可评价 |
| 6.5 | 消息通知 | P2 | 短信、站内信、Push、邮件模板 | 重要节点可触达且可退订 |
| 6.6 | 风控反欺诈 | P2 | 设备指纹、频控、黑名单、交易风控 | 黄牛/羊毛场景可拦截 |
| 6.7 | 多租户/多店 | P3 | 租户隔离、店铺、渠道定价 | 支持平台型扩展 |

## 10. 里程碑验收

| 里程碑 | 时间 | 验收演示 |
| --- | --- | --- |
| M1：技术底座与网关 | 第 3 周末 | 通过网关登录并查询商品；Trace 全链路可见 |
| M2：交易闭环 MVP | 第 6 周末 | 浏览商品、加购、下单、支付、扣库存、取消回补全链路可用 |
| M3：履约与后台治理 | 第 9 周末 | 后台授权后可管理商品/订单；订单可发货签收并售后 |
| M4：API 中台上线 | 第 11 周末 | 合作方申请 AppId/授权后调用开放接口，指标可查 |
| M5：灰度体系上线 | 第 13 周末 | 新版本 canary 只接收指定用户/比例流量，并可一键回滚 |

## 11. 风险与应对

| 风险 | 影响 | 应对 |
| --- | --- | --- |
| 一次性拆太多服务 | 开发慢、联调复杂 | 先模块化单体/粗粒度服务，交易闭环后再细拆 |
| FreeSql 结构同步用于生产 | 迁移不可控 | 生产改为显式迁移脚本和审批 |
| 分布式事务复杂度过高 | 资损和脏数据 | 事件 + Outbox + 幂等 + 对账 + 人工工单 |
| 灰度只做 HTTP 不覆盖异步 | 异步链路仍全量切换 | 消息携带版本/灰度组，消费方声明兼容矩阵 |
| 中台变成纯文档系统 | 治理失效 | 发布、鉴权、配额、指标强制接入 |
| Ocelot 自定义过重 | 升级困难 | 网关只保留横切逻辑，复杂流量决策外置 |
| 密钥和敏感配置泄漏 | 安全事故 | AgileConfig 权限隔离、密钥轮换、日志脱敏 |
