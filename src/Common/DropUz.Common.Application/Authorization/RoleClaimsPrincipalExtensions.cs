using System.Security.Claims;

namespace DropUz.Common.Application.Authorization;

public static class RoleClaimsPrincipalExtensions
{
    public static bool HasAnyApplicationRole(this ClaimsPrincipal principal, params string[] roles)
    {
        if (roles.Length == 0)
        {
            return false;
        }

        HashSet<string> expectedRoles = roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(RoleNameNormalizer.Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return principal.GetApplicationRoles()
            .Any(role => expectedRoles.Contains(RoleNameNormalizer.Normalize(role)));
    }

    public static IReadOnlyCollection<string> GetApplicationRoles(this ClaimsPrincipal principal)
    {
        return principal.Claims
            .Where(claim => claim.Type is ClaimTypes.Role or "role" or "roles")
            .SelectMany(claim => claim.Value.Split(
                [' ', ','],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
