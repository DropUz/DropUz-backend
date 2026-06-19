namespace DropUz.Common.Application.Authorization;

public static class RoleNameNormalizer
{
    private const string AppRolePrefix = "app.";

    public static string Normalize(string role)
    {
        string normalizedRole = role.Trim();

        if (normalizedRole.StartsWith(AppRolePrefix, StringComparison.OrdinalIgnoreCase))
        {
            normalizedRole = normalizedRole[AppRolePrefix.Length..];
        }

        return normalizedRole.ToLowerInvariant();
    }

    public static string ToStorageRoleName(string role)
    {
        return $"{AppRolePrefix}{Normalize(role)}";
    }

    public static bool IsKnownRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        string normalizedRole = Normalize(role);

        return ApplicationRoles.All.Contains(normalizedRole, StringComparer.OrdinalIgnoreCase);
    }
}
