using System.Reflection;
using CommunalService.Application.Common;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerService.Application;

public static class DependencyInjection
{
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        // 1. 注册 MediatR（自动扫描并注册所有 Handler）
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // 2. 注册 FluentValidation（自动扫描并注册所有 Validator）
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // 3. 注册 MediatR 管道行为（用于自动验证）
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

     
    }
}