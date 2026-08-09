using AgileConfig.Client;
using OrderService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// builder.Configuration
//     .SetBasePath(Directory.GetCurrentDirectory())
//     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
//     .AddEnvironmentVariables();
//
// // 调试：打印所有配置节
// foreach (var child in builder.Configuration.GetChildren())
// {
//     Console.WriteLine($"Top-level key: {child.Key}");
// }
//
// // 检查 AgileConfig 节是否存在
// var agileSection = builder.Configuration.GetSection("AgileConfig");
// if (agileSection.Exists())
// {
//     Console.WriteLine("✅ AgileConfig section found!");
//     foreach (var sub in agileSection.GetChildren())
//     {
//         Console.WriteLine($"  {sub.Key} = {sub.Value}");
//     }
// }
// else
// {
//     Console.WriteLine("❌ AgileConfig section NOT found!");
// }
// var agileConfigSection = builder.Configuration.GetSection("AgileConfig");
// if (agileConfigSection == null || !agileConfigSection.GetChildren().Any())
// {
//     throw new Exception("AgileConfig section not found in appsettings.json");
// }

// Console.WriteLine(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

ConfigClientOptions  agileConfigOptions = new ConfigClientOptions()
{
     AppId = builder.Configuration["AgileConfig:appId"],
     Tag = builder.Configuration["AgileConfig:tag"],
     Secret = builder.Configuration["AgileConfig:secret"],
     Nodes = builder.Configuration["AgileConfig:nodes"],
     Name = builder.Configuration["AgileConfig:name"],
     ENV = builder.Configuration[ "AgileConfig:env"]
};
builder.Host.UseAgileConfig(agileConfigOptions);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  
    app.MapOpenApi();
    app.UseSwaggerUI(options => {
        options.SwaggerEndpoint("/openapi/v1.json", "My API V1");
    });
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};



app.MapControllers();
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}