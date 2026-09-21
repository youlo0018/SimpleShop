using CommunalService.Domain;
using MediatR;

namespace ToolService.Application.Features.GetFile;

/// <summary>按对象键读取文件内容（本地存储回源；云存储直接走对象 URL，不经过本服务）。</summary>
/// <param name="ObjectKey">对象键。</param>
public sealed record GetFileQuery(string ObjectKey) : IRequest<(Stream Stream, string ContentType)?>;
