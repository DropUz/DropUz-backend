using DropUz.Common.Application.Messaging;

namespace DropUz.Modules.Identity.Application.Authentication.Register;

public sealed record RegisterUserCommand(
    string? PhoneNumber,
    string? Password,
    string FirstName,
    string? LastName) : ICommand<RegisterUserResponse>;
