# SimpleShop AI 会话交接文档

> 用途：在另一台电脑或新会话中恢复当前开发上下文。  
> 使用方式：新对话中直接说「请阅读 `AI_SESSION_HANDOFF.md`，按里面的进度继续」。

## 当前任务

按完整商城基本功能补齐服务端，并交付 **Vue 管理后台** 和 **UniApp 商城端**。日志/EFK、独立定时任务和分布式一致性加固已完成，当前重点是保持双前端可构建、可联调：

- 管理后台：登录、用户、分类、商品、订单、退款、商户、平台。
- 商城端：一套 UniApp 代码构建 H5/App 与微信小程序，覆盖浏览、搜索、购物车、下单、支付、地址、订单、退款。

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
- 业务审计日志已接入：
  - 订单创建
  - 支付确认
  - 商品创建
  - 库存锁定、扣减、释放
  - 商户入驻、商户审核
- 定时任务与一致性加固已完成：
  - 新增 `src/ScheduledService/ScheduledService` 独立 Worker。
  - 支付超时关单已从 `OrderService.Api` 迁移过去。
  - 全局扫描锁 + 订单状态锁避免多实例竞争。
  - 取消和支付消费补充锁后复检与数据库条件更新。
  - 支付确认幂等分支会补发 `payment.succeeded`。

### 双前端与端到端验证已完成

- `apps/admin-vue/` 是正式 Vue 3 + Vite + Element Plus 后台，已覆盖登录、工作台、用户、分类、商品、订单、退款、商户和平台管理；生产构建通过。
- `apps/user-uniapp/` 是 Vue 3 UniApp 商城端，同一套源码可构建 H5 与微信小程序；页面包含首页搜索、分类、商品详情/SKU、购物车、结算、地址、订单和我的。
- 服务端补齐用户注册/登录/JWT、后台建号、地址簿、收藏、商品列表/分类创建、订单列表、支付/退款列表等接口。
- Gateway 用户下游端口已改为 UserService 实际 HTTP 端口 `5003`；开发 CORS 已放开，供 5173/5174 前端访问。
- CartService 已补注册 MediatR；异常中间件会输出原始异常日志，避免只返回“系统繁忙”导致无法定位问题。
- 已实测：注册/登录 → 商品浏览 → 加购 → 创建地址 → 下单 → 支付确认 → 订单转已支付 → 发货 → 签收 → 退款 → 退款单查询。
- 可重复执行 `tests/e2e/business-flow.sh`；旧静态入口保留在 `apps/admin-web/` 和 `apps/user-miniapp/`，新开发以 `admin-vue` 和 `user-uniapp` 为准。
- 本地静态演示：
  - 管理后台：`cd apps/admin-vue && npm run dev`，或构建后把 `dist/` 静态托管在 5173。
  - 商城 H5：`cd apps/user-uniapp && npm run dev:h5`，微信小程序执行 `npm run build:mp-weixin` 后导入 `dist/build/mp-weixin`。
  - 真机/App 联调时，必须把 `src/common/request.js` 的 `BASE_URL` 从本机回环地址改成局域网或公网 Gateway 地址。

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

## RBAC 与租户隔离优化

- PermissionService 已成为独立权限中心，使用独立库 `simpleshoppermission`、Redis DB `5`、REST `5022` 和 MagicOnion gRPC `5023`。
- 权限中心已建模并种子化 `permission / role / role_permission / user_role`；内置平台管理员、平台业务员、平台财务、商户管理员、商户业务员、商户财务。
- UserService 登录通过 PermissionService MagicOnion 解析租户与权限，JWT 注入 `tenant_type`、`platform_id`、`merchant_id`、多个 `permission` 和多个 `role`。
- Gateway 统一执行 RBAC，并把已验签声明重写为下游 `X-Claim-*`；请求入口会先清空外部传入的同类头，防止伪造身份或租户范围。
- 通配权限 `*` 只表示“动作授权”，不再自动放开数据边界：
  - 平台 ID 为 0 的 legacy 管理员保持全局可见。
  - 绑定到具体平台的 `platform-admin` 只能看到本平台数据。
- 下游服务用 `TenantContext` 强制隔离商品、商户、平台、订单、支付和退款数据；发货还会复核订单归属商户。
- 用户自助接口不再信任请求体/查询串中的 `userId`：购物车、地址、收藏、订单、支付确认与退款都会强制覆盖为 JWT 当前登录人。
- Customer Self-Service 路径已在网关单独放行但仍必须携带有效登录态，避免普通商城用户被后台权限误拦截。
- Gateway CORS 已移到 RBAC 之前，修复浏览器预检 OPTIONS 被误判为未登录的问题。
- 后台 Vue 增加角色权限页面，支持自定义角色、编辑权限和账号绑定；平台/商户角色绑定校验租户 ID。

### RBAC 验证结果

- 构建验证：`dotnet build SimpleShop.slnx --no-restore` 通过，0 错误。
- 隔离测试账号（密码均为 `Op123456`）：
  - `platformop`：平台 A 业务员。
  - `merchantop`：商户 A 业务员。
  - `merchantbop`：商户 B 业务员。
- 权限边界实测：
  - `merchantop` 只见商户 A 商品；`merchantbop` 只见商户 B 商品。
  - 业务员创建平台、创建用户等未授权动作返回 403。
  - 平台财务/商户财务可读订单、退款和报表，但不能建商品、审批退款、建用户或建平台。
  - 绑定到具体平台的 `customadmin` 使用 `*` 可管理平台 A 全部功能，但平台列表只返回平台 A。
  - legacy `codexadmin` 的 `*` 保持全局可见。
- 前端产物已重新构建：`admin-vue` 与 `user-uniapp` H5 均通过。
- 后台 UI 实测：管理员登录、权限页加载、角色表显示 `*`、自定义角色创建成功。
- 商城 UI 实测：注册 → 商品详情 → 加购 → 从购物车结账 → 新增/选择地址 → 下单支付 → 订单列表 → 申请退款 → 显示已退款。
- 安全增强后的自动化 E2E：`tests/e2e/business-flow.sh` 会先注册新客户并用 JWT 执行全链路，再使用商户 token 发货，最后由客户签收，当前已通过。

## 最近修复

- 后台页面按验收意见整体重写：工作台提供当日 DAU/订单/GMV/销售商品数、日/月切换和原生 SVG 线性回归线；用户增加修改按钮；分类改为父级下拉并严格限制三级；商品增删改查跳转独立页并支持上传/富文本/SKU 规格；订单和退款详情独立展示，商户/平台审核与启停可用；权限改树形编辑并支持超级管理员新增接口权限。
- 修复 Ocelot 报表路由遗漏（`/gateway/reports/* -> /api/Report`）。
- 修复 Base 路由与自定义方法段重复导致的 Product/MerchantPlatform/Payment/Permission 多个网关路径 404。
- 修复商品三级分类判定少一档的问题；新增/移动分类都会阻止第四级和循环父子关系。
- 修复后台管理员查询订单详情被错误归属校验拦截的问题，订单详情现在返回完整主单和明细。
- 修复 Payment 退款详情路由双重拼接问题，补齐退款明细拆分和历史退款查询。
- 修复角色权限软删除造成的唯一键冲突：重新保存角色时物理清理旧 RolePermission 映射。
- 已回归通过：主链路 E2E、全额退款审批事件、库存恢复、三级分类 CRUD、上传文件读取、平台/商户/用户编辑和报表聚合。
- 后端响应中的 Snowflake 大整数已全局序列化为字符串，避免 JavaScript 超过安全整型后截断 ID；前后端请求码统一用 `Number(body.code) === 200` 判断。
- MerchantPlatform 新增平台列表接口；后台平台页解包分页 `data.items`，Kestrel 同端口 REST/gRPC 使用 `Http1AndHttp2`，独立 gRPC 端口使用明文 HTTP/2。
- UniApp 商品详情改为解包 `{product, skus}`，避免商品名、ProductId 和 SKU 丢失导致订单创建校验失败。
- UniApp 订单列表金额改用 `paymentPrice`，状态按数字映射；订单详情携带当前 `customerId` 查询，退款按钮也按数字状态判断。
- 浏览器驱动 UI 全链路已复测：登录 → 商品详情 → 加入购物车 → 去结算 → 前端新增/选择地址 → 创建订单 → 支付确认 → 订单列表显示待发货/99.90 → 申请退款 → 显示已退款。
- 上述流程的数据库节点已核验：订单主单与明细入库，Payment `Status=20`，Refund `Status=20`，Order 转 `60` 并标记全额退款，库存 lock/deduct/restore 流水一致，用户地址落库。
- `admin-vue` 与 `user-uniapp` 的 H5、微信小程序产物均已重新构建成功。
- 服务间同步调用已按分布式约束收紧：订单/定时关单调用库存使用 MagicOnion gRPC；AuthService 认证也改为通过 UserService gRPC 校验，不再硬编码身份。
- Inventory 已拆分 REST 与 gRPC 端口（5062/5063），Consul 注册 `GrpcPort` 元数据；Kestrel 对独立 gRPC 端口启用明文 HTTP/2。
- 购物车事实数据已从 Redis 迁移到 PostgreSQL + FreeSql（`cart_items`），Redis 不再作为购物车主存储。
- 库存条件更新失败（售罄/并发竞争）返回业务失败，不再抛出服务异常；全链路已验证锁定、扣减、退款恢复。
- AuthService 已补 MediatR 注册并修复自定义登录响应循环序列化问题。
- 用户名长度放宽为 64，长用户名注册不再触发数据库截断异常。
- ScheduledService 已改为真正的双库模型：`Order:sqlConnectionString` 指向订单库，`Basic:sqlConnectionString` 指向补偿独立库；补偿仓储使用 keyed FreeSql。
- AgileConfig 种子脚本已补充 ScheduledService 的订单库配置；运行库也已写入该发布配置并清理了旧时间线的重复连接串。
- Payment/Order/Inventory 退款链路已实测通过：全额退款后订单进入 `Refunded(60)`，库存按退款单号幂等恢复。
- 商城端订单状态映射修正为后端真实状态；用户申请退款会携带 SKU 数量，保证退款触发库存恢复。
- 后台订单详情改为先取主单再查明细，发货不再依赖列表返回的空 items；后台退款也会自动组装库存项。
- CartItem 增加 PlatformId，购物车结算可以正确生成跨平台订单快照。
- `SimpleShop.slnx` 曾被误删，导致 Rider 提示需要 MSBuild 才能加载；已从 Git 恢复。
- 本机安装的是 .NET SDK `10.0.111`，`global.json` 已从 `10.0.302` 调整为该版本。
- Elasticsearch 客户端 9.5 的过滤条件已改为直接构造 `TermQuery` 并放入 `BoolQuery.Filter`。
- `/metrics` 使用默认 Registry 手动导出文本；当前依赖组合编译通过。
- LogService 与全解决方案已执行构建验证，均为 0 错误。

## 已按用户要求复原的内容

- 此前的临时回退说明已过期：业务审计扩展、独立定时项目和订单状态互斥修复现已重新落地；是否继续引入 Outbox/Saga 以最新审计报告为准。

## 下一步

1. 阅读 `DISTRIBUTED_CONSISTENCY_AUDIT.md`，继续评估 Payment/Order Outbox。
2. 为 Order/Inventory MQ 消费者配置 DLQ 与重试策略。
3. 如需生产部署，将开发期 CORS 白名单从“允许任意来源”收紧为前端真实域名。

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
- Vue 后台：`apps/admin-vue/src/views`
- UniApp 商城：`apps/user-uniapp/src/pages`
- 用户扩展实体：`src/UserService/UserService.Domain/Entity/Address.cs`
- 收藏实体：`src/UserService/UserService.Domain/Entity/Favorite.cs`
- 业务蓝图：`ECOMMERCE_BUSINESS_BLUEPRINT.md`
- 项目索引：`PROJECT_INDEX.md`

## 注意事项

- 不要主动 Git commit，除非用户明确要求。
- 不要修改旧项目 `AdminService.Domin` 的拼写问题。
- 本地没有 RabbitMQ 或 Elasticsearch 是预期情况；相关服务必须能降级启动。
- 用户要求中文注释，并且注释要解释业务意图，不要只写机械说明。
- 2026-08-26 扩展业务审计、迁移定时项目并加固分布式策略后，全解决方案构建通过，0 错误。
- 2026-08-26 已跑通网关下单 → 支付 → 库存扣减 → 发货 → 签收，以及超时关单 → 库存释放。
- 2026-08-26 `dotnet build SimpleShop.slnx --no-restore` 通过；`admin-vue` 生产构建通过；`user-uniapp` 的 H5 与微信小程序构建均通过。
- 本地演示账号：后台管理员 `codexadmin / Admin123456`；当前服务地址为 Gateway `http://127.0.0.1:5008/gateway`。
- 2026-08-27 已重新执行网关 E2E 主链路、登录、用户分页、购物车平台数据、全额退款和库存恢复验证。
- 2026-08-27 RBAC、伪造租户头防护、平台/商户自助身份归属、Gateway CORS、后台权限页和 UniApp 购物车结算均已回归通过。
- 工具调用保持小步执行，避免输出过大导致中断。
