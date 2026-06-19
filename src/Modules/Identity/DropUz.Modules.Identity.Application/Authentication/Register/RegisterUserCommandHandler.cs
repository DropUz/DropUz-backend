using DropUz.Common.Application.Authorization;
using DropUz.Common.Application.Helpers;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using DropUz.Modules.Identity.Application.Roles;
using DropUz.Modules.Identity.Domain.Roles;
using DropUz.Modules.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace DropUz.Modules.Identity.Application.Authentication.Register;

public sealed class RegisterUserCommandHandler(
    UserManager<User> userManager,
    RoleManager<AppRole> roleManager)
    : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    public async Task<Result<RegisterUserResponse>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var phoneNumber = StringNormalizer.Normalize(command.PhoneNumber);
        if (phoneNumber is null)
        {
            return Result.Failure<RegisterUserResponse>(AuthenticationErrors.PhoneNumberRequired);
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Result.Failure<RegisterUserResponse>(AuthenticationErrors.PasswordRequired);
        }

        var existingUser = await userManager.FindByNameAsync(phoneNumber);
        if (existingUser is not null)
        {
            return Result.Failure<RegisterUserResponse>(AuthenticationErrors.PhoneNumberAlreadyRegistered);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = phoneNumber,
            PhoneNumber = phoneNumber,
            FirstName = StringNormalizer.Normalize(command.FirstName) ?? command.FirstName,
            LastName = StringNormalizer.Normalize(command.LastName),
        };

        var createResult = await userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
        {
            return Result.Failure<RegisterUserResponse>(
                AuthenticationErrors.IdentityFailure(createResult.Errors));
        }

        var ensureRoleResult = await IdentityRoleStore.EnsureRoleExistsAsync(
            roleManager,
            ApplicationRoles.User);
        if (!ensureRoleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);

            return Result.Failure<RegisterUserResponse>(
                AuthenticationErrors.IdentityFailure(ensureRoleResult.Errors));
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, IdentityRoleNames.User);
        if (!addRoleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);

            return Result.Failure<RegisterUserResponse>(
                AuthenticationErrors.IdentityFailure(addRoleResult.Errors));
        }

        var response = new RegisterUserResponse(
            user.Id,
            user.PhoneNumber,
            user.FirstName,
            user.LastName);

        return Result.Success(response);
    }
}
