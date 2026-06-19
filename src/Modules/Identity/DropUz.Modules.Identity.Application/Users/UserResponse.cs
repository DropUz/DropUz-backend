namespace DropUz.Modules.Identity.Application.Users;

public sealed record UserResponse(
    Guid UserId,
    string? UserName,
    string? PhoneNumber,
    string? FirstName,
    string? LastName,
    bool PhoneNumberConfirmed,
    bool EmailConfirmed,
    IReadOnlyCollection<string> Roles);
