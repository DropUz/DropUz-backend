using System.Security.Claims;
using DropUz.Modules.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DropUz.Modules.Identity.Application.Authentication.Token;

internal static class OpenIddictPrincipalFactory
{
    public static async Task<ClaimsPrincipal> CreateAsync(
        UserManager<User> userManager,
        User user,
        IEnumerable<string> requestedScopes)
    {
        var identity = new ClaimsIdentity(
            authenticationType: "OpenIddict",
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.JwtId, Guid.NewGuid().ToString("N"));
        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(ClaimTypes.NameIdentifier, user.Id.ToString());
        identity.SetClaim(Claims.Name, user.UserName ?? user.PhoneNumber ?? user.Id.ToString());
        identity.SetClaim(Claims.PhoneNumber, user.PhoneNumber ?? string.Empty);
        identity.SetClaim(Claims.GivenName, user.FirstName ?? string.Empty);
        identity.SetClaim(Claims.FamilyName, user.LastName ?? string.Empty);

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        foreach (string role in roles)
        {
            identity.AddClaim(new Claim(Claims.Role, role));
        }

        var scopes = new HashSet<string>(requestedScopes, StringComparer.OrdinalIgnoreCase)
        {
            Scopes.OpenId,
            Scopes.Profile,
            Scopes.Phone,
            Scopes.Roles,
            Scopes.OfflineAccess
        };

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);
        identity.SetDestinations(GetDestinations);

        return principal;
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        return claim.Type switch
        {
            Claims.JwtId => [],
            Claims.Subject or ClaimTypes.NameIdentifier or Claims.Name or
                Claims.PhoneNumber or Claims.GivenName or Claims.FamilyName or Claims.Role =>
                [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        };
    }
}
