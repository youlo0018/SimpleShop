using CommunalService.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using YukeTools;

namespace CommunalService.Domain;

[ApiController]
[Route("api/[controller]/[action]")]
public class BaseController : Microsoft.AspNetCore.Mvc.Controller
{
    [ApiExplorerSettings(IgnoreApi = true)]
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