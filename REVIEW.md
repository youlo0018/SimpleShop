# REVIEW.md — 后台链路执行顺序与风险审计

> 更新：2026-08-31。供人工 review 使用，以当前源码为准（业务背景见 `BUSINESS.md`，分层规则见 `CODING_STANDARD.md`）。
> 标注：🔒=Redis 分布式锁 ✋=幂等 📤=MQ 事件 🗂=租户过滤 💾=落库。路径相对项目根。

---

## 第一部分：链路执行顺序

### 链路 0：所有请求的通用前置

```
① 前端 axios 拦截器附加 Bearer token（apps/admin-vue/src/api/request.js）
② Gateway Program.cs：CORS（先于 RBAC，否则 OPTIONS 预检 401）
③ AdminAuthorizationMiddleware：
   清除伪造 X-Claim-* → 验 JWT → 注入租户头 → 解析所需权限码（硬编码字典→GET 前缀→权限中心动态目录 30s 缓存）
   → 客户自查接口放行 / 后台身份须持有 * 或对应权限码
④ Ocelot 按 ocelot.json 转发（Consul 服务发现）
⑤ 下游：ExceptionLoggingMiddleware → PageViewLoggingMiddleware → Controller
⑥ TenantContext 读取 X-Claim-* → 查询按租户裁剪
```

### 链路 1：登录 `/users/Login`

1. `LoginCommandHandler`：按用户名查 User → 校验 `IsEnabled` + `SHA256(password+Salt)`
2. `PermissionCenterClient.ResolveAsync()`：gRPC 调权限中心 → `{tenantType, platformId, merchantId, permissions, roles}`
3. `AdminTokenIssuer.CreateToken()`：签发 JWT（租户声明+权限声明，12h）
4. 返回 `{token, user}`；前端存 localStorage `admin_token`

### 链路 2：注册 `/users/Register`

`RegisterCommandHandler`：查重 → 盐+散列落库（Role=customer）→ 解析权限 → 签发令牌（自动登录）。字段校验在 `RegisterValidator`。

### 链路 3：后台建号/改号 `/users/Create`、`/users/Update`

Validator（用户名 3-64、密码强度、手机/邮箱正则）→ Handler：查重 → 盐+散列落库 → `PermissionCenterClient.AssignRoleAsync()` 把真实角色写入权限中心 UserRole（登录时才生效）。

### 链路 4：列表查询通用模式（用户/订单/商品/支付/退款/商户/平台）

Handler 构建仓储分页查询：`!IsDeleted` → `WhereIf` 业务过滤 → 🗂 租户裁剪 → `CountAsync`（total 与取数同条件）→ `Page(页码, 大小)`。返回 `{items, total, page, pageSize}`，雪花 ID 序列化为字符串。

### 链路 5：工作台报表 `/reports/Report`

`ReportController.Get()` 直查 Order+OrderItem：当日 DAU/订单/GMV/销量 + 30 日（或月度）趋势；前端纯 SVG 渲染实际值 + 移动平均。

### 链路 6：商品管理

- **创建** `/products/CreateProduct`：`CreateProductValidator`（名称/主图/SKU 价格库存/编码去重）→ Handler：租户回填 → 分类存在校验 → SKU 编码唯一 → 💾 商品+SKU → 📤 `product.created`
- **编辑** `/products/Update`：`SaveProductCommandHandler` → 仓储 `UpdateProductWithSkusAsync`（主表更新 + SKU 按编码 Upsert）
- **上/下架** `/products/PublishProduct`、`/products/OffShelf`：租户归属校验在 Handler（403）
- **分类** `/products/CreateCategory|UpdateCategory|DisableCategory|DeleteCategory`：3 级上限与成环校验在 Handler；删除前检查子分类与在挂商品

### 链路 7：下单主链路 `/orders/Create`（核心）

1. 控制器绑定 `CustomerId = tenant.UserId`（只允许给自己下单）
2. `CreateOrderValidator`（管道自动执行）
3. `CreateOrderHandler`：
   - ✋ 幂等键：内存缓存 → 数据库查重（重复提交拒绝）
   - 🔒 `lock:order:create:{customerId}`（拿不到→「下单繁忙」）
   - **先锁库存**（gRPC→链路 8）：失败直接返回「库存不足」（尚未落单）
   - 💾 订单 + 明细逐条插入；失败 → gRPC 释放刚锁的库存（同步补偿）
   - 📤 `order.created` → ✋ 成功写入幂等缓存（10 分钟）→ 操作审计

### 链路 8：库存锁定（InventoryService gRPC）

`LockStockHandler`：按 SkuId 升序（固定顺序防死锁）→ 🔒 `lock:stock:{skuId}` → ✋ 流水查重 → 💾 条件更新（可售↓锁定↑）+ 流水；任一失败 → 补偿释放已锁部分（bizNo 加 `:lock-compensate` 后缀防重）。

### 链路 9：支付创建/确认 `/payments/Create`、`/payments/Confirm`

- **Create** `CreatePaymentHandler`：🔒 `lock:payment:order:{bizNo}` → ✋ BizNo 幂等返回 → 💾 待支付单（10）
- **Confirm** `ConfirmPaymentHandler`：🔒 `lock:payment:callback:{bizNo}` → 客户归属校验 →
  - 已支付(20)：✋ **补发** `payment.succeeded`（防「落库成功但发事件失败」）
  - 待支付(10)：💾 `MarkPaidAsync` 条件更新 → 📤 `payment.succeeded`（含 stockItems）→ 审计

### 链路 10：支付成功消费（双下游）

- **OrderService** `PaymentSucceededConsumer`（队列 `order.payment.succeeded`）：🔒 `lock:order:{id}`（锁不到→Nack **requeue**）→ 锁内重读复检 → 💾 条件更新已支付
- **InventoryService**（队列 `inventory.payment.succeeded`）：逐 SKU 🔒 → ✋ 流水幂等 → 💾 锁定转已扣；⚠️ 异常 Nack requeue:false（无 DLQ）

### 链路 11：发货/签收/取消

- **发货** `/orders/Shipment`：`CreateShipmentHandler`：无锁预检（已支付）→ 🔒 `lock:order:{id}` → 锁内重读 + 租户归属（403）→ 💾 发货单+明细 → 条件更新订单已发货(40)
- **签收** `/orders/Receive`：🔒 `lock:order:{id}` → 归属+状态校验 → 💾 包裹已签收 + 订单已完成(50)
- **取消** `/orders/Cancel`：`CancelOrderHandler`：归属（客户本人 / 平台本平台 / 商户本商户）→ `CanCancel` → 🔒 → 💾 `TryCancelAsync` 条件更新（数据库兜底防非法回退）→ 90

### 链路 12：退款 `/payments/Refund` → `ApproveRefund` / `RejectRefund`

1. **申请** `RefundPaymentHandler`：🔒 `lock:payment:refund:{bizNo}` → 已支付校验 → 租户归属 → ✋ 累计退款限额 → 💾 退款单(10) + 明细。不发事件。
2. **同意** `ApproveRefundCommandHandler`：状态机(10) + 权限（平台通吃/商户本户）→ 💾 `MarkRefundedAsync` → 计算 isAllRefund → 📤 `payment.refunded`
3. **拒绝**：条件更新状态 90 + 原因。
4. **消费**：OrderService（订单→60）、InventoryService（🔒+✋流水→库存回补）。

### 链路 13：支付超时关单（ScheduledService）

`PaymentTimeoutCloseJob` 每 30s：🔒 `lock:job:payment-timeout-scan`（waitTimeout=0，抢不到跳过）→ 重试 `pending_stock_release` 补偿 → 扫描过期待支付订单 → 逐单 🔒 `lock:order:{id}` + 锁内复检 → 💾 `TryCloseAsync`(91) → gRPC 释放库存（失败写补偿表）→ 📤 `order.cancelled`。

### 链路 14：权限链路

- **登录解析**（PermissionGrpcService.ResolveAsync）：UserRole → Role → platform-admin 或无绑定返回 `*`/customer → 否则联查 RolePermission×Permission 取权限码。
- **角色绑定**（AssignRoleAsync / BindUserCommandHandler）：customer/空 → 软删绑定；平台角色必须带 platformId、商户角色必须带 merchantId；一用户仅一条有效绑定。
- **后台管理**（PermissionController → 7 个 Handler）：角色/权限 CRUD；`ReplaceRolePermissionsAsync` 物理重写映射（唯一约束）。

### 链路 15：商户/平台/装修

- 商户：创建（平台强制归属）→ 审核（`ReviewMerchantHandler` 归属+状态机）→ 更新/启停。
- 平台：CRUD + 启停（停用后 MiniAppPlatforms 不再返回）。
- 装修：`Save`（发布则版本+1）→ 小程序 `/platform-configs/MiniApp?platformCode=` 下发已发布配置 → 前端动态渲染。

### 链路 16：日志埋点（横切）

PV 中间件（`X-Gateway-PV` 防重复计数）/ OperationLogger（关键动作）/ 异常中间件 → 脱敏 → 📤 三个 log topic → LogService 消费 → ✋ eventId 写 ES（按天索引）；失败进 DLQ `logservice.logs.dlq`。查询 `GET /gateway/logs/{kind}`（`X-Api-Key`，权限 `report:read`）。

---

## 第二部分：风险审计（review 重点）

### P0

| # | 风险 | 位置 | 现状与建议 |
|---|------|------|-----------|
| 1 | **下单孤儿预留** | 链路 7 锁库存成功后、落单前崩溃 | 库存被长期占用。有 `pending_stock_release` 补偿表但无 TTL；建议增加预留过期时间或对账任务 |
| 2 | **客户主动取消不释放库存** | 链路 11 取消 | 只有超时关单会释放；取消后的订单不再被扫描，占用需人工处理。建议取消时同步释放或纳入补偿表 |
| 3 | **本地多表写入无事务** | 订单+明细、商品+SKU、发货单+明细 | FreeSql 分次写入，中途失败产生半截数据（Saga 起点完整性受影响）。建议仓储级工作单元 |

### P1

| # | 风险 | 位置 | 建议 |
|---|------|------|------|
| 4 | **业务消费者无 DLQ** | Order/Inventory 的 MQ 消费者异常时 `Nack(requeue:false)` 直接丢消息 | 复制 LogService 的 DLQ 模式（死信交换机+重放入口） |
| 5 | **缺 Outbox** | 支付确认/下单/关单「本地写入→发事件」非原子 | 引入 Outbox 表 + 中继任务；幂等消费端已就绪 |
| 6 | **`order.created` / `order.cancelled` 无消费者** | 预留事件 | 接线前先补消费方，否则是死信噪音 |

### P2

| # | 风险 | 说明 |
|---|------|------|
| 7 | 网关权限双轨 | `ProtectedActions` 硬编码字典与权限中心动态目录并存；新增接口需两处同步 |
| 8 | 签收锁粒度 | ReceiveShipment 已按订单锁（历史问题已修复），但多包裹并发签收仍建议锁后复检包裹列表 |
| 9 | 禁用账号的在线令牌 | UpdateStatus 只挡新登录，已签发 JWT 到期前仍有效（12h）；高危操作可考虑网关侧黑名单 |

### review 方法建议

1. 按 `BUSINESS.md` 4.x 的业务流程走一遍，对照本文对应链路小节逐步打断点/看日志（`logs/runtime/*.log`）。
2. 每条链路的锁/幂等/补偿注释都写在 Handler 类头与步骤注释里，与本文互为索引。
3. 改动链路后更新本文对应小节（协作约定见 `AI_HANDOFF.md` 第 3 节）。
