using CommunalService.Domain;
using ToolService.Application.Features.GetFile;
using ToolService.Application.Features.UploadFile;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ToolService.Api.Controllers;

/// <summary>
/// 统一文件入口（后台/小程序/后续所有端共用）：
/// POST /gateway/files/Upload 上传；GET /gateway/files/Content/{key} 本地存储回源读取。
/// 存储后端与格式/大小限制全部来自 AgileConfig 配置。
/// </summary>
public class FileController(IMediator mediator) : BaseController
{
    /// <summary>上传文件（POST，multipart/form-data，字段名 file）：返回绝对访问地址。</summary>
    [HttpPost]
    public Task<ApiResponse> Upload(IFormFile file)
        => mediator.Send(new UploadFileCommand(file), CancellationToken.None);

    /// <summary>读取本地存储文件（GET）：云存储直接走对象地址，不经过本服务。</summary>
    [HttpGet("{**key}")]
    public async Task<IActionResult> Content(string key)
    {
        var result = await mediator.Send(new GetFileQuery(key), CancellationToken.None);
        if (result is null) return NotFound();
        return File(result.Value.Stream, result.Value.ContentType);
    }
}
