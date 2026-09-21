using CommunalService.Domain;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ToolService.Application.Features.UploadFile;

/// <summary>统一文件上传命令：所有业务（商品图/装修图/头像/附件）共用此入口。</summary>
/// <param name="File">上传的文件（multipart/form-data）。</param>
public sealed record UploadFileCommand(IFormFile File) : IRequest<ApiResponse>;
