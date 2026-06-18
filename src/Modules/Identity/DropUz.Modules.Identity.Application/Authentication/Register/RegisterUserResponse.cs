namespace DropUz.Modules.Identity.Application.Authentication.Register;

public sealed record RegisterUserResponse(
    Guid UserId,
    string? PhoneNumber,
    string? FirstName,
    string? LastName);
