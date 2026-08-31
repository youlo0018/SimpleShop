using CommunalService.Domain;
using MediatR;

namespace UserService.Application.Features.User.GetProfile;

public record GetProfileQuery(long Id = 0) : IRequest<ApiResponse>;
