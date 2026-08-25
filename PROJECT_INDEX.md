# SimpleShop 项目索引

> 索引生成时间：2026-08-25。本文档描述当前源码结构与主要运行链路。

## 1. 总览

- 技术栈：.NET 10 / ASP.NET Core 微服务、FreeSql + PostgreSQL、Redis、Consul、AgileConfig、MediatR、FluentValidation、MagicOnion gRPC、MessagePack。
- 架构形态：按业务域拆分服务；多数服务内部采用 Api / Application / Domain / Infrastructure 分层。
- 共享能力：`CommunalService.Domain` 提供配置中心、数据库、缓存、雪花 ID、健康检查、Consul 注册发现和 gRPC 基础设施。
- 当前状态：Product、Customer、Auth 已有较完整业务链路；Order 有实体/仓储但控制器与 Application 尚未实现业务功能；Permission 仍接近空壳。
- 根目录：包含 `src/`、根解决方案、网关、项目索引、架构路线图与电商业务蓝图。

## 2. 目录索引

| 路径 | 说明 |
| --- | --- |
| `src/AdminService/AdminService/AdminService.sln` | Admin 服务解决方案；目前只有 `AdminService.Domin` 类库。 |
| `src/AuthService/AuthService/AuthService.sln` | 认证授权服务，集成 OpenIddict、Cookie 认证与 PostgreSQL EF Core。 |
| `src/CommunalService/CommunalService/CommunalService.sln` | 公共基础设施库解决方案，核心项目为 `CommunalService.Domain`。 |
| `src/CustomerService/CustomerService.sln` | 客户服务，包含 REST API、MediatR 用例、gRPC 服务与 FreeSql 仓储。 |
| `src/MerchantPlatformService/` | 商户与平台合并微服务，提供商户准入审核和平台配置管理；当前使用根级解决方案组织。 |
| `src/OrderService/OrderService/OrderService.sln` | 订单服务，当前主要是基础分层与订单仓储骨架。 |
| `src/PermissionService/PermissionService.sln` | 权限服务，四个分层项目均为占位实现。 |
| `src/ProductService/ProductService.sln` | 商品服务，包含商品创建、详情、上架与分类树用例。 |
| `src/UserService/UserService.sln` | 用户服务，包含用户 API 骨架、共享基础设施接入与结构同步端点。 |

## 3. 服务索引

| 服务 | 启动入口 | HTTP 端口 | 主要能力 | 成熟度 |
| --- | --- | --- | --- | --- |
| Auth | `src/AuthService/AuthService/AuthService.Api/Program.cs:1` | 5019（HTTPS 配置 7155） | OpenIddict 授权服务器、登录页/授权端点、API 登录命令、EF Core PostgreSQL 迁移。 | 较完整 |
| Customer | `src/CustomerService/CustomerService.Api/Program.cs:1` | 5280（HTTPS 配置 7093） | 新增客户、查询客户、MagicOnion `ICustomerService.GetCustomerAsync`。 | 较完整 |
| Product | `src/ProductService/ProductService.Api/Program.cs:1` | 5058（HTTPS 配置 7011） | 创建商品、商品详情、商品发布、分类树；启动时执行 FreeSql 结构同步。 | 较完整 |
| User | `src/UserService/UserService.Api/Program.cs:1` | 5011（HTTPS 配置 7050） | 用户 API 骨架与 `SyncStructure` 端点；Application 层暂无业务 Handler。 | 骨架 |
| Order | `src/OrderService/OrderService/OrderService.Api/Program.cs:1` | 5064（HTTPS 配置 7162） | 接入公共基础设施；存在 Order 实体与仓储，但没有 REST/gRPC 业务端点。 | 骨架 |
| Permission | `src/PermissionService/PermissionService.Api/Program.cs:1` | 5019 | 仅 OpenAPI 占位；Application、Domain、Infrastructure 均为 `Class1.cs`。 | 占位 |
| MerchantPlatform | `src/MerchantPlatformService/MerchantPlatformService.Api/Program.cs:1` | 5070 | 商户创建、查询、审核；平台配置保存与查询。 | MVP |
| Cart | `src/CartService/CartService.Api/Program.cs:1` | 5060 | Redis 购物车：加入、查询、移除。 | MVP |
| Inventory | `src/InventoryService/InventoryService.Api/Program.cs:1` | 5062 | 库存锁定、扣减、释放，Redis 锁 + 流水表防超卖。 | MVP |
| Payment | `src/PaymentService/PaymentService.Api/Program.cs:1` | 5066 | 支付单创建、模拟支付确认、幂等回调与支付成功事件发布。 | MVP |
| Admin | 无 API 入口 | 无 | 只有 Domain 类库，并引用 NuGet 包 `CommunalService.Domain 1.0.9`。 | 占位 |
| Communal | 无独立入口 | 无 | 提供跨服务公共库与扩展方法。 | 可复用 |

## 4. 分层与依赖

- Api：承载 Controller、OpenAPI/Swagger、启动配置和中间件。
- Application：承载 MediatR Command/Handler/Validator/Mapper 或 gRPC 实现。
- Domain：承载实体、领域契约、枚举与部分消息契约。
- Infrastructure：承载 FreeSql 仓储、外部依赖适配与依赖注入。

### 关键项目引用

- `UserService.Domain` → `CommunalService.Domain`
- `CustomerService.Domain` → `CommunalService.Domain`
- `AuthService.Domain` → `CommunalService.Domain`
- 各服务的 `Api` → 本服务 `Application` 与 `Infrastructure`
- 各服务 `Infrastructure` → 本服务 `Domain`
- 多数服务 `Application` → 本服务 `Domain`

### 共享基础设施

核心扩展位于 `src/CommunalService/CommunalService/CommunalService.Domain/DependencyInjection.cs:19`：

- AgileConfig：读取 `AgileConfig:*` 并接入配置中心。
- PostgreSQL/FreeSql：读取 `Basic:sqlConnectionString`，注册单例 `IFreeSql`，启用 ADO 连接池、SQL 监控、软删除过滤与雪花 ID 审计。
- Redis：读取 `Basic:redisConnectionString` 和 `Basic:redisDb`，注册 `IConnectionMultiplexer` 与 `IDatabase`。
- Snowflake：通过 `RedisWorkerIdProvider` 和后台服务分配 Worker ID。
- Consul：读取 `Basic:Consul`，提供服务注册、地址轮询与健康检查托管服务。
- MagicOnion/gRPC：注册 ASP.NET Core gRPC 与 MagicOnion；Kestrel 按 `Basic:port:httpport` 与 `Basic:port:grpcport` 分别监听 HTTP/1、HTTP/2。
- MediatR：`AddMediatRWithHandlers` 扫描 Handler 程序集并注册 FluentValidation 校验管道。
- Redis 分布式锁：统一 `IDistributedLock` 抽象和 Lua 安全释放。
- RabbitMQ 消息：统一 `IMessagePublisher` 抽象；订单创建和支付成功事件已接入。
- PV/访问日志：网关 `LoggingEventMiddleware` 将请求耗时与路径发布到 `pv.log`。
- 业务操作日志：共享 `LoggingEventPublisher` 支持发布 `operation.log`。
- 库存消费链路：InventoryService 后台消费 `payment.succeeded`，按 BizNo + SKU + 动作幂等执行锁定库存转扣减。

### 网关业务路由

| 上游路径 | 下游服务 |
| --- | --- |
| `/gateway/carts/*` | CartService |
| `/gateway/stocks/*` | InventoryService |
| `/gateway/payments/*` | PaymentService |
| `/gateway/platforms/*` | MerchantPlatformService.PlatformController |
| `/gateway/merchants/*` | MerchantPlatformService.MerchantController |
| `/gateway/platform-configs/*` | MerchantPlatformService.PlatformConfigController |

### 日志 Topic 约定

| Topic | 用途 |
| --- | --- |
| `pv.log` | 网关访问、页面浏览、耗时分析 |
| `operation.log` | 后台业务操作审计 |
| `order.created` | 订单创建事件 |
| `payment.succeeded` | 支付成功事件 |
| `inventory.payment.succeeded` | InventoryService 内部队列；绑定 `payment.succeeded` 并驱动库存扣减 |

## 5. API 索引

所有 REST Controller 继承或遵循 `api/[controller]/[action]` 路由约定。

### AuthService

| 方法 | 路径 | 定义 |
| --- | --- | --- |
| POST | `/api/Auth/Login` | `src/AuthService/AuthService/AuthService.Api/Controllers/AuthController.cs:8` |
| GET | `/api/Account/Login` | 登录页面/回调流程，`src/AuthService/AuthService/AuthService.Api/Controllers/AccountController.cs:13` |
| GET | `/api/Account/Logout` | 注销 Cookie 会话，`src/AuthService/AuthService/AuthService.Api/Controllers/AccountController.cs:13` |
| GET | `/api/Account/Authorize` | OpenIddict 授权入口，`src/AuthService/AuthService/AuthService.Api/Controllers/AccountController.cs:13` |

### CustomerService

| 方法 | 路径 | 定义 |
| --- | --- | --- |
| POST | `/api/Customer/AddCustomer` | `src/CustomerService/CustomerService.Api/Controllers/CustomerController.cs:10` |
| GET | `/api/Customer/GetCustomer` | `src/CustomerService/CustomerService.Api/Controllers/CustomerController.cs:10` |
| gRPC | `ICustomerService.GetCustomerAsync` | `src/CustomerService/CustomerService.Application/GrpcServices/CustomerService.cs:11` |

### ProductService

| 方法 | 路径 | 定义 |
| --- | --- | --- |
| POST | `/api/Product/CreateProduct` | `src/ProductService/ProductService.Api/Controllers/ProductController.cs:11` |
| GET | `/api/Product/GetProductDetail` | `src/ProductService/ProductService.Api/Controllers/ProductController.cs:11` |
| POST | `/api/Product/PublishProduct` | `src/ProductService/ProductService.Api/Controllers/ProductController.cs:11` |
| GET | `/api/Product/GetCategoryTree` | `src/ProductService/ProductService.Api/Controllers/ProductController.cs:11` |

### UserService

| 方法 | 路径 | 定义 |
| --- | --- | --- |
| GET | `/api/User/SyncStructure` | 同步 FreeSql 实体结构，`src/UserService/UserService.Api/Controllers/UserController.cs:6` |

## 6. 数据模型索引

| 服务 | 实体/表 | 文件 |
| --- | --- | --- |
| Auth | `UserApplication`、`UserAuthorization`、`UserToken`、`UserScope`、`Application` | `src/AuthService/AuthService/AuthService.Domain/Entity/` |
| Product | `Brand`、`Category`、`Specification`、`SpecificationValue`、`Product`、`Sku`、`SkuSpecification` | `src/ProductService/ProductService.Domain/Entity/` |
| Order | `Order`、`OrderItem` | `src/OrderService/OrderService/OrderService.Domain/Entity/` |
| Customer | `Customer`、相关枚举 | `src/CustomerService/CustomerService.Domain/Entity/` |
| User | `User` | `src/UserService/UserService.Domain/Entity/User.cs` |
| Communal | `BaseEntity`（ID、软删除、审计字段等基类） | `src/CommunalService/CommunalService/CommunalService.Domain/Entity/BaseEntity.cs` |

## 7. 运行与配置

- 每个可运行服务使用独立的 `.sln` 与 `launchSettings.json`，没有根级统一启动器。
- 开发环境优先在 `appsettings.Development.json` 中提供 AgileConfig 地址；真实 `Basic:*` 配置通常由 AgileConfig 下发。
- 必备外部依赖：PostgreSQL、Redis、AgileConfig（多数服务）、Consul。
- 典型启动示例：

```powershell
dotnet run --project src/ProductService/ProductService.Api
```

Swagger/OpenAPI 地址通常为：

```text
http(s)://<host>:<port>/swagger
```

## 8. 快速定位

| 任务 | 起点 |
| --- | --- |
| 修改共享数据库/Redis/Consul 行为 | `src/CommunalService/CommunalService/CommunalService.Domain/DependencyInjection.cs` |
| 新增商品用例 | `src/ProductService/ProductService.Application/Features/`，并在 `ProductController` 暴露接口 |
| 新增客户查询/写入 | `src/CustomerService/CustomerService.Application/Features/` |
| 修改认证协议/客户端 | `src/AuthService/AuthService/AuthService.Api/Program.cs` 与 `AuthService.Infrastructure/OpenIddict/` |
| 查看服务端口 | 对应服务的 `Properties/launchSettings.json` |
| 查看 EF Core 迁移 | `src/AuthService/AuthService/AuthService.Infrastructure/Migrations/` |
| 查看共享消息契约 | `src/CommunalService/CommunalService/CommunalService.Domain/Contracts/` |
