using CommunalService.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using YukeTools;

namespace CommunalService.Domain;

[ApiController]
[Route("api/[controller]/[action]")]
public class BaseController : Microsoft.AspNetCore.Mvc.Controller
{
    [ApiExplorerSettings(IgnoreApi = true)]
    /// <summary>成功响应（业务码 200 + 数据）。</summary>
    protected ApiResponse Ok(object data)
    {
        return new ApiResponse()
        {
            Code = BaseApiResponseCode.Success.Value,
            Message = BaseApiResponseCode.Success.Name,
            Data = data
        };
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    /// <summary>成功响应（无数据）。</summary>
    protected ApiResponse Ok()
    {
        return new ApiResponse()
        {
            Code = BaseApiResponseCode.Success.Value,
            Message = BaseApiResponseCode.Success.Name,
            Data = new { }
        };
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    /// <summary>按业务码返回成功响应（通常用于带特殊成功码的场景）。</summary>
    protected ApiResponse Ok(BaseApiResponseCode code)
    {
        return new ApiResponse()
        {
            Code = code.Value,
            Message = code.Name,
            Data = new { }
        };
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    /// <summary>按业务码返回带数据的响应。</summary>
    protected ApiResponse Ok(object data, BaseApiResponseCode code)
    {
        return new ApiResponse()
        {
            Code = code.Value,
            Message = code.Name,
            Data = data
        };
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    /// <summary>失败响应（业务码 + 可直接展示的消息，HTTP 状态由异常/验证管道决定）。</summary>
    protected ApiResponse Error(BaseApiResponseCode code, string message = null)
    {
        return new ApiResponse()
        {
            Code = code.Value,
            Message = message.IsNull() ? code.Name : message,
            Data = { }
        };
    }
}
