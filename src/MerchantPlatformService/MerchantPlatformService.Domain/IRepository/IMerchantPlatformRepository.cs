using CommunalService.Domain.Interfaces;
using CommunalService.Domain.Entity;
using MerchantPlatformService.Domain.Entity;

namespace MerchantPlatformService.Domain.IRepository;

public interface IMerchantRepository : IBaseRepository<Merchant>
{
    /// <summary>商户审核状态更新（带取消令牌）。</summary>
    Task<bool> UpdateAsync(Merchant merchant, CancellationToken cancellationToken = default);

    /// <summary>后台分页查询商户（关键字/状态/平台过滤），返回页数据与总数。</summary>
    Task<(List<Merchant> Items, long Total)> QueryPagedAsync(string keyword, int? status, long platformId, int page, int pageSize);
}

public interface IPlatformConfigRepository : IBaseRepository<PlatformConfig>
{
    Task<bool> UpdateAsync(PlatformConfig config, CancellationToken cancellationToken = default);
    Task<bool> UpsertAsync(PlatformConfig config, CancellationToken cancellationToken = default);
}
