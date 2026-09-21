# SimpleShop 业务文档

> 更新：2026-08-31。本文描述"系统做了什么业务"，代码结构与规范见 `CODING_STANDARD.md`，链路细节与风险见 `REVIEW.md`。

## 1. 系统定位

**多平台（多租户）电商 MVP**，已跑通完整闭环：管理后台运营 + 用户商城购物（注册 → 浏览 → 加购 → 下单 → 支付 → 发货 → 签收 → 退款）。

- **平台（Platform）**：入驻运营方，拥有独立商城（独立首页装修、主题色、入口）。
- **商户（Merchant）**：挂靠在某平台下经营商品，发货与退款处理者。
- **客户（Customer）**：C 端购物用户，可浏览所有启用平台，数据按归属隔离。

**账号域分离（强制）**：
- 前台客户账号只存在于 CustomerService（`simpleshopcustomer.customer`），注册/登录走 `/gateway/customers/Register|Login`，令牌为 HS256 客户 JWT（`tenant_type=customer`）。
- 后台账号（平台/商户/运营）只存在于 UserService（`simpleshopuser."User"`），由平台管理员在后台建号，真实角色绑定在权限中心。
- 后台登录走 AuthService 的 OpenIddict 令牌端点 `/gateway/auth/Token`（password flow，公开客户端 `admin-app`，RS256 自签证书），令牌声明含 `tenant_type/permission/role`，网关验签后转 `X-Claim-*`。
- 互斥校验：客户账号走后台登录返回 `invalid_grant`；客户令牌访问后台接口 403；后台建号禁止 `customer` 角色。
- 存量数据迁移：`bash scripts/migrate-customers.sh`（默认只复制，`--purge` 清理 User 表客户，保留雪花 Id）。

## 2. 服务一览

| 服务 | HTTP | gRPC | 数据库 | 职责 |
|------|------|------|--------|------|
| Gateway | 5008 | - | - | Ocelot 路由 + JWT 验签 + RBAC（权限点↔接口路径）+ 租户声明注入 |
| Auth | 5019 | 5004 | simpleshopauth | 后台登录 OpenIddict 令牌（password flow、RS256 自签证书；EF Core） |
| User | 5011 | 5003 | simpleshopuser | 后台账号域：账号管理（列表/建号/改号/启停）+ 资料；不含客户 |
| Customer | 5280 | 5001 | simpleshopcustomer | 前台客户域：注册/登录（客户 JWT）、资料、地址簿、收藏 |
| Tool | 5080 | 5081 | simpleshoptool | 工具服务（统一文件上传）：多存储（Local/阿里云OSS/腾讯云COS/Azure Blob，AgileConfig 切换）、格式与大小限制可配 |
| Permission | 5022 | 5023 | simpleshoppermission | 权限中心：角色/权限点/用户绑定；登录解析与后台管理 |
| Product | 5058 | 5058 | simpleshopproduct | SPU/SKU、三级分类、审核上下架、图片上传 |
| Cart | 5060 | 5060 | simpleshopcart | 购物车（PostgreSQL 存储） |
| Inventory | 5062 | 5063 | simpleshopinventory | 库存锁定/扣减/释放 + 流水防重 |
| Order | 5064 | 5002 | simpleshoporder | 幂等下单、订单查询、发货/签收/取消、报表 |
| Payment | 5066 | 5066 | simpleshoppayment | 支付单、模拟确认、退款单与审批 |
| Marketing | 5072 | 5073 | simpleshopmarketing | 活动（满减/满折/满赠）、券模板/券活动/领券中心/券包、优惠计算、效果报表 |
| MerchantPlatform | 5070 | 5070 | simpleshopmerchant | 商户/平台管理 + 小程序装修配置 |
| Scheduled | - | - | simpleshopscheduled + simpleshoporder | 独立定时进程：支付超时关单 + 库存释放补偿 |
| Log | 5088 | - | Elasticsearch | 消费 pv/operation/exception 日志 → ES（带 DLQ） |

**统一文件上传（ToolService）**：
- 所有端（后台/小程序/后续端）统一走 `POST /gateway/files/Upload`（multipart，字段 `file`），返回绝对访问地址；本地存储通过 `GET /gateway/files/Content/{对象键}` 回源。
- 存储后端由 AgileConfig `FileStorage:Provider` 决定：`Local`（默认）/`AliyunOss`/`TencentCos`/`AzureBlob`；各厂商凭据、自定义域名均在 `FileStorage:{Provider}:*` 配置。
- 允许格式 `FileStorage:AllowedExtensions`、分类大小上限 `FileStorage:MaxSizeBytes`（image/document/audio/video/default，字节）均在 AgileConfig 调整，无需改代码。
- 校验顺序：扩展名白名单 → 分类大小 → 文件头魔数（防伪造扩展名）。

外部依赖：PostgreSQL `127.0.0.1:5432`（postgres/Aa123456..）、Redis `127.0.0.1:6379`、Consul `:8500`、RabbitMQ `localhost`（admin/admin123）、AgileConfig `:5000`（每服务独立应用配置）。

## 3. 角色与权限（两层租户）

权限按 `AllowedScopes` 分层：平台(1) / 商户(2) / 两者(3)。

| 内置角色 | 范围 | 权限要点 |
|----------|------|----------|
| platform-admin | 平台 | `*`（全部） |
| platform-operator | 平台 | 商品/分类/商户维护、退款审批、看板 |
| platform-finance | 平台 | 只读订单/退款/报表 |
| merchant-admin | 商户 | 本商户商品/订单/退款全权 |
| merchant-operator | 商户 | 商品维护、发货 |
| merchant-finance | 商户 | 只读订单/退款/报表 |

- 支持自定义角色与权限点；权限点绑定 `/gateway/*` 接口路径，网关动态识别（30s 缓存）。
- **数据隔离**：平台管理员看本平台，商户管理员只看本商户，客户只看自己——所有列表/详情查询按 `TenantContext` 裁剪。
- JWT 内声明：`tenant_type / platform_id / merchant_id / permission×N / role×N`；网关验签后转为 `X-Claim-*` 头传给下游，下游只信任网关。

## 4. 核心业务流程（摘要，逐步执行顺序见 REVIEW.md）

### 4.1 购物主链路（C 端）
```
客户注册/登录（CustomerService `/customers/Register|Login`，客户 JWT）→ 选平台（小程序按平台下发装修）→ 浏览/搜索 → 商品详情选 SKU
→ 加购（展示自动最优优惠金额）→ 结算（选地址 + 勾选用券，金额随优惠浮动）
→ 下单（幂等 → 营销结算占券 → 锁库存 → 落单 → 写营销记录）→ 支付（模拟确认，按实付金额）
→ 支付成功事件 → 订单已支付 + 库存扣减 + 满赠发券 → 商户发货 → 用户签收 → 完成
```

### 4.6 商城展示面（活动专区/店铺/收藏）
```
首页：会员问候卡（优惠券/收藏/订单，未登录不请求）+ 优惠专区（进行中活动与可领券卡，游客可见）
店铺页：/merchants/Shop（公开店铺信息）+ /products/List?merchantId= + 本店活动（ActiveActivities 按商户过滤）
收藏页：/customers/Favorites + 逐个商品详情（上限 20）
商品详情：主图+SKU 图图集、服务保障、配送说明、商品评价占位、吸底（店铺/购物车角标/加购/立购）
价格展示：到手价（京东/淘宝式）= 后端 FinalPrice 试算的活动/最优券更低价，商品卡与详情页
          展示"到手价 + 划线原价 + 优惠来源标签"；游客只算活动价，登录后自动计入最优券；
          列表页整页商品合并为一次批量试算（≤50 个/批），失败静默回退原价展示
```

### 4.5 营销优惠（活动 + 券）
```
运营配置：平台/商户活动（满减/满折/满赠）、平台/商户券模板、券活动（可领/满赠）、
         营销配置（活动优先 / 券优先，默认券优先）
用户：领券中心领券 → 券包（领取后 N 天有效）
结算：逐商品贪心取最优优惠；每个商品只能命中一个活动或一张券（互斥）；
      优先级按平台配置：券优先=先取最优券，无券才活动；活动优先反之；
      满赠折扣为 0，只在无任何折扣可用时命中（满 1 元赠券等）；
      0元减：优惠额必须小于商品行金额（券后价 > 0）；单行最多抵扣到 0.01 元
下单：以服务端结算结果为准写订单（总价/活动折扣/券折扣/实付 + 明细营销快照），
      支付成功后退款上限按实付；订单取消/关单回退券占用
报表：活动/券效果（参与订单数、折扣总额、赠券数）与订单/商品下钻明细
到手价：POST /marketing/FinalPrice（游客可调用），逐商品按"单商品订单"试算并取
        活动价与券后价的更低价（用户可在结算页取消勾选券，故展示最低可到手价）；
        与结算引擎共用同一套规则，金额拆分返回（原价/活动优惠/券优惠/到手价）
```


### 4.2 退款链路
```
客户申请退款（限已支付，金额累计不超实付）→ 退款单待审批(10)
→ 平台/商户审批：同意 → 退款单已退款(20) + payment.refunded 事件 → 订单已退款(60) + 库存回补
              拒绝 → 退款单拒绝(90)，无副作用
```

### 4.3 超时关单
```
定时任务每 30s：抢全局扫描锁 → 先重试上次失败的库存释放补偿 → 扫描支付超时订单
→ 单订单锁 + 条件更新关单(91) → 释放库存（失败写入补偿表下轮重试）→ 发布 order.cancelled
```

### 4.4 商户入驻
```
平台创建商户（待审核）→ 平台审核通过/拒绝 → 商户账号可登录后台运营本商户商品/订单
```

## 5. 多平台小程序装修（凯德星模式）

- 管理后台「小程序装修」按平台配置：主题色（primary/tabColor/background）、商城名称、公告、TabBar 文案、**首页轮播图**、首页模块（品牌头/公告/轮播/金刚区/分类/商品推荐，可排序）、**「我的服务」宫格**（图标/名称/跳转类型/参数）。
- 金刚区与我的服务跳转类型统一：商品列表/分类页/购物车/我的订单/收货地址/领券中心/我的券包/我的收藏（我的服务另支持刷新资料/联系客服）。
- 配置存 `platform_app_config`（发布版本号递增）；小程序启动按 `platformCode` 拉取已发布配置，动态渲染——**不同平台进入同一个小程序看到完全不同的商城**。

## 6. 数据模型（核心表）

| 服务 | 实体 | 说明 |
|------|------|------|
| User | User | 后台账号（平台/商户/运营；真实角色绑定在权限中心） |
| Customer | Customer, CustomerAddress, CustomerFavorite | 前台客户账号、地址簿与收藏（独立库，与后台账号完全分离） |
| File | StoredFile | 上传文件元数据（原始名/分类/大小/存储后端/对象键/公开地址），文件内容由存储后端保存 |
| Permission | Permission, Role, RolePermission, UserRole | 权限点/角色/映射/绑定（映射有唯一约束，重绑物理删除） |
| Product | Product, Sku, Category, Brand, UploadedFile | SKU 按编码 Upsert；分类强制 ≤3 级 |
| Cart | CartItem | 已从 Redis 迁移到 PostgreSQL |
| Inventory | Stock, StockFlow | 流水按 BizNo+SKU+动作幂等；补偿表 pending_stock_release |
| Order | Order, OrderItem, Shipment, ShipmentItem | 订单含收货快照与幂等键；状态机见 `OrderState` |
| Payment | PaymentOrder, RefundOrder, RefundOrderItem | 支付单 BizNo 唯一；退款累计限额 |
| MerchantPlatform | Platform, Merchant, PlatformConfig, PlatformAppConfig | 装修配置带发布版本 |
| Marketing | MarketingActivity(+Target), CouponTemplate, CouponActivity(+Target), UserCoupon, MarketingActivityRecord(+Item), CouponRecord(+Item), MarketingConfig | 活动范围/参与记录、券包与核销记录、每平台优惠优先级 |

## 7. 事件与锁（跨服务协同）

### 7.1 MQ Topic

| Topic | 生产者 | 消费者 |
|-------|--------|--------|
| `payment.refunded` | 退款审批 | OrderService（订单已退款）、InventoryService（回补库存） |
| `order.created` / `order.cancelled` | 下单 / 超时关单 + 主动取消 | MarketingService（cancelled：回退券占用；created 仍无消费者） |
| `payment.succeeded` | 支付确认（幂等补发） | OrderService（标记已支付）、InventoryService（扣库存）、MarketingService（满赠发券） |
| `product.created` | 创建商品 | InventoryService（初始化库存） |
| `pv.log` / `operation.log` / `exception.log` | PV 中间件 / OperationLogger / 异常中间件 | LogService → ES |

### 7.2 分布式锁键

| 锁键 | 粒度 |
|------|------|
| `lock:order:create:{customerId}` | 下单（按用户） |
| `lock:order:{orderId}` | 订单状态机互斥（支付消费/关单/发货/签收/取消/退款消费） |
| `lock:payment:order|callback|refund:{bizNo}` | 支付创建/确认/退款（按业务单号） |
| `lock:stock:{skuId}` | 库存（按 SKU） |
| `lock:job:payment-timeout-scan` | 关单全局扫描（多实例互斥） |

## 8. 前端

- **管理后台** `apps/admin-vue/`（Vue3 + Element Plus，Apple 风格设计系统在 `src/styles.css`）：http://127.0.0.1:5173 ，账号 `codexadmin / Admin123456`。路由按 `meta.permission` 守卫。
- **商城端** `apps/user-uniapp/`（UniApp，Apple 风格）：H5 http://127.0.0.1:5174 ，同源码构建微信小程序（`dist/build/mp-weixin`）。进入先选平台；主题按平台配置动态生效；「我的」含领券中心/我的券包，购物车与提交页展示优惠与券勾选。
- 雪花 ID 以字符串传输，前端禁止 `Number()` 转 ID。
- 项目路径含 `#`：Vite dev-server 会白屏，必须构建 + preview。
