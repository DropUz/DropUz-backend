using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;

namespace DropUz.Modules.Identity.Application.Users.GetUsers;

public sealed record GetUsersQuery(
    int? PageNumber,
    int? PageSize,
    string? Search,
    string? PhoneNumber,
    string? Role,
    string? SortBy,
    string? SortDirection) : IQuery<PagedResponse<UserResponse>>;
