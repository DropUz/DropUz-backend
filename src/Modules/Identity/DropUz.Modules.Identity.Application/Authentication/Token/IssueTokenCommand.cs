using DropUz.Common.Application.Messaging;
using System.Security.Claims;

namespace DropUz.Modules.Identity.Application.Authentication.Token;

public sealed record IssueTokenCommand(
    string? UserName,
    string? Password,
    IReadOnlyCollection<string> Scopes) : ICommand<ClaimsPrincipal>;
