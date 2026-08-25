namespace CommunalService.Domain.Logging;

using Microsoft.AspNetCore.Http;

/// <summary>
/// 业务操作日志入口：关键命令执行成功后主动记录一笔，方便审计时还原“谁动了什么”。
/// </summary>
public interface IOperationLogger
{
    Task LogAsync(
        HttpContext httpContext,
        string operationType,
        string objectType,
        string objectId,
        string summary,
        CancellationToken cancellationToken = default);
}
