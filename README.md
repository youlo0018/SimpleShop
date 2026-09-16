# SimpleShop

> 多平台微服务电商系统 · 管理后台 + 用户商城 + 营销中心
> Multi-tenant Microservice E-commerce Platform · Admin Console + Storefront + Marketing Hub

[简体中文](#简体中文) | [English](#english)

---

## 简体中文

### 项目简介

SimpleShop 是一个基于 **.NET 10 微服务** 与 **Vue 3 / UniApp** 的全栈电商系统 MVP，跑通了从注册、浏览、加购、下单、支付、发货、签收到退款的完整闭环，并内置**多平台（多租户）运营、营销活动与优惠券、库存、权限中心、报表、日志**等能力。

- **微服务架构**：12 个后端服务 + Ocelot 网关，服务间 gRPC（MagicOnion）通信，Consul 服务发现，RabbitMQ 事件驱动，AgileConfig 配置中心。
- **DDD 分层**：Api / Application / Domain / Infrastructure 四层，控制器纯转发，业务在 Handler，验证在 Validator，查询在仓储。
- **多租户隔离**：平台 → 商户 → 客户三层数据隔离，网关验签并注入租户声明，下游只信任网关注入的 `X-Claim-*`。
- **前后端双重校验**：FluentValidation 管道 + 前端统一校验工具，字段级错误提示。
- **双前端**：管理后台（Vue 3 + Element Plus，Apple 风格）与用户商城（UniApp，H5 与微信小程序同源）。

### 核心功能

| 模块 | 能力 |
|------|------|
| 交易闭环 | 幂等下单、库存锁定/扣减/释放、模拟支付、支付超时关单、发货/签收/取消、退款申请与审批（累计限额） |
| 营销中心 | 平台/商户活动（满减/满折/满赠）、券模板、券活动、领券中心与券包；逐商品贪心 + 券/活动互斥 + 平台优先级配置；订单优惠快照与效果报表 |
| 多平台小程序 | 按平台配置主题色、公告、TabBar、首页模块（凯德星模式），同一小程序按平台动态渲染 |
| 权限中心 | 角色/权限点/用户绑定，平台与商户两层权限；网关 RBAC（权限点 ↔ 接口路径） |
| 商品与库存 | SPU/SKU、三级分类、审核上下架、图片上传（魔数校验）、库存流水幂等与补偿 |
| 报表与日志 | 工作台经营报表、活动/券效果报表；PV/操作/异常日志经 RabbitMQ 入 Elasticsearch |

### 系统架构

| 服务 | HTTP | gRPC | 数据库 | 职责 |
|------|------|------|--------|------|
| Gateway | 5008 | - | - | Ocelot 路由 + JWT 验签 + RBAC + 租户声明注入 |
| Auth | 5019 | 5004 | simpleshopauth | OpenIddict 授权（EF Core） |
| User | 5011 | 5003 | simpleshopuser | 注册/登录（JWT）、地址簿、收藏 |
| Permission | 5022 | 5023 | simpleshoppermission | 角色/权限点/用户绑定，登录权限解析 |
| Product | 5058 | 5058 | simpleshopproduct | SPU/SKU、三级分类、上下架、图片上传 |
| Cart | 5060 | 5060 | simpleshopcart | 购物车（PostgreSQL） |
| Inventory | 5062 | 5063 | simpleshopinventory | 库存锁定/扣减/释放 + 流水防重 + 补偿表 |
| Order | 5064 | 5002 | simpleshoporder | 幂等下单、状态机、发货/签收/取消、报表 |
| Payment | 5066 | 5066 | simpleshoppayment | 支付单、模拟确认、退款与审批 |
| Marketing | 5072 | 5073 | simpleshopmarketing | 活动/券/优惠计算/领券/效果报表 |
| MerchantPlatform | 5070 | 5070 | simpleshopmerchant | 平台/商户管理、小程序装修配置 |
| Scheduled | - | - | simpleshopscheduled | 支付超时关单 + 库存释放补偿 |
| Log | 5088 | - | Elasticsearch | 消费 pv/operation/exception 日志 |

> 基础设施：PostgreSQL、Redis、Consul、RabbitMQ、AgileConfig（Docker 容器）。

### 技术栈

- **后端**：.NET 10、MediatR、FluentValidation、FreeSql（PostgreSQL）、MagicOnion（gRPC）、RabbitMQ、Consul、AgileConfig、Yitter 雪花 ID、OpenIddict
- **前端**：Vue 3、Vite、Element Plus、Pinia、UniApp（H5 / 微信小程序）
- **实践**：DDD 四层、CQRS 风格 Handler、分布式锁与幂等、事件驱动 + 补偿、统一响应与全局异常、字段级校验、审计与链路日志

### 快速开始

```bash
# 0) 基础设施（首次启动）
docker start postgres redis consul rabbitmq agile_config

# 1) 后端（.NET 10）
dotnet build SimpleShop.slnx
bash scripts/run-dev-services.sh        # 后台启动全部服务，日志在 logs/runtime/
#    健康检查：各服务 /health；网关 http://127.0.0.1:5008/health

# 2) 管理后台（路径含 #，必须 build + preview）
cd apps/admin-vue && npm install && npm run build
npx vite preview --host 0.0.0.0 --port 5173 --outDir dist --strictPort

# 3) 用户商城 H5
cd apps/user-uniapp && npm install && npm run build:h5
npx vite preview --host 0.0.0.0 --port 5174 --outDir dist/build/h5 --strictPort
#    微信小程序：npm run build:mp-weixin → 导入 dist/build/mp-weixin
```

- 管理后台：http://127.0.0.1:5173
- 用户商城：http://127.0.0.1:5174 （建议手机视口）
- 演示账号：后台 `codexadmin / Admin123456`

### 目录结构

```
SimpleShop/
├── src/                 # 后端微服务（Auth/User/Permission/Product/Cart/Inventory/Order/Payment/Marketing/...）
├── Gateway/             # Ocelot 网关（路由 + 鉴权中间件）
├── apps/
│   ├── admin-vue/       # 管理后台（Vue3 + Element Plus）
│   └── user-uniapp/     # 用户商城（UniApp：H5 + 微信小程序）
├── tests/e2e/           # 端到端脚本（主链路、营销链路、冒烟）
├── scripts/             # 启动脚本
├── deploy/              # AgileConfig / EFK 部署配置
├── BUSINESS.md          # 业务文档
├── CODING_STANDARD.md   # 分层与注释规范
├── REVIEW.md            # 链路执行顺序与风险审计
└── AI_HANDOFF.md        # AI 协作与交接文档
```

### 文档

| 文档 | 内容 |
|------|------|
| `BUSINESS.md` | 业务域、角色权限、数据模型、事件与锁 |
| `CODING_STANDARD.md` | 四层职责、Validator 规范、注释规范 |
| `REVIEW.md` | 全链路执行顺序、风险审计与改进建议 |
| `AI_HANDOFF.md` | 环境启动、协作约定、进度日志 |

### 端到端验证

```bash
bash tests/e2e/business-flow.sh    # 注册 → 下单 → 支付 → 发货 → 签收
bash tests/e2e/marketing-flow.sh   # 建活动/券 → 领券 → 优先级 → 下单抵扣 → 报表 → 满赠
```

---

## English

### Overview

SimpleShop is a full-stack e-commerce MVP built on **.NET 10 microservices** and **Vue 3 / UniApp**. It covers the complete loop from sign-up, browsing, cart and checkout to payment, shipping, delivery confirmation and refunds, with built-in **multi-tenant operations, promotions & coupons, inventory, RBAC, reporting and logging**.

- **Microservices**: 12 backend services behind an Ocelot gateway; inter-service gRPC (MagicOnion), Consul service discovery, RabbitMQ eventing, AgileConfig configuration center.
- **DDD layering**: Api / Application / Domain / Infrastructure. Controllers only forward, business lives in Handlers, validation in Validators, queries in Repositories.
- **Multi-tenancy**: Platform → Merchant → Customer isolation. The gateway validates JWTs and injects trusted tenant claims (`X-Claim-*`); downstream services trust nothing else.
- **Double validation**: FluentValidation pipeline on the backend plus shared frontend validators with field-level error messages.
- **Two frontends**: Admin console (Vue 3 + Element Plus, Apple-style) and storefront (UniApp, H5 and WeChat Mini Program from one codebase).

### Key Features

| Area | Highlights |
|------|-----------|
| Commerce | Idempotent order placement, stock lock/deduct/release, simulated payment, payment-timeout close, shipping/receipt/cancel, refund apply & approval with cumulative limits |
| Marketing | Platform/merchant campaigns (amount off / percentage off / free coupon gift), coupon templates & coupon activities, coupon center and wallet; greedy per-item discount with campaign/coupon exclusivity and a platform-level priority switch; discount snapshots on orders and effect reports |
| Multi-platform Mini Program | Theme colors, notice, TabBar and home modules configured per platform (CapitaStar-style dynamic rendering) |
| RBAC | Roles, permission points and user bindings for platform and merchant scopes; gateway RBAC mapping permission points to API paths |
| Catalog & Stock | SPU/SKU, 3-level categories, publish/unpublish review, image upload with magic-byte checks, idempotent stock flows and compensation |
| Reporting & Logs | Dashboard reports, campaign/coupon effect reports; PV/operation/exception logs shipped to Elasticsearch via RabbitMQ |

### Architecture

| Service | HTTP | gRPC | Database | Responsibility |
|---------|------|------|----------|----------------|
| Gateway | 5008 | - | - | Ocelot routing + JWT validation + RBAC + tenant claims |
| Auth | 5019 | 5004 | simpleshopauth | OpenIddict authorization (EF Core) |
| User | 5011 | 5003 | simpleshopuser | Registration/login (JWT), addresses, favorites |
| Permission | 5022 | 5023 | simpleshoppermission | Roles/permissions/bindings, login permission resolution |
| Product | 5058 | 5058 | simpleshopproduct | SPU/SKU, categories, publishing, image upload |
| Cart | 5060 | 5060 | simpleshopcart | Shopping cart (PostgreSQL) |
| Inventory | 5062 | 5063 | simpleshopinventory | Stock lock/deduct/release, idempotent flows, compensation |
| Order | 5064 | 5002 | simpleshoporder | Idempotent orders, state machine, shipping/receipt/cancel, reports |
| Payment | 5066 | 5066 | simpleshoppayment | Payment orders, simulated confirm, refunds & approval |
| Marketing | 5072 | 5073 | simpleshopmarketing | Campaigns/coupons, discount engine, coupon center, reports |
| MerchantPlatform | 5070 | 5070 | simpleshopmerchant | Platform/merchant management, mini-program design config |
| Scheduled | - | - | simpleshopscheduled | Payment-timeout close + stock release compensation |
| Log | 5088 | - | Elasticsearch | Consumes pv/operation/exception logs |

> Infrastructure: PostgreSQL, Redis, Consul, RabbitMQ, AgileConfig (Docker containers).

### Tech Stack

- **Backend**: .NET 10, MediatR, FluentValidation, FreeSql (PostgreSQL), MagicOnion (gRPC), RabbitMQ, Consul, AgileConfig, Yitter snowflake IDs, OpenIddict
- **Frontend**: Vue 3, Vite, Element Plus, Pinia, UniApp (H5 / WeChat Mini Program)
- **Practices**: DDD layering, CQRS-style handlers, distributed locks & idempotency, event-driven flows with compensation, unified responses & global exception handling, field-level validation, auditing and tracing

### Getting Started

```bash
# 0) Infrastructure (if stopped)
docker start postgres redis consul rabbitmq agile_config

# 1) Backend (.NET 10)
dotnet build SimpleShop.slnx
bash scripts/run-dev-services.sh        # starts all services in background, logs under logs/runtime/
#    Health checks: /health on each service; gateway at http://127.0.0.1:5008/health

# 2) Admin console (the repo path contains '#', so build + preview is required)
cd apps/admin-vue && npm install && npm run build
npx vite preview --host 0.0.0.0 --port 5173 --outDir dist --strictPort

# 3) Storefront H5
cd apps/user-uniapp && npm install && npm run build:h5
npx vite preview --host 0.0.0.0 --port 5174 --outDir dist/build/h5 --strictPort
#    WeChat Mini Program: npm run build:mp-weixin → import dist/build/mp-weixin
```

- Admin console: http://127.0.0.1:5173
- Storefront: http://127.0.0.1:5174 (mobile viewport recommended)
- Demo account: `codexadmin / Admin123456`

### Repository Layout

```
SimpleShop/
├── src/                 # Backend microservices (Auth/User/Permission/Product/Cart/Inventory/Order/Payment/Marketing/...)
├── Gateway/             # Ocelot gateway (routing + authorization middleware)
├── apps/
│   ├── admin-vue/       # Admin console (Vue3 + Element Plus)
│   └── user-uniapp/     # Storefront (UniApp: H5 + WeChat Mini Program)
├── tests/e2e/           # End-to-end scripts (main flow, marketing flow, smoke)
├── scripts/             # Startup scripts
├── deploy/              # AgileConfig / EFK deployment configs
├── BUSINESS.md          # Business domain documentation
├── CODING_STANDARD.md   # Layering & commenting standards
├── REVIEW.md            # Request-flow ordering and risk audit
└── AI_HANDOFF.md        # Collaboration & handover notes
```

### Documentation

| Document | Contents |
|----------|----------|
| `BUSINESS.md` | Business domains, roles & permissions, data model, events and locks |
| `CODING_STANDARD.md` | Layer responsibilities, validator rules, commenting standards |
| `REVIEW.md` | Full request-flow ordering, risk audit and improvement backlog |
| `AI_HANDOFF.md` | Environment setup, collaboration rules, progress log |

### End-to-End Checks

```bash
bash tests/e2e/business-flow.sh    # register → order → pay → ship → receive
bash tests/e2e/marketing-flow.sh   # campaigns/coupons → claim → priority → checkout discount → reports → gift coupon
```

---

## License

[MIT](LICENSE)
