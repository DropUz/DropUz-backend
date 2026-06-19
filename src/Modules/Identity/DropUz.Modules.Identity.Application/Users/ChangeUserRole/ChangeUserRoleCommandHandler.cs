using DropUz.Common.Application.Helpers;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using DropUz.Modules.Identity.Application.Authentication;
using DropUz.Modules.Identity.Application.Roles;
using DropUz.Modules.Identity.Domain.Roles;
using DropUz.Modules.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace DropUz.Modules.Identity.Application.Users.ChangeUserRole;

public sealed class ChangeUserRoleCommandHandler(
    UserManager<User> userManager,
    RoleManager<AppRole> roleManager)
    : ICommandHandler<ChangeUserRoleCommand, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(
        ChangeUserRoleCommand command,
        CancellationToken cancellationToken)
    {
        string? role = StringNormalizer.Normalize(command.Role);
        if (role is null)
        {
            return Result.Failure<UserResponse>(AuthenticationErrors.RoleRequired);
        }

        if (!IdentityRoleStore.IsKnownRole(role))
        {
            return Result.Failure<UserResponse>(AuthenticationErrors.InvalidRole);
        }

        User? user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
        {
            return Result.Failure<UserResponse>(AuthenticationErrors.UserNotFound);
        }

        IdentityResult ensureRoleResult = await IdentityRoleStore.EnsureRoleExistsAsync(roleManager, role);
        if (!ensureRoleResult.Succeeded)
        {
            return Result.Failure<UserResponse>(
                AuthenticationErrors.IdentityFailure(ensureRoleResult.Errors));
        }

        IReadOnlyCollection<string> existingRoles = (await userManager.GetRolesAsync(user)).ToArray();
        string[] removableRoles = existingRoles
            .Where(IdentityRoleStore.IsKnownRole)
            .ToArray();

        if (removableRoles.Length > 0)
        {
            IdentityResult removeResult = await userManager.RemoveFromRolesAsync(user, removableRoles);
            if (!removeResult.Succeeded)
            {
                return Result.Failure<UserResponse>(
                    AuthenticationErrors.IdentityFailure(removeResult.Errors));
            }
        }

        string storageRoleName = IdentityRoleStore.ToStorageRoleName(role);
        IdentityResult addResult = await userManager.AddToRoleAsync(user, storageRoleName);
        if (!addResult.Succeeded)
        {
            return Result.Failure<UserResponse>(
                AuthenticationErrors.IdentityFailure(addResult.Errors));
        }

        IReadOnlyCollection<string> updatedRoles = (await userManager.GetRolesAsync(user))
            .Select(IdentityRoleStore.ToApiRoleName)
            .OrderBy(roleName => roleName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var response = new UserResponse(
            user.Id,
            user.UserName,
            user.PhoneNumber,
            user.FirstName,
            user.LastName,
            user.PhoneNumberConfirmed,
            user.EmailConfirmed,
            updatedRoles);

        return Result.Success(response);
    }
}
