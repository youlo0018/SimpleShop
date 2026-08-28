using CustomerService.Domain.Entity;
using FreeSql;

namespace CustomerService.Infrastructure;

public static class DatabaseInitializer
{
    public static void Initialize(IFreeSql freeSql)
    {
        // 客户服务自治管理自己的表结构；避免请求路径触发 DDL。
        freeSql.CodeFirst.SyncStructure<Customer>();
    }
}
