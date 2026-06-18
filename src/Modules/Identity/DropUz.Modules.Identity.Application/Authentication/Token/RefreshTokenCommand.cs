using System.Security.Claims;
using DropUz.Common.Application.Messaging;

namespace DropUz.Modules.Identity.Application.Authentication.Token;

public sealed record RefreshTokenCommand(
    string? UserId,
    IReadOnlyCollection<string> Scopes) : ICommand<ClaimsPrincipal>;
