using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.User.ListUsers;

public record ListUsersQuery(string Keyword = "", int Page = 1, int PageSize = 10, string Role = "") : IRequest<ApiResponse>;
