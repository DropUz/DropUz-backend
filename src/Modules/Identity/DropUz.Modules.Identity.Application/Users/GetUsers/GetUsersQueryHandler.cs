using DropUz.Common.Application.Authorization;
using DropUz.Common.Application.Helpers;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;
using DropUz.Common.Domain;
using DropUz.Modules.Identity.Application.Authentication;
using DropUz.Modules.Identity.Application.Data;
using DropUz.Modules.Identity.Application.Roles;
using DropUz.Modules.Identity.Domain.Roles;
using DropUz.Modules.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Identity.Application.Users.GetUsers;

public sealed class GetUsersQueryHandler(
    UserManager<User> userManager,
    IIdentityRepository repository)
    : IQueryHandler<GetUsersQuery, PagedResponse<UserResponse>>
{
    public async Task<Result<PagedResponse<UserResponse>>> Handle(
        GetUsersQuery query,
        CancellationToken cancellationToken)
    {
        var pageRequest = new PageRequest(query.PageNumber ?? 1, query.PageSize ?? 20);
        IQueryable<User> users = userManager.Users.AsNoTracking();

        string? search = StringNormalizer.Normalize(query.Search)?.ToLowerInvariant();
        if (search is not null)
        {
            users = users.Where(user =>
                (user.UserName != null && user.UserName.ToLower().Contains(search)) ||
                (user.PhoneNumber != null && user.PhoneNumber.ToLower().Contains(search)) ||
                user.FirstName.ToLower().Contains(search) ||
                (user.LastName != null && user.LastName.ToLower().Contains(search)));
        }

        string? phoneNumber = StringNormalizer.Normalize(query.PhoneNumber);
        if (phoneNumber is not null)
        {
            users = users.Where(user => user.PhoneNumber != null && user.PhoneNumber.Contains(phoneNumber));
        }

        string? role = StringNormalizer.Normalize(query.Role);
        if (role is not null)
        {
            if (!IdentityRoleStore.IsKnownRole(role))
            {
                return Result.Failure<PagedResponse<UserResponse>>(AuthenticationErrors.InvalidRole);
            }

            string normalizedStorageRoleName = RoleNameNormalizer
                .ToStorageRoleName(role)
                .ToUpperInvariant();
            IQueryable<Guid> userIdsInRole =
                from userRole in repository.Query<IdentityUserRole<Guid>>()
                join appRole in repository.Query<AppRole>() on userRole.RoleId equals appRole.Id
                where appRole.NormalizedName == normalizedStorageRoleName
                select userRole.UserId;

            users = users.Where(user => userIdsInRole.Contains(user.Id));
        }

        users = ApplySorting(users, query.SortBy, query.SortDirection);

        int totalCount = await users.CountAsync(cancellationToken);
        List<User> items = await users
            .Skip(pageRequest.Skip)
            .Take(pageRequest.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, IReadOnlyCollection<string>> rolesByUserId = await GetRolesByUserIdAsync(
            items.Select(user => user.Id).ToArray(),
            cancellationToken);

        UserResponse[] responseItems = items
            .Select(user => new UserResponse(
                user.Id,
                user.UserName,
                user.PhoneNumber,
                user.FirstName,
                user.LastName,
                user.PhoneNumberConfirmed,
                user.EmailConfirmed,
                rolesByUserId.TryGetValue(user.Id, out IReadOnlyCollection<string>? roles)
                    ? roles
                    : []))
            .ToArray();

        var response = new PagedResponse<UserResponse>(
            responseItems,
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize,
            totalCount);

        return Result.Success(response);
    }

    private async Task<Dictionary<Guid, IReadOnlyCollection<string>>> GetRolesByUserIdAsync(
        Guid[] userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Length == 0)
        {
            return [];
        }

        var roleRows = await (
            from userRole in repository.Query<IdentityUserRole<Guid>>()
            join appRole in repository.Query<AppRole>() on userRole.RoleId equals appRole.Id
            where userIds.Contains(userRole.UserId)
            select new UserRoleRow(userRole.UserId, appRole.Name ?? string.Empty))
            .ToListAsync(cancellationToken);

        return roleRows
            .GroupBy(row => row.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group
                    .Select(row => IdentityRoleStore.ToApiRoleName(row.RoleName))
                    .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
                    .ToArray());
    }

    private static IQueryable<User> ApplySorting(
        IQueryable<User> users,
        string? sortBy,
        string? sortDirection)
    {
        bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        string normalizedSortBy = StringNormalizer.Normalize(sortBy)?.ToLowerInvariant() ?? "username";

        return normalizedSortBy switch
        {
            "phone" or "phonenumber" => descending
                ? users.OrderByDescending(user => user.PhoneNumber)
                : users.OrderBy(user => user.PhoneNumber),
            "firstname" or "first_name" => descending
                ? users.OrderByDescending(user => user.FirstName)
                : users.OrderBy(user => user.FirstName),
            "lastname" or "last_name" => descending
                ? users.OrderByDescending(user => user.LastName)
                : users.OrderBy(user => user.LastName),
            _ => descending
                ? users.OrderByDescending(user => user.UserName)
                : users.OrderBy(user => user.UserName)
        };
    }

    private sealed record UserRoleRow(Guid UserId, string RoleName);
}
