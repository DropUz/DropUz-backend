namespace DropUz.Modules.Identity.Application.Authentication.Me;

public sealed record CurrentUserResponse(
    bool IsAuthenticated,
    Guid? UserId,
    string? UserName,
    string? PhoneNumber,
    string? FirstName,
    string? LastName,
    IReadOnlyCollection<string> Roles);
