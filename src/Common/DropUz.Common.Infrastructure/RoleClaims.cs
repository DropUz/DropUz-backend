using System.Security.Claims;

namespace DropUz.Common.Infrastructure;

public static class RoleClaims
{
    private static readonly StringComparer RoleComparer = StringComparer.OrdinalIgnoreCase;

    public static bool HasAnyRole(ClaimsPrincipal principal, params string[] roles)
    {
        if (roles.Length == 0)
        {
            return false;
        }

        HashSet<string> expectedRoles = roles.ToHashSet(RoleComparer);

        return GetRoles(principal).Any(role => MatchesAnyRole(role, expectedRoles));
    }

    public static IReadOnlySet<string> GetRoles(ClaimsPrincipal? principal)
    {
        HashSet<string> roles = new(RoleComparer);

        foreach (Claim claim in principal?.Claims ?? [])
        {
            if (!IsRoleClaim(claim.Type))
            {
                continue;
            }

            foreach (string role in SplitClaimValues(claim.Value))
            {
                roles.Add(role);
            }
        }

        return roles;
    }

    private static bool IsRoleClaim(string claimType)
    {
        return claimType is "roles" or "role" || claimType == ClaimTypes.Role;
    }

    private static IEnumerable<string> SplitClaimValues(string value)
    {
        return value
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(role => !string.IsNullOrWhiteSpace(role));
    }

    private static bool MatchesAnyRole(string actualRole, HashSet<string> expectedRoles)
    {
        if (expectedRoles.Contains(actualRole))
        {
            return true;
        }

        string normalizedActualRole = NormalizeRoleName(actualRole);

        return expectedRoles.Any(role => NormalizeRoleName(role) == normalizedActualRole);
    }

    private static string NormalizeRoleName(string role)
    {
        return role
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}
