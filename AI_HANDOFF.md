# AI_HANDOFF.md — AI 协作与交接文档

> 用途：新会话（或另一台机器）恢复上下文。开场说「阅读 AI_HANDOFF.md，按里面的进度继续」。
> 最后更新：2026-08-31。

## 1. 必读文档与分工

| 文档 | 内容 | 什么时候读 |
|------|------|-----------|
| `CODING_STANDARD.md` | 代码分层规范 + **注释规范** | 写任何代码之前 |
| `BUSINESS.md` | 业务域、角色权限、数据模型、事件与锁 | 理解需求时 |
| `REVIEW.md` | 全链路执行顺序 + 风险审计 | 改交易/库存/定时链路时 |
| 本文件 | 环境、启动、协作约定 | 每次开场 |

## 2. 环境与启动

```bash
# 0) 基础设施（Docker 容器，停止状态时）
docker start postgres redis consul rabbitmq agile_config

# 1) 后端（.NET 10，解决方案 SimpleShop.slnx）
dotnet build SimpleShop.slnx
bash scripts/run-dev-services.sh          # 后台起全部服务（免 sudo，日志在 logs/runtime/）

# 2) 管理后台（路径含 #，dev-server 白屏，必须 build + preview）
cd apps/admin-vue && npm run build
npx vite preview --host 0.0.0.0 --port 5173 --outDir dist --strictPort

# 3) 商城 H5
cd apps/user-uniapp && npm run build:h5
npx vite preview --host 0.0.0.0 --port 5174 --outDir dist/build/h5 --strictPort
#    微信小程序：npm run build:mp-weixin → 导入 dist/build/mp-weixin
```

- 后台 http://127.0.0.1:5173 （`codexadmin / Admin123456`）；商城 http://127.0.0.1:5174 （手机视口验证）。
- 健康检查：各服务 `/health`；网关 `:5008/health`。
- 单独重启某服务：杀 `logs/runtime/{Name}.pid` 里的 PID，再按脚本里的 `start_service` 方式重启。

## 3. 硬性协作约定

1. **文档同步**：每一步修改（代码/配置/前端/脚本）完成后，同一次会话内更新对应文档（改业务→`BUSINESS.md`；改链路/风险→`REVIEW.md`；新增模式/陷阱→`CODING_STANDARD.md`；影响交接状态→本文件「进度日志」）。描述必须让下一个 AI 不看聊天记录就能继续。
2. **README 同步**：功能、架构、服务清单、技术栈、启动方式或演示账号发生变化时，必须同步更新根目录 `README.md`（中英双语），保持项目介绍与代码一致。
3. **不要主动 git commit / push**，除非用户明确要求。
4. **注释同步**：改业务逻辑必须同步更新类头/步骤注释（`CODING_STANDARD.md` 第 5 节）。
5. 工具调用保持小步执行，避免输出过大导致中断。

## 4. 代码现状（2026-08-31）

- **DDD 分层已完成**（含营销服务 MarketingService，5072/5073）：6 个服务（User/Payment/Order/Product/MerchantPlatform/Permission）按 CustomerService 基准迁移——控制器纯转发、业务在 Handler、验证在 Validator、查询在仓储。新代码直接按 `CODING_STANDARD.md` 写。
- **注释已补齐**：共享库核心类（锁/消息/日志/仓储）+ 全部 Feature Handler 类头 + 订单状态机枚举。
- **前端 Apple 风**：admin-vue 与 user-uniapp 均已改版（设计变量集中在 `admin-vue/src/styles.css` 与 `uniapp App.vue`）；tabBar 已带图标（PNG 由 `apps/user-uniapp/scripts/gen-*.py` 生成，可重跑）。
- **已知风险**（详见 REVIEW.md 风险节）：下单孤儿预留无 TTL、客户主动取消不释放库存、Inventory 消费者无 DLQ、`order.created/order.cancelled` 无消费者、本地多表写入无事务。

## 5. 进度日志（倒序，新条目写在最上面）

### 2026-09-16（第三轮）：营销子系统（活动 + 券 + 结算 + 报表 + 双端）
- 新建 **MarketingService**（REST 5072 / gRPC 5073 / 库 `simpleshopmarketing`，Redis db6）：11 张表（活动+范围/参与记录、券模板/券活动+范围/用户券/核销记录、每平台营销配置），已加入 slnx、`scripts/run-dev-services.sh`、`ocelot.json`（`/gateway/marketing/**`）、AgileConfig 种子（`deploy/agileconfig/init-service-configs.sql`）。
- 规则：逐商品贪心 + 券/活动互斥 + 平台配置优先级（1 活动优先 / 2 券优先，缺省券优先）；满赠只在无折扣可用时命中；0元减要求券后价 > 0；单行最多抵扣到 0.01。
- 订单链路：`CreateOrderHandler` 调 gRPC `SettleAsync`（占券）→ 锁库存 → 落单（含活动/券折扣与逐商品营销快照）→ `CommitAsync` 记录；失败/取消回退券。`order.cancelled` 现在由主动取消也发布（营销消费回退券）；`payment.succeeded` 新增营销消费者发满赠券。Order/OrderItem 新增优惠字段（自动加列）。
- 后台：营销管理页（活动/券模板/券活动/报表/配置，权限点 `marketing:read|create`，已加入 Permission 种子与网关静态权限）；订单详情展示活动/券与逐商品折扣。
- 商城：领券中心、我的券包（profile 入口 + 新 cell 图标）、购物车/提交页展示优惠与券勾选（提交页可选用券，金额浮动），订单详情展示逐商品优惠。
- 验证：`tests/e2e/marketing-flow.sh` 26 项通过（创建→领券→优先级→下单→支付→报表→满赠）；`verify-marketing-ui.js` 4 项 UI 通过；`business-flow.sh`、`verify-validations.sh` 回归通过。
- 修复：Permission 种子新增权限点用 `index+1` 撞主键（改为 max+1）；种子阶段不能依赖雪花 AOP；FreeSql 批量插入传 `IReadOnlyCollection` 命中单实体重载（改 `ToList()`）。
- 已知边界（详见 REVIEW.md P1/P2）：支付/退款金额仍以客户端提交为准；营销服务不可用会阻断下单；退款不返券；报表内存聚合。

### 2026-09-16（第二轮）：全量参数校验补全（前端 + 后端）
- 后端新增 30+ 个 FluentValidation Validator：PaymentService（创建/确认/退款/审批/拒绝/列表分页，此前 0 校验）、CartService（加购/查询/移除）、InventoryService（锁/扣/释放，拦截空明细与非法数量）、OrderService（列表/详情/发货/签收/取消，CreateOrder 补幂等键与 StockItems 规则）、ProductService（列表/详情/上下架/分类启停删除，Create/Save SKU 校验收紧）、UserService（登录长度/用户分页/启停/删地址/收藏）、MerchantPlatform（审核/状态枚举/详情/平台启停，列表分页内联兜底）、Permission（编辑角色）、Customer（用户名/手机号/性别/生日）。
- 安全顺带修复：`/orders/Cancel` 控制器强制回填 `OverrideCustomerScope`（此前客户可伪造请求体越权取消他人订单）；图片上传增加扩展名白名单 + 文件头魔数并规范化 ContentType；平台装修 JSON 补空值/100KB 上限。
- 前端：新增 `apps/admin-vue/src/utils/validators.js` 与 `apps/user-uniapp/src/common/validators.js`（手机/邮箱/颜色/编码正则 + 规则工厂 + trim）；admin 全部表单接规则（Login/Users/Categories/Orders 发货/OrderDetail 退款可退余额/RefundDetail 拒绝原因/Merchants/Platforms/ProductEdit 描述与 SKU/AppDesign 装修），uniapp（登录/注册含确认密码与强度/地址/购物车/商品数量库存/退款确认/结算/搜索）。
- 验证：`/tmp/opencode/verify-validations.sh` 26 项通过（非法参数 400、合法请求 200）；`tests/e2e/business-flow.sh` 主链路通过；Chromium CDP 后台三用例通过（空表单字段级提示、登录、用户弹窗手机号正则）。前后端均已重新构建，preview 5173/5174 已重启。
- 注意：重启脚本 `logs/runtime/*.pid` 包含前端 preview 的 pid，重启后端时不要遍历整个目录杀进程。

### 2026-09-16：后台登录过期不跳转修复
- 现象：token 过期后接口返回 401「请先登录」，后台只弹提示不跳登录页。
- 前端 `apps/admin-vue/src/api/request.js`：响应拦截器识别 HTTP 401 / body.code 401 → 清 `admin_token`+`admin_user` 并跳 `#/login`（3s 去重，登录页内不弹"过期"）；`router/index.js` 守卫补 JWT `exp` 本地校验，过期直接跳登录。
- 网关 `Gateway/Ocelot.ApiGateway/Middleware/AdminAuthorizationMiddleware.cs`：① 畸形 token 解码抛 `ArgumentException` 未捕获导致 500，现与 `SecurityTokenException` 一并视为未登录返回 401；② `/gateway/reports/**` 原只精确匹配 `/gateway/reports`，`/reports/Report` 可匿名访问，现按 `dashboard:view` 前缀保护。
- 验证：Chromium CDP 三用例通过（本地过期 token 守卫跳转 / 服务端 401 拦截器跳转并清会话 / 正常登录不被误跳）；网关 curl 复测 401/200 符合预期。改动后 admin-vue 已重新构建并重启 preview。

### 2026-08-31（第四轮）：文档重构 + 注释补齐
- 应用户要求删除全部 8 份旧文档，重构为 4 份：`CODING_STANDARD.md` / `BUSINESS.md` / `AI_HANDOFF.md` / `REVIEW.md`。
- 给共享库核心类（分布式锁/消息信封/仓储基类）、订单状态机枚举、33+ 个 Feature Handler 补详细注释；注释规范写入 `CODING_STANDARD.md` 第 5 节。
- 全量构建 0 错误。

### 2026-08-31（第三轮）：DDD 分层迁移
- 参考 CustomerService 把 6 个服务的 API 层业务/验证迁到 Application/Infrastructure；新增 `ApiResults`；PermissionService 补注册 MediatR 管道。完整交易链路冒烟通过（下单→支付→已支付→退款→审批→已退款）。

### 2026-08-31（第二轮）：列表分页修复
- 修复 FreeSql `.Page()` 误传偏移量导致第二页无数据的全局 bug（5 服务 7 处）。

### 2026-08-31（第一轮）：前端苹果风
- admin-vue 全站 Apple 风 + user-uniapp 苹果风改版（两轮精细化）+ tabBar 图标；工作台移除线性回归/预测带。

## 6. 历史设计决策（为什么是现在这样）

| 决策 | 背景 |
|------|------|
| 独立 ScheduledService | 定时任务与请求驱动服务解耦；多实例用全局扫描锁互斥 |
| AgileConfig 每服务独立配置 | 早期共用配置导致串库（Scheduled 连到 Order 库），已各自独立 |
| FreeSql 统一 ORM（Auth 除外） | Auth 先行用了 EF Core + OpenIddict，保留 |
| gRPC 内部通信 / REST 仅对外 | 服务间 MagicOnion（MessagePack），网关聚合 REST |
| 平台/商户两层权限 | 需求：平台管理员全权，业务员/财务按权限点隔离，A 平台看不了 B 平台数据 |
| 小程序按平台配置动态渲染 | 参考凯德星：不同平台入口展示完全不同页面 |
| 雪花 ID 前端保留字符串 | 超出 JS Number 安全范围曾导致商品新增失败 |
| 前后端双重验证 | 后台 UI 大批量修复期的结论：前端提示字段级错误，后端 Validator 兜底 |
| UI 采用 Apple 设计系统 | 用户要求「精致」；admin 集中在 styles.css 变量，uniapp 在 App.vue 基线 |

## 7. 注意事项

- 项目路径含 `#`：不要用 Vite dev-server；uniapp H5 的 input 无原生 placeholder（Playwright 用 `locator('input')` 定位）；同源 hash 跳转后要 `reload()` 才能加载新构建。
- uniapp tabBar 图标必须 PNG：用 `scripts/gen-tabbar-icons.py` / `gen-cell-icons.py` 重新生成。
- 演示账号：后台 `codexadmin / Admin123456`。
- 本机服务进程可能被会话事件回收：冒烟前先 `/health` 检查网关 5008 与 5062（Inventory）。
