using CustomerService.Domain.Entity;
using FreeSql;

namespace CustomerService.Infrastructure;

/// <summary>客户服务表结构初始化：服务启动时自治迁移自己的表，避免请求路径触发 DDL。</summary>
public static class DatabaseInitializer
{
    /// <summary>同步客户账号/地址/收藏三张表结构。</summary>
    public static void Initialize(IFreeSql freeSql)
    {
        freeSql.CodeFirst.SyncStructure<Customer>();
        freeSql.CodeFirst.SyncStructure<CustomerAddress>();
        freeSql.CodeFirst.SyncStructure<CustomerFavorite>();
    }
}
