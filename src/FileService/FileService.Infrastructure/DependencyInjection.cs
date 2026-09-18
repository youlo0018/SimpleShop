
using FileService.Common.Services;
using FileService.Common.Services;
using FileService.Domain.IRepository;
using FileService.Common.Options;
using FileService.Infrastructure.Repository;
using FileService.Infrastructure.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.Infrastructure;

/// <summary>
/// 基础设施注册：仓储 + 按配置（FileStorage:Provider）选择存储后端。
/// 切换本地/阿里云/腾讯云/微软云只改 AgileConfig，无需改代码。
/// </summary>
public static class DependencyInjection
{
    /// <summary>注册仓储与存储后端（必须在 builder.Build() 之前调用）。</summary>
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IFileRepository, FileRepository>();
        builder.Services.AddSingleton<LocalFileStorage>();
        builder.Services.AddSingleton<AliyunOssFileStorage>();
        builder.Services.AddSingleton<TencentCosFileStorage>();
        builder.Services.AddSingleton<AzureBlobFileStorage>();
        builder.Services.AddSingleton<IFileStorage>(provider =>
        {
            var providerName = builder.Configuration[$"{FileStorageOptions.SectionName}:Provider"] ?? "Local";
            return providerName.Trim().ToLowerInvariant() switch
            {
                "aliyunoss" or "aliyun" or "oss" => provider.GetRequiredService<AliyunOssFileStorage>(),
                "tencentcos" or "tencent" or "cos" => provider.GetRequiredService<TencentCosFileStorage>(),
                "azureblob" or "azure" => provider.GetRequiredService<AzureBlobFileStorage>(),
                _ => provider.GetRequiredService<LocalFileStorage>()
            };
        });
    }
}
