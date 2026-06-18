namespace DropUz.Modules.Identity.Presentation.Contracts;

public sealed record RegisterUserRequest(
    string? PhoneNumber,
    string? Password,
    string FirstName,
    string? LastName);
