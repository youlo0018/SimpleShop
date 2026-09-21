using ToolService.Common.Options;
using ToolService.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ToolService.Application;

/// <summary>应用层注册：存储配置绑定、上传校验器（校验规则来自配置，AgileConfig 可热更）。</summary>
public static class DependencyInjection
{
    /// <summary>注册应用服务（必须在 builder.Build() 之前调用）。</summary>
    public static void AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection(FileStorageOptions.SectionName));
        builder.Services.AddSingleton(provider =>
        {
            var options = new FileStorageOptions();
            builder.Configuration.GetSection(FileStorageOptions.SectionName).Bind(options);
            return new FileUploadValidator(options);
        });
    }

    /// <summary>应用层启动钩子（当前无操作，保留统一启动入口）。</summary>
    public static void AddApplication(this WebApplication app) { }
}
