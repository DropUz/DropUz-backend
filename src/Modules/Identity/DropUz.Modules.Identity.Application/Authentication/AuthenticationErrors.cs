using DropUz.Common.Domain;
using Microsoft.AspNetCore.Identity;

namespace DropUz.Modules.Identity.Application.Authentication;

public static class AuthenticationErrors
{
    public static readonly Error PhoneNumberRequired = Error.Validation(
        "Identity.PhoneNumberRequired",
        "Phone number is required.");

    public static readonly Error PasswordRequired = Error.Validation(
        "Identity.PasswordRequired",
        "Password is required.");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Identity.InvalidCredentials",
        "Phone number or password is invalid.");

    public static readonly Error NotAuthenticated = Error.Unauthorized(
        "Identity.NotAuthenticated",
        "Current user is not authenticated.");

    public static readonly Error CurrentUserNotFound = Error.NotFound(
        "Identity.CurrentUserNotFound",
        "Current user was not found.");

    public static readonly Error PhoneNumberAlreadyRegistered = Error.Conflict(
        "Identity.PhoneNumberAlreadyRegistered",
        "Phone number is already registered.");

    public static Error IdentityFailure(IEnumerable<IdentityError> errors)
    {
        string[] descriptions = errors
            .Select(error => string.IsNullOrWhiteSpace(error.Code)
                ? error.Description
                : $"{error.Code}: {error.Description}")
            .Where(description => !string.IsNullOrWhiteSpace(description))
            .ToArray();

        return Error.Validation(
            "Identity.Validation",
            descriptions.Length == 0
                ? "Identity operation failed."
                : string.Join("; ", descriptions));
    }
}
