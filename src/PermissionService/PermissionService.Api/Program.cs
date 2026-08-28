using AgileConfig.Client;
using CommunalService.Application.Common;
using CommunalService.Domain;
using CommunalService.Domain.Infrastructure.Locks;
using FreeSql;
using PermissionService.Domain.Entity;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddAgileConfig(new ConfigClient(new ConfigClientOptions
{
    AppId = builder.Configuration["AgileConfig:appId"],
    Secret = builder.Configuration["AgileConfig:secret"],
    Nodes = builder.Configuration["AgileConfig:nodes"],
    Name = builder.Configuration["AgileConfig:name"],
    Tag = builder.Configuration["AgileConfig:tag"],
    ENV = builder.Configuration["AgileConfig:env"]
}));

builder.Services.AddOpenApi();
builder.AddBasicServices();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();
await app.AddBaseInfrastructure();
await SeedAsync(app);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.MapControllers();
app.Run();

static async Task SeedAsync(WebApplication app)
{
    var lockProvider = app.Services.GetRequiredService<IDistributedLock>();
    await using var seedLock = await lockProvider.AcquireAsync("lock:permission:bootstrap", TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(10));
    if (seedLock is null) return;

    var freeSql = app.Services.GetRequiredService<IFreeSql>();
    freeSql.CodeFirst.SyncStructure<Permission>();
    freeSql.CodeFirst.SyncStructure<Role>();
    freeSql.CodeFirst.SyncStructure<RolePermission>();
    freeSql.CodeFirst.SyncStructure<UserRole>();

    var permissions = new (string Code, string Name, string Resource, string Action, int AllowedScopes)[]
    {
        ("dashboard:view", "查看经营概览", "dashboard", "read", 3),
        ("user:read", "查询用户", "user", "read", 1),
        ("user:create", "创建用户", "user", "create", 1),
        ("user:update-status", "变更用户状态", "user", "update", 1),
        ("category:read", "查询分类", "category", "read", 3),
        ("category:create", "创建分类", "category", "create", 3),
        ("product:read", "查询商品", "product", "read", 3),
        ("product:create", "创建商品", "product", "create", 3),
        ("product:publish", "上架商品", "product", "publish", 3),
        ("order:read", "查询订单", "order", "read", 3),
        ("order:ship", "订单发货", "order", "ship", 2),
        ("order:receive", "确认收货", "order", "receive", 2),
        ("order:cancel", "取消订单", "order", "cancel", 3),
        ("refund:read", "查询退款", "refund", "read", 3),
        ("refund:approve", "审批退款", "refund", "approve", 3),
        ("merchant:read", "查询商户", "merchant", "read", 1),
        ("merchant:create", "创建商户", "merchant", "create", 1),
        ("merchant:review", "审核商户", "merchant", "review", 1),
        ("platform:read", "查询平台", "platform", "read", 1),
        ("platform:create", "创建平台", "platform", "create", 1),
        ("report:read", "查看业务报表", "report", "read", 3),
        ("permission:manage", "管理角色权限", "permission", "manage", 1)
    };

    var existingPermissionCodes = (await freeSql.Select<Permission>().ToListAsync()).Select(item => item.Code).ToHashSet();
    var permissionEntities = permissions.Where(item => !existingPermissionCodes.Contains(item.Code)).Select((item, index) => new Permission
    {
        Id = index + 1, Code = item.Code, Name = item.Name, Resource = item.Resource, Action = item.Action, AllowedScopes = item.AllowedScopes
    }).ToList();
    if (permissionEntities.Count > 0) await freeSql.Insert(permissionEntities).ExecuteAffrowsAsync();
    var scopeMap = permissions.ToDictionary(item => item.Code, item => item.AllowedScopes);
    foreach (var permission in await freeSql.Select<Permission>().ToListAsync())
    {
        var scope = scopeMap.GetValueOrDefault(permission.Code, 3);
        if (permission.AllowedScopes != scope)
        {
            permission.AllowedScopes = scope;
            await freeSql.Update<Permission>().SetSource(permission).ExecuteAffrowsAsync();
        }
    }

    var roleSeeds = new (string Code, string Name, TenantType Type, string Description, string[] Codes)[]
    {
        ("platform-admin", "平台管理员", TenantType.Platform, "拥有平台内全部权限", ["*"]),
        ("platform-operator", "平台业务员", TenantType.Platform, "维护商品、商户并审批退款", ["dashboard:view", "product:read", "product:create", "product:publish", "category:read", "category:create", "merchant:read", "merchant:create", "merchant:review", "refund:read", "refund:approve"]),
        ("platform-finance", "平台财务", TenantType.Platform, "只读订单、退款和经营报表", ["dashboard:view", "order:read", "refund:read", "report:read"]),
        ("merchant-admin", "商户管理员", TenantType.Merchant, "管理本商户商品、订单和退款", ["dashboard:view", "product:read", "product:create", "product:publish", "order:read", "order:ship", "refund:read", "refund:approve"]),
        ("merchant-operator", "商户业务员", TenantType.Merchant, "维护本商户商品和订单", ["product:read", "product:create", "product:publish", "order:read", "order:ship"]),
        ("merchant-finance", "商户财务", TenantType.Merchant, "只读本商户订单、退款和报表", ["dashboard:view", "order:read", "refund:read", "report:read"])
    };
    var existingRoles = await freeSql.Select<Role>().ToListAsync();
    var permissionMap = (await freeSql.Select<Permission>().ToListAsync()).ToDictionary(item => item.Code, item => item.Id);
    var seedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var mappingIndex = 0;
    foreach (var seed in roleSeeds)
    {
        var roleId = 100 + Array.IndexOf(roleSeeds, seed) + 1;
        var role = existingRoles.FirstOrDefault(item => item.Code == seed.Code);
        if (role is null)
        {
            role = new Role { Id = roleId, Code = seed.Code, Name = seed.Name, TenantType = (int)seed.Type, Description = seed.Description, IsSystem = true };
            await freeSql.Insert(role).ExecuteAffrowsAsync();
        }
        else
        {
            role.Name = seed.Name; role.Description = seed.Description; role.IsSystem = true;
            await freeSql.Update<Role>().SetSource(role).ExecuteAffrowsAsync();
        }

        var permissionIds = seed.Codes.Where(code => permissionMap.ContainsKey(code)).Select(code => permissionMap[code]).ToList();
        var mappings = await freeSql.Select<RolePermission>().DisableGlobalFilter("SoftDelete")
            .Where(item => item.RoleId == role.Id).ToListAsync();
        await freeSql.Update<RolePermission>().Where(item => item.RoleId == role.Id && !permissionIds.Contains(item.PermissionId))
            .Set(item => item.IsDeleted, true).ExecuteAffrowsAsync();
        var inserts = new List<RolePermission>();
        foreach (var permissionId in permissionIds)
        {
            var mapping = mappings.FirstOrDefault(item => item.PermissionId == permissionId);
            if (mapping is null)
                inserts.Add(new RolePermission { Id = seedTime * 1000 + Interlocked.Increment(ref mappingIndex), RoleId = role.Id, PermissionId = permissionId });
            else if (mapping.IsDeleted)
                await freeSql.Update<RolePermission>().DisableGlobalFilter("SoftDelete").Where(item => item.Id == mapping.Id)
                    .Set(item => item.IsDeleted, false).ExecuteAffrowsAsync();
        }
        if (inserts.Count > 0) await freeSql.Insert(inserts).ExecuteAffrowsAsync();
    }
}
