using CommunalService.Domain.Enums;

namespace CommunalService.Domain;

/// <summary>
/// Application 层 Handler 构造统一响应的入口；控制器不再手写业务响应，只转发 Handler 结果。
/// </summary>
public static class ApiResults
{
    public static ApiResponse Ok(object? data = null) => new()
    {
        Code = BaseApiResponseCode.Success.Value,
        Message = BaseApiResponseCode.Success.Name,
        Data = data ?? new { }
    };

    public static ApiResponse Fail(BaseApiResponseCode code, string? message = null, object? errors = null) => new()
    {
        Code = code.Value,
        Message = message ?? code.Name,
        Data = errors ?? new { }
    };
}
