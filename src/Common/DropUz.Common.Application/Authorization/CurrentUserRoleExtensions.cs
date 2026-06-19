using DropUz.Common.Application.Abstractions;

namespace DropUz.Common.Application.Authorization;

public static class CurrentUserRoleExtensions
{
    public static bool HasAnyApplicationRole(this ICurrentUser currentUser, params string[] roles)
    {
        if (roles.Length == 0)
        {
            return false;
        }

        HashSet<string> expectedRoles = roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(RoleNameNormalizer.Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return currentUser.Roles
            .Any(role => expectedRoles.Contains(RoleNameNormalizer.Normalize(role)));
    }
}
