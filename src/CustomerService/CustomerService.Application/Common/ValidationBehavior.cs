using FluentValidation; // FluentValidation 核心命名空间
using MediatR; // MediatR 核心命名空间
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommunalService.Application.Common;

/// <summary>
/// 泛型管道行为，用于对实现了 IRequest<TResponse> 的请求执行自动验证。
/// 它会在 Handler 执行之前运行，如果验证失败则抛出异常。
/// </summary>
/// <typeparam name="TRequest">请求类型（如 CreateOrderCommand）</typeparam>
/// <typeparam name="TResponse">响应类型（如 long）</typeparam>
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> // 实现 MediatR 的管道行为接口
    where TRequest : IRequest<TResponse> // 约束：TRequest 必须实现了 IRequest<TResponse>
{
    // 依赖注入：获取当前请求类型对应的所有验证器（可能有多个）
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// 构造函数，通过依赖注入（DI）注入所有实现了 IValidator<TRequest> 的验证器。
    /// 如果有多个验证器（例如通过继承），它们都会被注入。
    /// </summary>
    /// <param name="validators">所有验证器实例集合</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// MediatR 管道行为的核心方法，会在请求被 Handler 处理之前自动调用。
    /// </summary>
    /// <param name="request">当前请求对象（例如 CreateOrderCommand 实例）</param>
    /// <param name="next">管道中的下一个委托，代表后续处理（可能是下一个 Behavior 或最终的 Handler）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最终响应结果</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // 1. 检查是否有任何验证器被注册
        if (_validators.Any())
        {
            // 2. 创建 FluentValidation 的验证上下文，将当前请求对象传入
            var context = new ValidationContext<TRequest>(request);

            // 3. 并行执行所有验证器的验证方法（异步）
            //    使用 Task.WhenAll 提高性能，如果验证器很多可以并行执行
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))
            );

            // 4. 将所有验证结果中的错误（Error）提取出来，合并成一个扁平列表
            //    SelectMany 将多个 ValidationResult 中的 Errors 集合连接起来
            //    Where 过滤掉 null 值（一般不会有 null，但防御性编程）
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            // 5. 如果有任何错误，则抛出 FluentValidation 的 ValidationException
            //    该异常会被全局异常处理中间件捕获，并转换为 HTTP 400 BadRequest 响应
            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        // 6. 所有验证通过，继续执行管道中的下一个行为（或最终的 Handler）
        //    next 委托代表后续的处理链
        return await next();
    }
}