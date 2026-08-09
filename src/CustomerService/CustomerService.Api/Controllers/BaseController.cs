using CommunalService.Domain;
using CustomerService.Domin.Enums;
using Microsoft.AspNetCore.Mvc;
using YukeTools;

namespace CustomerService.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class BaseController : Controller
{
    protected ApiResponse<T> Ok<T>(T data)
    {
        return new ApiResponse<T>()
        {
            Code = ApiResponseCode.Success.Value,
            Message = ApiResponseCode.Success.Name,
            Data = data
        };
    }

    protected ApiResponse<object> Ok()
    {
        return new ApiResponse<object>()
        {
            Code = ApiResponseCode.Success.Value,
            Message = ApiResponseCode.Success.Name,
            Data = new { }
        };
    }

    protected ApiResponse<object> Ok(ApiResponseCode code)
    {
        return new ApiResponse<object>()
        {
            Code = code.Value,
            Message = code.Name,
            Data = new { }
        };
    }


    protected ApiResponse<T> Ok<T>(T data, ApiResponseCode code)
    {
        return new ApiResponse<T>()
        {
            Code = code.Value,
            Message = code.Name,
            Data = data
        };
    }

    protected ApiResponse<object> Error(ApiResponseCode code, string message = null)
    {
        return new ApiResponse<object>
        {
            Code = code.Value,
            Message = message.IsNull() ? code.Name : message,
            Data = { }
        };
    }
}