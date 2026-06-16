using System.Security.Claims;
using DropUz.Common.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace DropUz.Common.Infrastructure.Identity;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            string? value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Principal?.FindFirstValue("sub");

            return Guid.TryParse(value, out Guid userId) ? userId : null;
        }
    }

    public string? UserName => Principal?.Identity?.Name
        ?? Principal?.FindFirstValue(ClaimTypes.Name)
        ?? Principal?.FindFirstValue("preferred_username");

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public IReadOnlyCollection<string> Roles => Principal?
        .FindAll(claim => claim.Type is ClaimTypes.Role or "role" or "roles")
        .Select(claim => claim.Value)
        .Where(role => !string.IsNullOrWhiteSpace(role))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? [];

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;
}
