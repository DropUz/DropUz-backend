using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using DropUz.Modules.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace DropUz.Modules.Identity.Application.Authentication.Me;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUser currentUser,
    UserManager<User> userManager)
    : IQueryHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    public async Task<Result<CurrentUserResponse>> Handle(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure<CurrentUserResponse>(AuthenticationErrors.NotAuthenticated);
        }

        var user = await userManager.FindByIdAsync(currentUser.UserId.Value.ToString());
        if (user is null)
        {
            return Result.Failure<CurrentUserResponse>(AuthenticationErrors.CurrentUserNotFound);
        }

        var roles = (await userManager.GetRolesAsync(user))
            .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var response = new CurrentUserResponse(
            IsAuthenticated: true,
            UserId: user.Id,
            UserName: user.UserName,
            PhoneNumber: user.PhoneNumber,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Roles: roles);

        return Result.Success(response);
    }
}
