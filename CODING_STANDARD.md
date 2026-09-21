# SimpleShop 后端编码规范（DDD 分层 + 注释规范）

> 生效：2026-08-31。基准模板：`src/CustomerService/`。适用所有 `src/` 微服务。
> 配套文档：`BUSINESS.md`（业务）、`AI_HANDOFF.md`（AI 协作）、`REVIEW.md`（链路与风险）。

## 1. 四层职责

| 层 | 项目 | 职责 | 禁止 |
|----|------|------|------|
| Api | `*.Api` | 协议转换：HTTP 绑定 → `mediator.Send()` → 返回；文件流（IFormFile）等协议职责 | 业务逻辑、字段验证、IFreeSql 查询、事件发布 |
| Application | `*.Application` | `Features/{实体}/{动作}/` 三件套；编排仓储与外部服务；gRPC 服务实现 | 直接依赖 IFreeSql |
| Domain | `*.Domain` | 实体（继承 `BaseEntity`）、仓储接口、枚举 | 框架业务依赖 |
| Infrastructure | `*.Infrastructure` | 仓储实现（`BaseRepository<T>` + IFreeSql）、DI 注册 | 业务规则 |

## 2. 控制器（Api 层）

```csharp
// ✅ 标准写法：一行转发。无验证、无查询、无业务分支。
[HttpPost]
public Task<ApiResponse> Create([FromBody] CreateUserCommand command)
    => mediator.Send(command, CancellationToken.None);

// ✅ 例外 1：把网关注入的租户上下文绑定为命令参数（协议适配，不是业务）
command = command with { CustomerId = tenant.UserId, OverrideOwnerCheck = !tenant.IsCustomer };

// ✅ 例外 2：Handler 是旧式 IRequest<object> 时用 Ok() 包一层
return Ok(await mediator.Send(query, CancellationToken.None));
// ⛔ 禁止：Handler 返回类型已是 ApiResponse 时再包 Ok() → 产生双层信封 {data:{code,message,data}}，
//    前端 request.js 解包后读不到 success/code（历史缺陷：/orders/Cancel）。
```

- 私有 helper（校验函数、查询方法）**不允许**留在控制器。
- 保留在 Api 层的合理例外：文件上传（IFormFile 流处理）、AuthService（EF Core + OpenIddict 另一套模式）、纯查询转发接口。

## 3. Feature 三件套（Application 层）

每个动作一个文件夹：`Features/{实体}/{动作}/Command + Handler + Validator`。

### 3.1 Command

```csharp
public record CreateUserCommand(string UserName, ..., long PlatformId = 0) : IRequest<ApiResponse>;
```

新代码统一 `IRequest<ApiResponse>`（Handler 自构造完整响应，含失败语义）。存量 `IRequest<object>` 允许存在，新动作不要再用。

### 3.2 Handler

```csharp
public class CreateUserCommandHandler(IUserRepository repository, PermissionCenterClient permissionCenter)
    : IRequestHandler<CreateUserCommand, ApiResponse>
{
    public async Task<ApiResponse> Handle(CreateUserCommand request, CancellationToken ct)
    {
        if (await repository.ExistsAsync(request.UserName, request.Phone))
            return ApiResults.Fail(BaseApiResponseCode.BadRequest, "用户名或手机号已存在");
        ...
        return ApiResults.Ok(UserShaper.Shape(user));
    }
}
```

- 成功/失败统一 `ApiResults.Ok(data)` / `ApiResults.Fail(BaseApiResponseCode.X, "消息")`（`CommunalService.Domain/Common/ApiResults.cs`），保证前端收到的 `code/message/errors` 一致。
- 租户过滤：注入 `TenantContext`（读网关注入的 `X-Claim-*`），Handler 内做 `IsPlatform/IsMerchant/IsCustomer` 裁剪。
- 跨服务调用封装为 `Application/Services/*Client`（如 `PermissionCenterClient`），Handler 不直接写 gRPC 客户端代码。
- 分布式锁/幂等/事件发布的模式参考 `CreateOrderHandler`、`ConfirmPaymentHandler`（链路细节见 `REVIEW.md`）。

### 3.3 Validator（模型验证必须写在这里）

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().Length(3, 64).WithMessage("用户名必须为3-64个字符");
        RuleFor(x => x.Password).MinimumLength(8)
            .Must(p => p.Any(char.IsDigit) && p.Any(char.IsLetter)).WithMessage("密码至少8位且包含字母和数字");
    }
}
```

- `ValidationBehavior`（MediatR 管道）自动执行；失败抛 `ValidationException`，全局中间件统一转 `400 + { code, message, errors: { 字段: [消息] } }`——前端 `request.js` 依赖该结构做字段级提示。
- 正则/长度/必填/范围写 Validator；需查库的校验（唯一性、存在性）写 Handler。
- 服务必须注册管道：`builder.AddMediatRWithHandlers(typeof(Xxx.Application.*).Assembly, typeof(ValidationBehavior<,>).Assembly);`
- **覆盖要求**：每个写入口（Create/Update/Delete/审批/状态变更）与列表查询都必须有对应 Validator；列表分页统一 `Page>=1`、`PageSize 1-100`。新增接口时 Validator 与 Command 同目录同批提交。
- **字段规则口径**（前后端一致）：手机号 `^1[3-9]\d{9}$`；邮箱 `^[^\s@]+@[^\s@]+\.[^\s@]{2,}$`（或 `EmailAddress()`）；金额 `>0` 且最多两位小数；数量 `1-99`（交易链路）或 `1-10000`（库存明细）；字符串长度对齐实体 `[Column(StringLength = N)]`；原价 `0` 或 `>= 售价`。
- **前端同步**：admin-vue 用 `src/utils/validators.js`、user-uniapp 用 `src/common/validators.js`（正则与规则工厂集中维护，禁止在页面里重复写正则）；提交前必须 `trim` 字符串字段，纯空格不得通过必填。
- **两种 400 形态**：Validator 失败是 HTTP 400 + `errors`；控制器内联 `Error(BaseApiResponseCode.BadRequest, ...)` 是 HTTP 200 + `body.code=400`（用于无字段归属的守卫，如"缺平台/缺审核结论"）。

## 4. 仓储模式

- 接口 `Domain/IRepository/`，实现 `Infrastructure/Repository/`（注入 IFreeSql）。
- 简单 CRUD 继承 `IBaseRepository<T>` / `BaseRepository<T>`；分页组合查询定义领域方法（如 `QueryPagedAsync`），实现用 `WhereIf/Count/Page`，**返回 `(List<T> Items, long Total)`**，total 与取数同条件。
- DI 注册：仓储 → `Infrastructure.AddInfrastructure(builder)`；应用服务 → `Application.AddApplication(builder)`（Program 必须 `Build()` 前调用）。
- ⚠️ 新增仓储忘记注册 DI → 服务启动即崩（编译期发现不了）。

## 5. 注释规范（强制）

注释的目标读者是**下一个改这段代码的人**：他需要知道"这段代码为什么存在、在整条链路的什么位置、有哪些不能踩的约束"。禁止写"这行做了什么"的废话注释。

### 5.0 注释完备性（强制检查项）

**接口、方法、字段三类必须 100% 有 XML 注释，缺一不可**；新增服务（如 MarketingService）与新增代码一律按此标准执行，评审时按清单逐项核对：

| 对象 | 必须写 | 注释要点 |
|------|--------|----------|
| 接口/类/枚举 | `/// <summary>` | 职责 + 在链路中的位置 + 关键约束 |
| **接口的每个方法** | `/// <summary>` + `/// <param>` + `/// <returns>` | 语义、输入约束、返回含义、是否幂等/有副作用 |
| **类的每个方法（含私有）** | `/// <summary>` | 用途、输入输出、边界条件；私有方法说明"为什么存在" |
| **DTO/Command/Query/实体的每个字段与属性** | `/// <summary>`（或紧随其后的行内说明） | 业务含义、单位、取值/枚举含义、默认值、可空语义 |
| 枚举的每个成员 | `/// <summary>` | 触发条件与去向 |
| 控制器每个动作 | `/// <summary>` | 接口用途、入参、权限点、协议差异（HTTP 200+code vs 400） |
| Validator 类 | `/// <summary>` | 覆盖哪些字段、什么规则、与实体的长度/范围对齐关系 |
| 仓储接口方法 | `/// <summary>` + `/// <param>` + `/// <returns>` | 过滤条件、分页语义、返回元组含义 |
| MQ 消费者 | `/// <summary>` | 队列名、绑定事件、幂等键、失败策略 |

> 判断标准：**打开任意一个文件，不看实现也能凭注释知道每个字段能填什么、每个方法会做什么、失败会发生什么**。`[Description]` 特性（实体列描述）可以与 XML 注释并存，但不能替代字段注释。

### 5.1 必须写 XML 注释（`/// <summary>`）的位置

| 对象 | 注释内容要求 | 示例 |
|------|--------------|------|
| 所有 public 类/接口 | 职责 + 在链路中的位置 + 关键约束（锁/幂等/租户/事件） | `CreateOrderHandler`、`RedisDistributedLock` |
| 所有 Handler | 同上，并说明失败语义（返回什么码/消息） | `ApproveRefundCommandHandler` |
| public 接口方法 | 参数与返回的业务含义 | `IDistributedLock.AcquireAsync` |
| 实体类 + 非自明属性 | 业务含义；状态字段必须列全状态值 | `PaymentOrder.Status`（10/20/30/40） |
| 枚举 | 每个成员的触发条件与去向 | `OrderState` |
| 仓储自定义方法 | 过滤条件、返回元组含义 | `IUserRepository.QueryPagedAsync` |

### 5.2 必须写行内注释（`//`）的位置

1. **分布式约束**：为什么加锁、锁键含义、锁内为什么要重读（"锁内重读避免用旧状态覆盖关单结果"）。
2. **幂等设计**：幂等键放哪、重复投递如何去重（"流水按 BizNo+SKU+动作 幂等"）。
3. **补偿逻辑**：失败时回滚什么、bizNo 后缀如何防重（"`:lock-compensate` 后缀防重"）。
4. **业务规则来源**：魔法数字/限制的业务原因（"第三次循环说明会创建第四级 → 分类不能超 3 级"）。
5. **安全考量**：为什么强制覆盖某字段（"强制 UserId = tenant.UserId，避免替他人支付"）。

### 5.3 禁止

- 把需求原文、提交记录、"修复了 xxx bug"写进注释（那属于文档/提交信息）。
- 注释与代码行为不一致——改代码必须同步改注释（见 5.4）。
- 空话注释：`// 获取用户`、`// 遍历列表`。

### 5.4 同步义务

修改业务逻辑时，同一次改动内必须更新受影响的类头与步骤注释；`REVIEW.md` 记录的链路若受影响，也要同步修订对应小节。

### 5.5 参考样板

写得好的文件（新代码照着写）：
- `OrderService.Application/Features/CreateOrder/CreateOrderHandler.cs`（步骤注释 + 补偿说明）
- `CommunalService.Domain/Infrastructure/Locks/RedisDistributedLock.cs`（算法 + 使用约定）
- `CommunalService.Domain/Logging/LoggingEventPublisher.cs`（职责边界 + 失败降级）
- `PaymentService.Application/Features/ConfirmPayment/ConfirmPaymentHandler.cs`（幂等分支说明）

## 6. 已知陷阱

1. `Features/{User|Address|Favorite|Permission|Category|Product}` 命名空间段会遮蔽同名实体（CS0118）→ 用 `using XxxEntity = ...` 别名。
2. FreeSql `.Page(pageNumber, pageSize)` 第一参数是**页码**，不是偏移量。
3. 雪花 ID 全局 JSON 配置自动转字符串；Handler 内用 `long` 比较，不要 `ToString()` 后比。
4. `IRequest<T>` 与 Handler 泛型必须一致（改返回类型时两处同步，否则 CS0311）。
5. `IBaseRepository.UpdateAsync(entity)` 单参；带 CancellationToken 的重载是部分服务自定义接口才有的。
6. **后端数字统一按字符串下发**（雪花 ID 防精度丢失，全局 `JsonNumberHandling.WriteAsString`）：前端比较/回填枚举、金额、数量必须先 `Number()`，否则 `el-radio/el-select` 严格比较不回显、`===` 判断失效；但**ID 禁止 `Number()`**（雪花 ID 超出 JS 安全整数会丢精度），ID 原样用字符串传参与比较，仅"是否为 0"可用 `Number(id) > 0` 判断。
7. 服务端种子代码不要依赖雪花 AOP 自动生成主键（启动阶段 Yitter 尚未初始化会 NRE）；新增固定主键时用"当前最大 Id + 1"。
8. **列表查询必须显式 ORDER BY**：PostgreSQL 无排序时按堆物理顺序返回（更新/清理后变化），表现为"随机排序"。营销引擎的活动遍历也按 `CreatedAt, Id` 固定，保证同优惠力度/满赠兜底时结果确定。
9. `/carts/Add` 是**累加数量**语义：调用方传本次增量（加购传购买数量、购物车加减传 ±1），不要传"目标数量"，否则会出现倍数增长；累计上限 99 由服务端与前端双重校验。
10. **字段级更新必须用 `SetDto`**：`UpdateColumns(a => obj)` 对捕获的匿名对象解析不出列，会生成空 SET 静默不更新（`BaseRepository.UpdateColumnsAsync` 曾因此让活动启停/软删全部失效）；正确写法 `freeSql.Update<T>().Where(x => x.Id == id).SetDto(obj)`，且更新后要断言数据库（`full-chain.sh` 有落库断言样板）。
11. **营销读路径走 `MarketingSnapshotCache`**：配置/启用活动/范围按平台缓存，写操作（保存活动、启停、保存配置）必须调用 `Invalidate(platformId)`；快照不缓存时间窗口，使用时按 `now` 过滤，避免活动提前/延迟生效。
12. **账号域禁止混用**：客户账号只能进 CustomerService（`/customers/*`，客户 JWT），后台账号只能进 UserService + AuthService（`/users/*` 管理、`/auth/Token` 登录，OpenIddict 令牌）；后台建号禁止 `customer` 角色，客户账号从后台登录必须拒绝。
13. **统一上传入口**：所有文件上传（商品图/装修图/头像/后续附件）一律走 ToolService `/gateway/files/Upload`，业务服务禁止自建上传接口；允许格式与分类大小限制在 AgileConfig `FileStorage:*` 配置（默认本地存储，可切阿里云 OSS/腾讯云 COS/Azure Blob），校验顺序为扩展名白名单 → 大小 → 文件头魔数。
14. **权限只认显式绑定**：禁止用账号字段（如旧 `User.Role`）做权限兜底；`user_role` 无绑定即无后台权限（fail-closed），全局超管通过 `platform-admin` + PlatformId=0 的显式绑定表达。后台登录与客户登录严格分离，历史 issuer/兜底分支一律删除。
15. **OpenIddict 陷阱（AuthService）**：① 不要定义与 OpenIddict 自带 `ClientType`（public/confidential）同名的属性——会遮蔽它导致公开客户端仍被要求 client_secret（本项目的业务分类列已改名 `AppCategory`）；② 自定义声明必须 `RegisterClaims(...)` 且 `identity.SetDestinations(AccessToken)` 才会写入令牌；③ `sub` 声明是强制的；④ 令牌端点需 `DisableTransportSecurityRequirement()`（开发经 HTTP 网关）与 `AcceptAnonymousClients()`（SPA 公开客户端）；⑤ 签名必须非对称（RS256），证书用 `LocalSigningCertificate` 共享路径生成，网关读取同一文件验签。

## 7. 分层迁移完成度（2026-09-16）

UserService / PaymentService / OrderService / ProductService / MerchantPlatformService / PermissionService ✅ 已全部迁移；CustomerService ✅ 基准；MarketingService ✅ 新服务按四层落地（优惠计算集中在 `Application/Services/DiscountEngine`，跨服务调用走 gRPC + Consul）。
保留例外：文件上传、AuthService（EF Core + OpenIddict）、`PlatformAppConfigController` 纯查询转发、Merchant/Platform 列表直查（控制器内联分页兜底）。
