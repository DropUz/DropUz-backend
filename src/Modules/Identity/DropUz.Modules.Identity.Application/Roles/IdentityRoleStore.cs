using DropUz.Common.Application.Authorization;
using DropUz.Modules.Identity.Domain.Roles;
using Microsoft.AspNetCore.Identity;

namespace DropUz.Modules.Identity.Application.Roles;

internal static class IdentityRoleStore
{
    public static async Task<IdentityResult> EnsureRoleExistsAsync(
        RoleManager<AppRole> roleManager,
        string role)
    {
        string storageRoleName = RoleNameNormalizer.ToStorageRoleName(role);

        if (await roleManager.RoleExistsAsync(storageRoleName))
        {
            return IdentityResult.Success;
        }

        return await roleManager.CreateAsync(new AppRole(storageRoleName));
    }

    public static string ToStorageRoleName(string role)
    {
        return RoleNameNormalizer.ToStorageRoleName(role);
    }

    public static string ToApiRoleName(string role)
    {
        return RoleNameNormalizer.Normalize(role);
    }

    public static bool IsKnownRole(string? role)
    {
        return RoleNameNormalizer.IsKnownRole(role);
    }
}
