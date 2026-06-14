using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using DropUz.Common.Application.Abstractions;

namespace DropUz.Common.Infrastructure;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private static readonly StringComparer PermissionComparer = StringComparer.OrdinalIgnoreCase;

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            string? value =
                GetClaimValue("user_id") ??
                GetClaimValue("uid") ??
                GetClaimValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(value, out Guid userId))
            {
                return userId;
            }

            string? subject = Subject;

            return Guid.TryParse(subject, out Guid subjectAsUserId)
                ? subjectAsUserId
                : null;
        }
    }

    public string? Subject => NormalizeOptional(GetClaimValue("sub") ?? GetClaimValue(ClaimTypes.NameIdentifier));

    public string? Email => NormalizeOptional(GetClaimValue("email") ?? GetClaimValue(ClaimTypes.Email))?.ToLowerInvariant();

    public string? UserName =>
        NormalizeOptional(
            GetClaimValue("preferred_username") ??
            GetClaimValue("username") ??
            GetClaimValue(ClaimTypes.Name));

    public IReadOnlySet<string> Roles => RoleClaims.GetRoles(Principal);

    public IReadOnlySet<string> Permissions
    {
        get
        {
            HashSet<string> permissions = new(PermissionComparer);

            foreach (Claim claim in Principal?.Claims ?? [])
            {
                if (!IsPermissionClaim(claim.Type))
                {
                    continue;
                }

                foreach (string permission in SplitClaimValues(claim.Value))
                {
                    permissions.Add(permission);
                }
            }

            return permissions;
        }
    }

    private string? GetClaimValue(string claimType)
    {
        return Principal?.FindFirst(claimType)?.Value;
    }

    private static bool IsPermissionClaim(string claimType)
    {
        return claimType is "permissions" or "permission" or "scope" or "scp";
    }

    private static IEnumerable<string> SplitClaimValues(string value)
    {
        return value
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
