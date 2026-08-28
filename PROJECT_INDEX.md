# SimpleShop 项目索引

> 索引更新时间：2026-08-26。本文档以当前源码为准，描述服务结构、运行入口、主要接口和数据模型。

## 1. 总览

- 运行时：.NET 10（`global.json` 锁定 `10.0.111`，允许最新 Feature SDK）。
- 架构形态：按业务域拆分微服务，多数服务采用 Api / Application / Domain / Infrastructure 分层。
- 根解决方案：`SimpleShop.slnx` 聚合网关与各业务服务项目。
- 主要技术栈：ASP.NET Core、FreeSql + PostgreSQL、EF Core + PostgreSQL（Auth）、Redis、RabbitMQ、Consul、AgileConfig、MediatR、FluentValidation、MagicOnion gRPC、Elasticsearch、Kibana、Prometheus、Ocelot。
- 共享能力：`CommunalService.Domain` 提供基础配置、数据库、缓存、雪花 ID、分布式锁、Consul、消息发布、日志埋点、gRPC 和 MediatR 管道。
- 当前状态：购物车、商品、库存、订单、支付、商户/平台和日志链路已有可运行 MVP；用户与客服能力仍是骨架或轻量 MVP；权限服务和 Admin 服务仍主要是占位。

## 2. 目录索引

| 路径 | 说明 |
| --- | --- |
| `Gateway/Ocelot.ApiGateway/` | Ocelot API 网关，提供静态路由、健康检查、灰度上下文和 PV 日志。 |
| `apps/admin-web/` | 轻量管理端静态页面，覆盖平台、商户、商品、订单、支付和库存操作。 |
| `apps/user-miniapp/` | 原生微信用户小程序骨架，覆盖商品详情、地址、支付结果和本地订单列表。 |
| `src/AdminService/AdminService/AdminService.sln` | Admin 类库占位；当前只有 `AdminService.Domin`。 |
| `src/AuthService/AuthService/AuthService.sln` | 认证授权服务，集成 OpenIddict、Cookie 登录和 EF Core PostgreSQL。 |
| `src/CartService/CartService.Api` | Redis 购物车服务。 |
| `src/CommunalService/CommunalService/CommunalService.Domain` | 跨服务共享基础库。 |
| `src/CustomerService/CustomerService.sln` | 客户服务，包含 REST、MediatR 用例、MagicOnion gRPC 和 FreeSql 仓储。 |
| `src/InventoryService/InventoryService.Api` | 库存锁定、扣减、释放和支付成功消费。 |
| `src/LogService/LogService.Api` | RabbitMQ 日志消费、Elasticsearch 写入、查询 API 和 Prometheus 指标。 |
| `src/MerchantPlatformService/` | 商户、平台和平台配置合并微服务。 |
| `src/OrderService/OrderService/OrderService.sln` | 订单创建、取消、发货、签收、支付结果消费和超时关单。 |
| `src/ScheduledService/ScheduledService` | 独立定时任务 Worker，当前承载支付超时关单。 |
| `src/PaymentService/PaymentService.Api` | 支付单创建、确认、退款和支付成功事件发布。 |
| `src/PermissionService/PermissionService.sln` | 权限服务占位，仅保留默认 WeatherForecast 示例端点。 |
| `src/ProductService/ProductService.sln` | 商品、SKU、类目、品牌、规格与商品发布链路。 |
| `src/UserService/UserService.sln` | 用户服务骨架，当前只有结构同步端点。 |
| `deploy/efk/` | Elasticsearch 与 Kibana Docker Compose 编排。 |
| `tests/e2e/` | 网关主链路 E2E 与前端冒烟测试脚本。 |

## 3. 服务索引

| 服务 | 启动入口 | HTTP 端口 | 当前能力 | 成熟度 |
| --- | --- | --- | --- | --- |
| Gateway | `Gateway/Ocelot.ApiGateway/Program.cs:1` | 5008 | Ocelot 路由、`/health`、灰度 Header 处理、请求 ID 和 PV 日志。 | MVP |
| Auth | `src/AuthService/AuthService/AuthService.Api/Program.cs:1` | 5019 / HTTPS 7155 | OpenIddict 授权、登录页/授权/注销、API 登录、EF Core PostgreSQL。 | 较完整 |
| Customer | `src/CustomerService/CustomerService.Api/Program.cs:1` | 5280 / HTTPS 7093 | 创建客户、查询客户、MagicOnion 客户查询。 | MVP |
| Product | `src/ProductService/ProductService.Api/Program.cs:1` | 5058 / HTTPS 7011 | 商品创建、详情、发布和类目树；启动时同步核心表。 | MVP |
| User | `src/UserService/UserService.Api/Program.cs:1` | 5011 / HTTPS 7050 | 用户表结构同步；Application 尚无业务 Handler。 | 骨架 |
| Order | `src/OrderService/OrderService/OrderService.Api/Program.cs:1` | 5064 / HTTPS 7162 | 下单幂等、库存联动、订单/详情查询、取消、发货、签收和支付成功消费。 | MVP |
| Permission | `src/PermissionService/PermissionService.Api/Program.cs:1` | 5019 / HTTPS 7155 | 仅 OpenAPI 和示例 WeatherForecast。 | 占位 |
| MerchantPlatform | `src/MerchantPlatformService/MerchantPlatformService.Api/Program.cs:1` | 5070 | 平台创建/查询、商户创建/查询/审核、平台配置保存/查询。 | MVP |
| Cart | `src/CartService/CartService.Api/Program.cs:1` | 5060 | Redis 购物车加入、查询、移除。 | MVP |
| Inventory | `src/InventoryService/InventoryService.Api/Program.cs:1` | 5062 | 库存锁定、扣减、释放，Redis 分布式锁和流水防重，消费支付成功事件。 | MVP |
| Payment | `src/PaymentService/PaymentService.Api/Program.cs:1` | 5066 | 支付单创建、模拟支付确认、支付成功事件、退款记录。 | MVP |
| Log | `src/LogService/LogService.Api/Program.cs:1` | 5088 / HTTPS 7263 | 消费 PV/操作/异常日志，写入按天 Elasticsearch 索引，提供查询 API、死信队列和指标。 | MVP |
| Admin | 无 API 入口 | 无 | 只有 `AdminService.Domin` 类库。 | 占位 |
| Communal | 无独立入口 | 无 | 提供跨服务公共库与启动扩展。 | 可复用 |
| Scheduled | 无 HTTP 入口 | 无 | 独立定时任务进程；全局扫描锁 + 订单状态锁执行支付超时关单。 | MVP |

注意：Auth 与 Permission 的开发端口都是 `5019`，HTTPS 也都配置为 `7155`；同时启动前需要调整其中一个端口。

## 4. 分层与依赖

- Api：承载 Controller/Endpoint、OpenAPI/Swagger、启动配置、托管消费任务和中间件。
- Application：承载 MediatR Command/Handler/Validator、Mapper 或 gRPC 实现。
- Domain：承载实体、领域契约、枚举、仓储接口和部分消息契约。
- Infrastructure：承载 FreeSql/EF Core 仓储、外部客户端和依赖注入。

### 关键项目引用

- `CustomerService.Domain`、`UserService.Domain`、`AuthService.Domain` → `CommunalService.Domain`。
- 多数服务 `Api` → 本服务 `Application` 与 `Infrastructure`。
- 多数服务 `Application` 与 `Infrastructure` → 本服务 `Domain`。
- `Gateway/Ocelot.ApiGateway` → `CommunalService.Domain`。

### 共享基础设施

核心扩展位于 `src/CommunalService/CommunalService/CommunalService.Domain/DependencyInjection.cs`：

- PostgreSQL/FreeSql：读取 `Basic:sqlConnectionString`，注册 `IFreeSql`，启用连接池、软删除过滤和雪花 ID 审计。
- Redis：读取 `Basic:redisConnectionString` 与 `Basic:redisDb`，注册 `IConnectionMultiplexer` 和 `IDatabase`。
- Snowflake：通过 Redis 分配 Worker ID。
- Consul：提供服务注册、发现、地址轮询与健康检查托管服务。
- RabbitMQ：懒连接发布器，交换机默认 `simpleshop.events`。
- 分布式锁：统一 `IDistributedLock` 抽象与 Lua 安全释放。
- MagicOnion/gRPC：按 `Basic:port:httpport` 与 `Basic:port:grpcport` 分别监听 HTTP/1 与 HTTP/2。
- MediatR：扫描 Handler 程序集并注册 FluentValidation 校验管道。
- 日志：异常中间件、PV 中间件、`LoggingEventPublisher` 和 `IOperationLogger`。

## 5. 网关路由

网关监听 `http://localhost:5008`，静态路由定义在 `Gateway/Ocelot.ApiGateway/ocelot.json`：

| 上游路径 | 下游路径 | 目标端口 |
| --- | --- | --- |
| `/gateway/auth/{everything}` | `/api/Auth/{everything}` | 5019 |
| `/gateway/products/{everything}` | `/api/Product/{everything}` | 5058 |
| `/gateway/customers/{everything}` | `/api/Customer/{everything}` | 5280 |
| `/gateway/orders/{everything}` | `/api/Order/{everything}` | 5064 |
| `/gateway/merchants/{everything}` | `/api/Merchant/{everything}` | 5070 |
| `/gateway/platform-configs/{everything}` | `/api/PlatformConfig/{everything}` | 5070 |
| `/gateway/carts/{everything}` | `/api/Cart/{everything}` | 5060 |
| `/gateway/stocks/{everything}` | `/api/Stock/{everything}` | 5062 |
| `/gateway/payments/{everything}` | `/api/Payment/{everything}` | 5066 |
| `/gateway/logs/{everything}` | `/api/log/{everything}` | 5088 |

平台 Controller 已存在，但网关尚未暴露独立的 `/gateway/platforms/*` 路由。

## 6. API 索引

REST 控制器通常遵循 `BaseController` 的 `api/[controller]/[action]` 路由约定，响应统一包装为 `{Code, Message, Data}`。

### AuthService

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/Auth/Login` | API 登录。 |
| GET | `/api/Account/Login` | 登录页/回调流程。 |
| GET | `/api/Account/Logout` | 注销 Cookie 会话。 |
| GET | `/api/Account/Authorize` | OpenIddict 授权入口。 |

### CustomerService

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/Customer/AddCustomer` | 创建客户。 |
| GET | `/api/Customer/GetCustomer` | 按 ID 查询客户。 |
| gRPC | `ICustomerService.GetCustomerAsync` | MagicOnion 客户查询。 |

### ProductService

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/Product/CreateProduct` | 创建商品/SKU。 |
| GET | `/api/Product/GetProductDetail` | 查询已审核且已上架商品。 |
| POST | `/api/Product/PublishProduct` | 发布商品。 |
| GET | `/api/Product/GetCategoryTree` | 查询类目树。 |

### MerchantPlatformService

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/Platform/Create` | 创建平台。 |
| GET | `/api/Platform/Get` | 查询平台。 |
| POST | `/api/Merchant/Create` | 商户入驻。 |
| GET | `/api/Merchant/Get` | 查询商户。 |
| POST | `/api/Merchant/Review` | 商户审核。 |
| GET | `/api/PlatformConfig/Get` | 查询平台配置。 |
| POST | `/api/PlatformConfig/Save` | 保存平台配置。 |

### CartService

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/Cart/Add` | 加入购物车。 |
| GET | `/api/Cart/Get` | 查询购物车。 |
| POST | `/api/Cart/Remove` | 移除购物车项。 |

### InventoryService

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/Stock/Lock` | 锁定库存。 |
| POST | `/api/Stock/Deduct` | 扣减库存。 |
| POST | `/api/Stock/Release` | 释放库存。 |

### OrderService

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/Order/Create` | 幂等下单并请求库存锁定。 |
| GET | `/api/Order/Get` | 查询订单列表。 |
| GET | `/api/Order/Detail` | 查询订单详情。 |
| POST | `/api/Order/Shipment` | 已支付订单创建发货单。 |
| POST | `/api/Order/Receive` | 发货单签收。 |
| POST | `/api/Order/Cancel` | 取消待支付订单。 |

### PaymentService

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| POST | `/api/Payment/Create` | 按业务单号幂等创建支付单。 |
| POST | `/api/Payment/Confirm` | 模拟支付确认并发布 `payment.succeeded`。 |
| POST | `/api/Payment/Refund` | 创建部分或全额退款记录。 |

### LogService

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `/health` | 健康检查。 |
| GET | `/metrics` | Prometheus 文本指标。 |
| GET | `/api/log/{kind}` | 查询 `pv`、`operation`、`exception` 日志；需要 `X-Api-Key`。 |

### UserService

| 方法 | 路径 | 说明 |
| --- | --- | --- |
| GET | `/api/User/SyncStructure` | 同步 FreeSql 用户实体结构。 |

## 7. 数据模型索引

| 服务 | 实体/存储 | 文件位置 |
| --- | --- | --- |
| Auth | `UserApplication`、`UserAuthorization`、`UserToken`、`UserScope`、`Application` | `src/AuthService/AuthService/AuthService.Domain/Entity/` |
| Product | `Product`、`Sku`、`Category`、`Brand`、`Specification`、`SpecificationValue`、`SkuSpecification` | `src/ProductService/ProductService.Domain/Entity/` |
| Inventory | `stock`、`stock_flow` | `src/InventoryService/InventoryService.Domain/Entity/Stock.cs` |
| Order | `order`、`order_item`、`shipment`、`shipment_item` | `src/OrderService/OrderService/OrderService.Domain/Entity/` |
| Payment | `PaymentOrder`、`RefundOrder` | `src/PaymentService/PaymentService.Domain/Entity/` |
| MerchantPlatform | `platform`、`merchant`、`platform_config` | `src/MerchantPlatformService/MerchantPlatformService.Domain/Entity/` |
| Customer | `customer` | `src/CustomerService/CustomerService.Domain/Entity/Customer.cs` |
| User | `User` | `src/UserService/UserService.Domain/Entity/User.cs` |
| Communal | `BaseEntity`（ID、软删除、审计字段） | `src/CommunalService/CommunalService/CommunalService.Domain/Entity/BaseEntity.cs` |
| Log | Elasticsearch 按天索引 `logs-pv-*`、`logs-operation-*`、`logs-exception-*` | `src/LogService/LogService.Api/Elasticsearch/ElasticsearchLogWriter.cs` |

## 8. 消息与日志链路

### Topic

| Topic | 生产方/用途 | 消费方 |
| --- | --- | --- |
| `pv.log` | 网关和服务 PV 中间件 | LogService |
| `operation.log` | 业务命令主动审计 | LogService |
| `exception.log` | 全局异常中间件 | LogService |
| `order.created` | CreateOrderHandler | 当前预留外部消费 |
| `payment.succeeded` | ConfirmPaymentHandler | OrderService、InventoryService |
| `order.cancelled` | ScheduledService 超时关单任务 | 当前预留外部消费 |

### 一致性机制

- 订单创建使用内存缓存 + 数据库幂等键检查 + 用户维度分布式锁。
- 下单先尝试锁库存，订单落库失败时释放库存。
- 支付确认使用分布式锁和状态检查保证回调幂等，成功后发布事件。
- OrderService 后台消费者在支付事件丢失后补偿更新已支付状态。
- InventoryService 后台消费者按 BizNo + SKU + 动作流水幂等扣减库存。
- 支付超时任务运行在 `ScheduledService`，默认每 30 秒扫描；全局扫描锁避免多实例重复调度，订单锁保证关单与支付互斥。
- LogService 使用主队列、死信交换机/队列和 eventId 幂等写入。
- 订单创建、支付确认、商品创建、库存锁定/扣减/释放、商户入驻和商户审核成功后会发布操作审计事件。

## 9. 运行与配置

- 根解决方案构建：`./build.ps1`；还原：`./build.ps1 -Target Restore`；构建并测试：`./build.ps1 -Target Test`。
- 单个服务典型启动：`dotnet run --project src/ProductService/ProductService.Api`。
- 开发环境优先在 `appsettings.Development.json` 提供 AgileConfig 地址；真实 `Basic:*` 配置通常由 AgileConfig 下发。
- 必备外部依赖：PostgreSQL、Redis、AgileConfig、Consul、RabbitMQ；日志链路另需 Elasticsearch 和 Kibana。
- RabbitMQ、Consul 或 Elasticsearch 不可用时，多数后台组件采用延迟重试/懒连接，不要求所有外部依赖先于应用启动。

Swagger/OpenAPI 地址通常是：

```text
http(s)://<host>:<port>/swagger
```

## 10. 快速定位

| 任务 | 起点 |
| --- | --- |
| 修改共享数据库/Redis/Consul/消息/日志行为 | `src/CommunalService/CommunalService/CommunalService.Domain/DependencyInjection.cs` |
| 新增业务用例 | 对应服务的 `Application/Features/`，并在 Controller 或 Endpoint 暴露 |
| 修改网关路由 | `Gateway/Ocelot.ApiGateway/ocelot.json` |
| 调整灰度 Header/白名单 | `Gateway/Ocelot.ApiGateway/appsettings.json` 的 `GrayRelease` |
| 查看服务端口 | 对应服务的 `Properties/launchSettings.json` |
| 查看 EF Core 迁移 | `src/AuthService/AuthService/AuthService.Infrastructure/Migrations/` |
| 查看 EFK 部署 | `deploy/efk/docker-compose.yml` 和 `deploy/efk/README.md` |
