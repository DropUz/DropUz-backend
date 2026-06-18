using System.Security.Claims;
using DropUz.Common.Application.Helpers;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using DropUz.Modules.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Identity.Application.Authentication.Token;

public sealed class IssueTokenCommandHandler(UserManager<User> userManager)
    : ICommandHandler<IssueTokenCommand, ClaimsPrincipal>
{
    public async Task<Result<ClaimsPrincipal>> Handle(
        IssueTokenCommand command,
        CancellationToken cancellationToken)
    {
        var userName = StringNormalizer.Normalize(command.UserName);
        if (userName is null)
        {
            return Result.Failure<ClaimsPrincipal>(AuthenticationErrors.PhoneNumberRequired);
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Result.Failure<ClaimsPrincipal>(AuthenticationErrors.PasswordRequired);
        }

        var user = await userManager.FindByNameAsync(userName)
            ?? await userManager.Users.FirstOrDefaultAsync(
                entity => entity.PhoneNumber == userName,
                cancellationToken);

        if (user is null)
        {
            return Result.Failure<ClaimsPrincipal>(AuthenticationErrors.InvalidCredentials);
        }

        var isPasswordValid = await userManager.CheckPasswordAsync(user, command.Password);
           

        if (!isPasswordValid)
        {
            return Result.Failure<ClaimsPrincipal>(AuthenticationErrors.InvalidCredentials);
        }

        var principal = await OpenIddictPrincipalFactory.CreateAsync(
            userManager,
            user,
            command.Scopes);

        return Result.Success(principal);
    }
}
