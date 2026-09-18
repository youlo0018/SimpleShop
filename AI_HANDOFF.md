# AI_HANDOFF.md — AI 协作与交接文档

> 用途：新会话（或另一台机器）恢复上下文。开场说「阅读 AI_HANDOFF.md，按里面的进度继续」。
> 最后更新：2026-09-18。

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
- 健康检查：各服务 `/health`；网关 `:5008/health`。**共 14 个后端服务**（含 Customer 5280/5001、File 5080/5081）。
- 单独重启某服务：按端口找真实进程 PID（`ss -ltnp | grep :端口`）kill 后重启（`logs/runtime/{Name}.pid` 里是 `dotnet run` 包装进程，杀它可能留下旧子进程占端口）。
- 演示账号：平台超管 `codexadmin/Admin123456`（显式绑定 platform-admin）；商户 `demo-merchant/Demo123456`、`merchantop/Op123456`；客户 `demo_user_01~08/Test123456`。
- 演示平台编码 **`DEMOPL`**（平台编码规则：6 位字母，创建时用户输入；商户编号 = 平台编码 + 雪花 Id）。
- 小程序平台在 `apps/user-uniapp/src/common/platform-config.js` 的 `PLATFORM_CODE` 锁定（默认 DEMOPL），应用内不提供平台切换。
- 测试数据：`node scripts/seed-test-data.js`（dummyjson 商品 + 演示平台/商户/活动/券/用户/订单）；测试会创建带占位图的临时商品，跑完可清理（见 REVIEW 风险）。
- 回归脚本（`tests/e2e/`）：`api-regression.sh`(114) / `ui-regression.js`(46，需 5173+5174 preview) / `full-chain.sh`(104) / `marketing-flow.sh`(26) / `business-flow.sh`。
- 文件存储切换：AgileConfig（appId=`FileService`）`FileStorage:Provider` = Local（默认）/AliyunOss/TencentCos/AzureBlob，凭据在 `FileStorage:{Provider}:*`；格式白名单 `AllowedExtensions`、分类大小 `MaxSizeBytes` 均在此配置。

## 3. 硬性协作约定

1. **文档同步**：每一步修改（代码/配置/前端/脚本）完成后，同一次会话内更新对应文档（改业务→`BUSINESS.md`；改链路/风险→`REVIEW.md`；新增模式/陷阱→`CODING_STANDARD.md`；影响交接状态→本文件「进度日志」）。描述必须让下一个 AI 不看聊天记录就能继续。
2. **README 同步**：功能、架构、服务清单、技术栈、启动方式或演示账号发生变化时，必须同步更新根目录 `README.md`（中英双语），保持项目介绍与代码一致。
3. **不要主动 git commit / push**，除非用户明确要求。
4. **注释同步**：改业务逻辑必须同步更新类头/步骤注释（`CODING_STANDARD.md` 第 5 节）。
5. **注释完备性（强制）**：**每个接口、每个方法（含私有）、每个字段/DTO 属性都必须写 XML 注释**，标准见 `CODING_STANDARD.md` 5.0 节清单；新增服务与新增文件一律执行，缺注释视为未完成。
6. 工具调用保持小步执行，避免输出过大导致中断。

## 4. 代码现状（2026-09-18）

- **服务与分层**：14 个后端服务全部按 DDD 四层落地（Api/Application/Domain/Infrastructure；控制器纯转发、业务在 Handler、验证在 Validator、查询在仓储）。新增 CustomerService（客户域）与 FileService（统一上传）。
- **账号域分离**：前台客户在 CustomerService（客户 JWT + Redis 会话，支持滑动续期/刷新/登出，网关校验会话键）；后台账号在 UserService + AuthService OpenIddict（password flow、RS256 自签证书）；权限只认 `user_role` 显式绑定（fail-closed），无 legacy 兜底。
- **营销与到手价**：DiscountEngine（活动/券互斥 + 平台优先级 + 到手价取更低价）；`MarketingSnapshotCache` 平台快照缓存（写操作失效 + 30s TTL）；`FinalPrice` 批量试算供列表/详情展示。
- **统一上传**：所有端走 FileService `/gateway/files/Upload`（本地/阿里云/腾讯云/Azure 可切），商品主图与装修上传框为固定尺寸 + 悬浮玻璃层。
- **小程序**：按凯德星参考图重做（首页/商城/店铺/详情/我的），平台由发布配置锁定；后台装修为可视化拖拽 + 手机预览（轮播/图标资源在后台域通过 `assetUrl()` 解析）。
- **注释完备性**：`src/` 全量公开/私有成员 XML 注释已补齐（扫描 0 缺失），新增代码按 `CODING_STANDARD.md` 5.0 强制标准执行。
- **已知风险**（详见 REVIEW.md 风险节）：下单孤儿预留无 TTL、Inventory 消费者无 DLQ、`order.created/order.cancelled` 无消费者、本地多表写入无事务、上传整文件读入内存（大视频有内存压力）、云存储未用真实凭据联调、测试临时商品会污染列表。

## 5. 进度日志（倒序，新条目写在最上面）

### 2026-09-18（第二十四轮）：统一文件服务（多存储）+ 上传交互统一
- 新增 **FileService**（5080/5081，`simpleshopfile`，表 `stored_file`）：所有端统一上传入口 `POST /gateway/files/Upload`，返回绝对地址；本地存储回源 `GET /gateway/files/Content/{对象键}`。
- **多存储可切换**（AgileConfig `FileStorage:Provider`）：`Local`（默认）/`AliyunOss`/`TencentCos`/`AzureBlob`；凭据/自定义域名在 `FileStorage:{Provider}:*`；格式白名单 `AllowedExtensions` 与分类大小上限 `MaxSizeBytes`（image 5MB/document 20MB/audio 20MB/video 200MB/default 10MB，字节）均可配置；校验顺序：扩展名 → 大小 → 文件头魔数。已接入官方 SDK（Aliyun.OSS.SDK.NetCore/Tencent.QCloud.Cos.Sdk/Azure.Storage.Blobs）。
- **前端统一上传交互**：商品编辑主图与装修图片/图标改为**固定尺寸上传框 + 悬浮玻璃层**（点击上传/更换，不随图片尺寸变化、不再显示链接输入与独立上传按钮）；小程序头像上传切到 `/files/Upload`；移除 ProductService 的旧上传端点（历史图片读取保留兼容）。
- 网关新增 `/gateway/files/{everything}` 路由；`run-dev-services.sh`、解决方案、健康检查（14 服务）同步；`api-regression` 新增 FILE-01~06 并把 IMG 用例切到新入口（114/114），`ui-regression` 46/46。
- 验证：上传 PNG → 落库 Provider=Local → 网关回源 200；伪 PNG/非法扩展名/超限文档均 400；后台两处上传框与悬浮层 CDP 实测通过。

### 2026-09-18（第二十三轮）：后台装修轮播图/图标预览修复
- **根因**：装修页在后台域（5173）下无法加载 `/static/...`（小程序静态资源）与 `/gateway/...`（网关相对路径），导致头图、轮播缩略图、属性面板图标预览全部空白；且顶部 `home.banners` 只有渲染没有编辑入口。
- **修复**：新增 `assetUrl()`（`/static/` → 小程序 H5 基址 `VITE_USER_H5_BASE`，默认 `http://127.0.0.1:5174`；`/gateway/` → 网关基址），统一用于头图、图片/图标输入组件预览、轮播缩略图；新增「首页轮播图（头图）」编辑块（增删/上下移/缩略图/点击选中）+ 右侧「轮播图设置」（图片上传/标题/跳转），点击预览头图也可直接选中；移除组件库中重复的「轮播图」模块入口。
- 验证（CDP 实测）：轮播图 3 条可编辑、头图 `naturalWidth=1125` 已加载、权益图标预览 `naturalWidth=96`、轮播属性面板正常；`ui-regression` 46/46。

### 2026-09-18（第二十二轮）：小程序图片与质感修复（1:1 还原补强）
- **商品图不显示根因**：商品列表按创建时间倒序，测试脚本创建的占位图商品（`API回归商品/API隔离商品/链路商品`，共 74 个）排在演示商品之前；已软删这批测试商品（SKU 同步），演示商品（dummyjson 真实图）恢复展示。
- **营销 banner**：用 PIL + Noto Serif CJK 生成 3 张 1125×840 设计稿（渐变 + 商品图白卡 + 标题/副标题/胶囊标签）到 `apps/user-uniapp/src/static/banners/`；后端 `DefaultDesign` 与管理端 `emptyDesign` 默认带上这 3 张，DEMOPL 平台装修同步发布（首页头图不再是纯色渐变）。
- **质感补强**：重新生成 `placeholder.png`（浅灰渐变 + 图片图标）；用 PIL 把黑色线性图标着色为蓝/紫两套 `static/line-color/*.png`（14 个），会员权益行改用彩色图标（参考图配色）；商城团购卡「自营」标签与商品名同行排版。
- 验证：5 页截图复核（首页头图/商城团购/店铺/详情/我的权益均正常显示图片与彩色图标）；`ui-regression` 46/46、`api-regression` 108/108；H5/微信小程序/后台构建通过。

### 2026-09-18（第二十一轮）：小程序按凯德星参考图整体重做（像素级还原）
- **我的页**（参考图 2）：渐变会员卡（圆点纹理 + 升级周期 + 进度条/¥目标 + 会员权益中心白胶囊）、居中权益文案 + 4 个白色圆形权益、我的账户三栏、我的服务 5 列线性图标宫格；头像/昵称/掩码手机号头部 + 客服/刷新圆钮。
- **店铺页**（参考图 1）：顶部搜索、店铺头（logo + 名称 + 五星评分 + 全部商品数）、商品/活动下划线 Tab、分类 chips、优惠专区大图活动卡（时间 + 进行中 + 进度条）、销量/价格排序、两列商品网格 + 右下悬浮购物车/分享。
- **商品详情**（参考图 3/4）：红色大字价格 + 分享方钮 + 到手价/已优惠胶囊 + 库存、配送灰底行、规格/数量、商品评价、商品详情、悬浮首页、底部橙黄/红渐变胶囊双按钮（店铺/购物车角标）。
- **首页**（参考图 5）：铺满头图（轮播或主题渐变）+ 定位胶囊 + ⭐评分/消息圆钮 + 轮播 1/N 指示 + 悬浮搜索、白卡四宫格金刚区、Hi 问候卡（等级 chip + 三栏账户）、优惠专区横滑卡、装修模块（分类/商品两列/三列/横滑）。
- **商城页**（参考图 6，原分类页）：商场特惠头 + 搜索 + 分类 chips、商场团购横滑卡（自营角标）、为您推荐三列网格、商场活动大图卡（活动时间）。
- **主题**：默认主色/标签色改为凯德星青绿 `#00c1a2`、背景 `#f5f5f5`；底部 Tab 文案「分类」改「商城」；后台 `DefaultDesign` 与装修编辑器默认值（主题、我的服务线性图标、快捷入口四项、标签文案）同步更新，编辑器手机预览同步新样式。
- 验证：`ui-regression` 按新 UI 调整选择器/断言后 46/46 通过；`api-regression` 108/108；H5 与微信小程序构建通过；5 个页面截图核对与参考图结构一致。

### 2026-09-18（第二十轮）：小程序装修改为可视化拖拽编辑器
- 后台「小程序装修」页重做（`apps/admin-vue/src/views/AppDesign.vue`）：左侧组件库（品牌头/轮播图/快捷入口/分类导航/商品推荐/公告，HTML5 拖拽或点击添加）+ 中间**手机预览**（按小程序真实渲染示意：头图/悬浮搜索/快捷宫格/会员卡/公告/优惠专区/可排序模块/底部 TabBar）+ 右侧属性面板（选中模块/权益/服务即编辑，含图片与图标上传、分类/商户下拉、跳转选择器）。
- **指针拖拽（非 HTML5 DnD）**：调色板拖入预览显示跟随幽灵 + 虚线插入占位；预览内拖动模块排序；拖出手机（红色虚线 + 「松手删除」提示）即移除；「我的页装修」权益/服务同样支持拖拽排序与拖出删除。已移除高级 JSON 编辑器（避免抽象配置）。
- 预览使用真实数据：分类树与商户下拉、商品模块按配置拉取真实商品（带缓存），图标路径映射为相近 emoji 保证可读；保存/发布仍走 `/platform-configs/Save`，校验规则与后端/小程序一致。
- 测试：`ui-regression` 新增 ADM-07b~g（手机预览框架、点击添加、属性面板、真实鼠标拖入占位、拖出删除、拖动排序），全套 46/46 通过；测试需桌面视口（1680×1050）与手机预览滚动配合。

### 2026-09-18（第十九轮）：清除 legacy 账号体系与死代码
- **权限只认显式绑定**：删除 `PermissionGrpcService.ResolveAsync` 的 legacy admin 兜底（无 `user_role` 绑定一律按 customer，fail-closed）；`AuthorizationRequest.LegacyRole` 字段、`PermissionCenterClient` 传参同步移除；codexadmin 改为显式绑定 `platform-admin`（PlatformId=0 全局超管，`AssignRoleAsync` 允许 platform-admin 全局绑定）。
- **User 表去角色化**：移除 `User.Role`/`OperationId`/`IsCancel`/`IsAllAgreeAgreement` 属性与全部读写（建号/改号/列表过滤/UserShaper/前端回退），并 DROP 对应数据库列；后台权限完全由权限中心绑定决定。
- **死代码清理**：删除 `ICustomerService`/`GetCustomerResponse`/`GetCustomerRequest`（旧客户 gRPC 契约）、`CusromerSourceCode`（拼写错误的遗留枚举）、`CustomerService.Domain.Enums.ApiResponseCode`（重复枚举）、AuthService `AccountController`（硬编码 testuser 演示端点）与 Cookie 认证配置（只保留 OpenIddict password 令牌端点）、`UserApplication.AppCategory` 列（OpenIddict `ClientType` 不再被遮蔽）、网关历史 issuer `SimpleShop.UserService`。
- **环境**：Consul 重复注册实例已注销（Product/User 各一个旧实例）；`api-regression` 108/108、`full-chain` 104/104、`marketing-flow` 26/26、`business-flow`、`ui-regression` 40/40 全通过。

### 2026-09-17（第十七轮）：账号域分离（前台 CustomerService / 后台 UserService + AuthService OpenIddict）
- **架构重构**：前台客户业务（注册/登录/资料/地址/收藏）从 UserService 迁到 **CustomerService**（5280/5001，`simpleshopcustomer`，表 `customer`/`customer_address`/`customer_favorite`，客户 JWT issuer=`SimpleShop.CustomerService`）；UserService 只保留后台账号（列表/建号/改号/启停/资料 + gRPC 登录校验），后台建号禁止 `customer` 角色；`/users/Register|Login|Addresses|SaveAddress|DeleteAddress|Favorites|ToggleFavorite` 全部移除。
- **后台登录切 OpenIddict**：AuthService 令牌端点 `/gateway/auth/Token`（password flow、公开客户端 admin-app、`DisableTransportSecurityRequirement`+`AcceptAnonymousClients`）；`UserGrpcService.LoginAsync` 返回租户/权限，AuthService 组装 claims（`sub/tenant_type/platform_id/merchant_id/permission/role`，`SetDestinations(AccessToken)`）后 `SignIn`，以 **RS256 自签证书**（`LocalSigningCertificate` 共享路径，默认 `~/.local/share/SimpleShop/auth-signing.pfx`，gitignore）签发 12h 令牌；客户账号从后台登录被拒（invalid_grant）。
- **网关双令牌验签**：issuer=`https://simpleshop.local/auth` → RS256 证书验签；客户/历史令牌 → HS256 共享密钥（issuer 白名单）；claims 映射与 `X-Claim-*` 注入逻辑不变。
- **修复 OpenIddict 接入陷阱**：自定义 `UserApplication.ClientType` 遮蔽 OpenIddict 自带 public/confidential（改名 `AppCategory`，DB 迁移 rename/add）；自定义声明需 `RegisterClaims` + `SetDestinations`；强制 `sub`；`SetIssuer` 必须绝对 URI。
- **数据迁移**：`scripts/migrate-customers.sh` 把 186 个客户 + 地址/收藏复制到客户库（保留雪花 Id，排除后台绑定账号），`--purge` 清理 User 表；迁移后 User 表仅 10 个后台账号。
- **前端**：小程序注册/登录/地址/收藏切 `/customers/*`；后台登录改 `/auth/Token`（表单）并从 JWT 解析用户/权限，request 拦截器透传 OAuth 响应；后台用户管理角色选项去掉「商城客户」。
- **测试**：api-regression 101/101（新增后台 OpenIddict 登录/客户登录/跨域拒绝/资料路径）、full-chain 101/101（链路 1 落库断言改 customer 库）、marketing-flow 26/26、business-flow、ui-regression 40/40（新增 ADM-00 后台登录表单真实走 OpenIddict）；H5/微信小程序/后台构建通过。
- 文档：README（服务表 13 服务 + 账号域）、BUSINESS（账号域分离与数据模型）、REVIEW（链路 1/2/3 重写）、CODING_STANDARD（陷阱 12/13）、TEST_CASES（AUTH/USER/CHAIN/报错矩阵）同步。

### 2026-09-17（第十六轮）：优惠/到手价性能优化 + 修复字段级更新静默失效
- 新增 `MarketingSnapshotCache`（Application/Services，单例 + IMemoryCache）：按平台缓存"配置 + 全部启用活动 + 活动范围"，保存活动/启停/保存配置时显式 `Invalidate(platformId)`，另有 30s TTL 兜底；快照不缓存时间窗口，使用时按 `now` 过滤 StartAt/EndAt，活动按时开始/结束不受影响。`DiscountEngine` 与 `ActiveActivities` 改用快照；实测到手价/结算预览 4-6ms → **~1ms**（游客命中缓存时零查询），下单 18ms 不变。
- 首页 `loadModules` 改为商品模块并行加载 + 全部到齐后**合并为一次 FinalPrice 批量请求**（原按模块串行，4 模块=4 次营销上下文构建）。
- **修复真实缺陷（P1 级，全服务共享）**：`BaseRepository.UpdateColumnsAsync` 使用 `UpdateColumns(a => obj)` 对捕获匿名对象生成不了 SET，**静默不更新**——活动启停、券模板启停全部未落库（接口仍返回成功）；改为 `SetDto(obj)`，券活动专用更新同步修复。陷阱已写入 `CODING_STANDARD.md` 第 6 节。
- 测试：`full-chain.sh` 101/101（新增：停用/启用后数据库 `IsEnabled` 落库断言 + 到手价随启停变化，验证缓存失效与字段更新）；`api-regression.sh` 98/98、`marketing-flow.sh` 26/26、`business-flow.sh`、`ui-regression.js` 39/39 全通过；文档（BUSINESS/REVIEW/CODING_STANDARD/TEST_CASES/README）同步。

### 2026-09-17（第十五轮）：京东/淘宝式「到手价」
- 后端新增 `POST /gateway/marketing/FinalPrice`（**游客可调用**，网关白名单放行）：逐商品按"单商品订单"用优惠引擎试算，**同时按活动优先/券优先各算一次取更低价**（用户可在结算页取消勾选券，取最低可到手价才能不负于展示），返回 `originalPrice/activityDiscount/couponDiscount/finalPrice/hitType/activityName/couponName`；批量 1-50 行，Validator 报错空清单/价格≤0/数量越界/超 50。
- 引擎新增 `DiscountEngine.FinalPriceAsync`（`MarketingFinalPriceResult`）：复用同一套活动/券/互斥规则，保证"展示到手价"与结算口径同源；列表页整页商品合并为一次批量请求。
- 小程序新增 `common/final-price.js`（`enrichFinalPrices/priceInfo/finalPriceOf/formatMoney`），首页、分类页、店铺页、收藏页商品卡与商品详情统一改为京东/淘宝式展示：**到手价标签 + 大字到手价 + 划线原价 + 优惠来源（券减¥x/活动减¥x）**；游客看活动价，登录后自动计入最优券；排序改为按到手价；接口失败静默回退原价。
- 测试：`full-chain.sh` 97/97（链路 2 断言登录 90=活动更低价、游客 90、报错矩阵；链路 4b 断言到手价=下单实付）；`ui-regression.js` 39/39（详情页到手价=营销引擎、划线原价=数据库 SKU 价、到手价标签与已优惠说明、分类页列表到手价=引擎且标签/划线正确）；`api-regression.sh` 98/98、`marketing-flow.sh` 26/26、`business-flow.sh` 通过；H5 与微信小程序构建通过。
- 文档：`BUSINESS.md`（展示面与营销章节）、`TEST_CASES.md`（CAL-14..16、MP-DB-05、CHAIN 到手价断言）、`README.md`（功能与测试口径）同步。

### 2026-09-17（第十四轮）：业务链路级测试 + 取消订单释放库存修复
- 新增 `tests/e2e/full-chain.sh`（88 项，全过）：6 条业务链路（注册→登录→地址→加购 / 领券→结算→下单锁库存→支付扣减 / 退款→回补→报表 / 纯活动订单→参与记录 / 取消→券回退+库存释放 / 满赠赠券）+ 16 条精确报错矩阵；每条链路独立租户与 SKU，接口响应与 PostgreSQL 落库双向断言（金额/状态/库存三列/流水/券状态逐字段比对）。
- **修复 P0 缺陷（REVIEW 风险 2）**：客户取消订单不释放库存。`Order` 新增 `StockLocked`（下单锁库存成功后置位）；`CancelOrderHandler` 取消后按明细汇总 `ReleaseStockAsync`，失败写 `pending_stock_release`（OrderService 侧新增同名实体共用该表）并记 Error 日志；未锁过库存的订单不释放，避免虚增可用。
- 库存测试口径统一为三列：`AvailableQuantity`（总量不变）/ `LockedQuantity` / `DeductedQuantity`，可用 = Available-Locked-Deducted（INV/ORD 用例与文档已同步）。
- `ui-regression.js` 扩到 35 项：新增 UI 真实下单（点「提交并支付」→跳订单列表），断言页面实付金额=数据库 `PaymentPrice`、状态 20、列表显示数据库订单号/金额、商品价格与 `sku.Price` 一致、购物车数量与 `cart_items` 一致、我的页三项统计与数据库 count 一致、后台订单详情=数据库订单；修复测试路由漏写 `/#` 导致的空白页问题。
- `TEST_CASES.md` 新增第 21 节「业务链路端到端（CHAIN）」：链路步骤预测值 + 精确报错矩阵（HTTP 码/errors 字段/中文消息）+ 执行结果更新（api 98 / ui 35 / marketing 26 / full-chain 88，business-flow 通过）。

### 2026-09-17（第十一轮）：商城对标参考图 + 平台装修配置化
- 参考凯德星小程序：新增/重做 **首页会员问候卡 + 优惠专区（进行中活动/可领券）**、**我的页会员账户统计 + 配置化服务宫格**、**商品详情图集/分享/配送/评价占位/吸底（店铺+购物车角标+加购+立购）**、**店铺页**（公开店铺信息+商品+本店活动）、**收藏页**、分类页价格排序。
- 后端新增：`GET /gateway/marketing/ActiveActivities`（活动专区，游客可见，支持 merchantId 过滤）、`GET /gateway/merchants/Shop`（店铺公开信息，仅展示字段），网关显式放行；`DefaultDesign` 补金刚区默认项与 `profile.services`（我的服务）默认配置。
- 装修配置化（后台 AppDesign）：新增「首页轮播图」（写 `home.banners`，小程序首页顶部大图优先读取）与「我的服务」编辑器（icon/名称/跳转类型/参数，含校验）；快捷入口/轮播/我的服务共用一套跳转类型。
- 验证：`/tmp/opencode/verify-parity.js` 5 用例（商品详情齐全+加购、购物车 +1、店铺页、我的页、首页会员卡/优惠专区/金刚区）通过；`tests/e2e/marketing-flow.sh` 26 项与 `business-flow.sh` 回归通过；H5 与微信小程序构建通过。
- 说明：积分体系、会员等级、商品评价、团购等参考图能力需先定义后端业务模型，当前以占位/替代实现（评价显示 0、会员显示"普通会员"）。

### 2026-09-17（第十三轮）：测试体系（用例文档 + API/UI 自动化回归）
- 新增 `tests/TEST_CASES.md`：按 20 个功能模块编写 150+ 条用例（编号/优先级/前置/步骤/期望/自动化对照），含资金与库存副作用的校验要求。
- 新增 `tests/e2e/api-regression.sh`（98 项，全过）：健康检查与 Consul、认证/注册校验、网关鉴权（401/403/畸形与过期 token/伪造 X-Claim）、多租户隔离（商户改他商商品 404、读他商订单无权）、分类层级、商品与 SKU 校验、上传魔数、购物车增量、订单幂等/校验/取消、支付幂等、退款限额与审批/拒绝/重复审批、库存空明细、活动/券 CRUD 与限领/库存、优惠引擎（券优先/活动优先/0元减不命中/单行封顶 0.01/游客仅活动）、报表、装修配置、装修 JSON 校验。
- 新增 `tests/e2e/ui-regression.js`（25 项，全过）：小程序首页结构/会员问候/优惠专区、商品详情元素与图集、购物车 +1 与图片、结算金额明细与券勾选浮动、券包无重复¥、收藏页、店铺页头与排序、我的页会员卡/权益/服务宫格、token 失效跳登录；后台营销管理 Tab、装修页编辑器、401 跳登录、控制台无异常。
- **修复真实缺陷**：`/orders/Cancel` 控制器用 `Ok()` 包裹 Handler 的 ApiResponse，产生双层信封（前端读不到 success/code）；改为一行转发，并在 `CODING_STANDARD.md` 明令禁止该写法。
- 回归：`marketing-flow.sh` 26/26、`business-flow.sh` 主链路通过。

### 2026-09-17（第十二轮）：按参考图重做小程序（会员中心/店铺页/商品详情/首页）
- 参考凯德星小程序：铺满式首页头图 + 悬浮搜索/平台切换、金刚区白卡、会员问候卡、优惠专区；我的页改为会员中心（渐变会员卡+升级进度+权益行+我的账户+5 列线性图标服务宫格）；商品详情（图集指示点、活动标签、配送、评价占位、橙黄/红渐变拼接口袋按钮、悬浮首页）；店铺页（渐变头+商品/活动双 Tab+分类 chips+优惠专区+综合/价格排序+悬浮购物车/分享）；分类页价格排序。
- 新增线性图标集 `src/static/line/*.png`（`scripts/gen-line-icons.py` 生成，18 个）；装修默认配置改用图标路径，后台「小程序装修」新增首页轮播图与我的服务/权益编辑器（icon 支持路径或 emoji），商品模块新增三列布局。
- 会员等级与升级进度为**前端展示**（按订单实付累计计算，未建后端成长值模型）；评价/积分/团购等仍为占位。

### 2026-09-16（第十轮）：MarketingService 注释完备性
- 按用户要求把"**每个接口、每个方法（含私有）、每个字段/DTO 属性都必须写 XML 注释**"升级为硬性规范：`CODING_STANDARD.md` 新增 5.0 节检查清单（覆盖类型/接口方法/类方法/字段/枚举成员/控制器动作/Validator/仓储/MQ 消费者），`AI_HANDOFF.md` 协作约定新增第 5 条。
- 逐文件补齐 MarketingService 与共享契约注释：实体 11 张表全字段、`IMarketingRepository` 全部方法与参数、`MarketingMessages`（MessagePack DTO）全字段、`IMarketingService` 全方法、19 个后台/用户端 Handler 方法与类头、13 个 Validator（类头+构造器规则说明）、报表查询与校验、优惠引擎（含私有方法、CalculationContext 字段）、落账服务、gRPC 服务、MQ 消费者（队列/幂等/失败策略）、Api 控制器 19 个动作、Program/DI；仓储实现统一 `/// <inheritdoc />`。
- 审计脚本核对：MarketingService + 契约文件 **缺注释 public 条目 0、缺注释枚举成员 0**（注释行 788/3626 ≈ 21%），构建通过，服务重启 `/health` 200，营销 E2E 回归通过。

### 2026-09-16（第九轮）：参考设计案例的商城 UI 重做
- 参考来源（联网检索）：Apple HIG / WWDC Design（内容优先、玻璃质感、8pt 间距、44pt 触达、SF 字阶）、《小米商城高端化改版》（克制配色、回归产品、统一图片规格与间距）、Dribbble/Figma 电商模板（搜索优先导航、圆角促销卡、价格突出的商品卡、吸底 CTA）。
- 首页：无 banner 时用平台主题渐变 Hero 卡（品牌名+卖点+入口）；区块标题加副标题与「更多 ›」；金刚区 iOS 图标底座；分类胶囊；商品卡改 1:1 白卡 + 边框 + 阴影 + 价格行「＋」快捷点；加载改骨架屏；过滤空模块。
- 分类页：顶部搜索优先结构；左侧圆角高亮导航并新增「全部」入口（默认不再落空分类）；右侧商品卡与「＋」。
- 商品详情：价格+划线原价+库存药丸；新增服务保障行（正品/极速发货/7 天无理由）；规格卡与数量卡分区；底部吸底「购物车入口 + 加入购物车 + 立即购买」。
- 我的：平台主题渐变会员卡头部（首字母头像 + 会员徽标）。
- 登录/注册：环境光斑背景 + 卡片化输入。
- 主题：默认 Apple 蓝（上轮已改）；`applyTheme` 同步底部 TabBar 选中色，换肤全局一致。
- 验证：H5 / 微信小程序构建通过；截图逐页复核；购物车、用券切换、营销 UI 三套回归全过。

### 2026-09-16（第八轮）：商城端 Apple 风视觉优化
- 默认主题：服务端装修默认色从高饱和随机色改为 Apple 蓝 `#0071e3`（life/mall 用低饱和青/靛），页面背景统一 `#f5f5f7`；避免未装修平台出现粉色/杂色。
- 全局：`App.vue` 增加设计变量（底色/字号层级/分隔线/圆角/阴影）与通用卡片/空状态样式；H5 `uni-page-body` 背景贯通。
- 首页：过滤空模块（不再出现只有标题的空卡片）、商品改白卡+阴影+浅灰图底、分类胶囊/公告/轮播/头部间距统一。
- 商品详情：价格与库存分行（库存改药丸）、选中规格改主题色浅底描边、页面底部预留操作条空间。
- 购物车：空状态插画文案、卡片/步进器/删除打磨、底部条毛玻璃、去掉贴底遮挡。
- 提交页：勾选与提交按钮改用平台主题色，新增金额明细已在上轮完成；底部留白。
- 领券中心/券包：修复金额重复 `¥¥`（满折不在金额前缀），票券卡片与领取按钮打磨（主题色+阴影）。
- 我的：领券中心换礼盒图标（新增 `cell-gift.png` 生成逻辑）。
- 订单：状态改为浅底色药丸；地址页新增空状态与整屏底色、表单输入框改浅灰圆角。
- 验证：H5 与微信小程序构建通过；真实页面截图逐页核对；`verify-cart-fix.js`、`verify-toggle-coupon.js`、`verify-marketing-ui.js` 全量回归通过。

### 2026-09-16（第七轮）：购物车与领券中心修复
- 购物车加号倍数增长：`/carts/Add` 是累加语义，前端却传"目标数量"（1→2 时后端变 3）。修复 `cart.vue` 改传增量 ±1 并加防连点；后端补累计上限 99（`AddCartItemHandler`）。
- 购物车/提交页图片不展示：CartItem 无图片字段（旧数据只能落到不可用的外链占位）。修复：`CartItem.Image` 列 + 命令/校验/Handler 图片快照刷新；商品详情加购带图；购物车/结算页展示 `image`；外链占位图统一换本地 `/static/placeholder.png`（含首页/分类/商品详情）。
- 优惠展示不明显：购物车优惠条改为"已优惠 -¥X + 活动/券标签"；提交页新增「金额明细」卡片（商品金额/优惠合计/实付金额），优惠行金额加粗放大。
- 领券中心随机排序：查询无 ORDER BY 时 PostgreSQL 返回堆顺序。修复券活动与计算引擎活动查询排序（券中心 `CreatedAt DESC`，引擎 `CreatedAt,Id ASC`）；实测连续 3 次返回顺序一致。
- 验证：`verify-cart-fix.js`（1→2→3、图片、金额明细）、`verify-toggle-coupon.js`（勾选 295 → 取消 290）、`verify-marketing-ui.js` 4 用例均通过。

### 2026-09-16（第六轮）：演示测试数据填充
- 新增 `scripts/seed-test-data.js`：从 dummyjson 公开测试数据 API 拉商品（失败回退内置 8 条），建演示平台「演示商城」（编码 `demo`）、两个已审核商户、商户管理员 `demo-merchant/Demo123456`、三级分类、商品与双 SKU 并自动上架、券模板/券活动/平台与商户活动、8 个演示用户领券、下单/支付/发货/签收/退款；可重复执行（平台/商户/分类/活动按名称复用，商品按 `DEMO-` SKU 去重，订单追加）。
- 当前数据量：商品 79、用户 76、订单 86、支付单 68、退款单 14；活动参与 16 单/折扣 285 元，用券 8 笔/抵扣 85 元。
- 验证：商城首页（演示平台）渲染 10 张演示商品图、分类页展示演示分类；README 已补「演示数据」章节。

### 2026-09-16（第五轮）：小程序登录失效不跳登录修复
- 现象：商城（H5/小程序）token 失效后接口提示"请先登录"但不跳登录页。
- 修复：`apps/user-uniapp/src/common/request.js` 统一识别 `HTTP 401` 与 `body.code=401`（部分服务是 HTTP 200 + code=401）→ 清 token/user → toast「登录已过期，请重新登录」→ 跳转 `/pages/auth/login`（3 秒去重防并发重复跳转，登录页内不跳，navigateTo 失败回退 reLaunch）。
- 验证：`/tmp/opencode/verify-uni-401.js` 三用例通过（网关 401 券包、业务 401 购物车、重新登录成功）；营销 UI 4 用例回归通过。

### 2026-09-16（第四轮）：营销页面数字字符串回填修复
- 现象：营销管理编辑弹窗的归属/活动类型/参与范围不回显，列表"门槛/优惠""范围"列展示错误（满折显示成满赠、券活动显示 `0元减undefined`）。
- 原因：后端数字统一按字符串下发，页面用 `===`/el-radio 严格比较。修复 `Marketing.vue`：枚举/金额/数量 `Number()` 归一，**ID 保持字符串**（禁止 Number 雪花 ID）；新增 `couponActivityBenefit` 用模板快照字段；模板/活动/券活动回填与提交统一口径；`Users.vue` 角色 `tenantType` 比较同步修复；uniapp 领券中心/券包 `couponType/status/source` 比较修复（未使用券不再全部置灰、满赠来源正确）。
- 验证：`verify-marketing-edit.js`（编辑回填+列表展示）、`verify-marketing-misc.js`（满折展示/券包置灰/用户角色字段）、`verify-marketing-ui.js` 4 用例全部通过。

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
