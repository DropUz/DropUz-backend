using System.Security.Claims;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using DropUz.Modules.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace DropUz.Modules.Identity.Application.Authentication.Token;

public sealed class RefreshTokenCommandHandler(UserManager<User> userManager)
    : ICommandHandler<RefreshTokenCommand, ClaimsPrincipal>
{
    public async Task<Result<ClaimsPrincipal>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(command.UserId, out Guid userId))
        {
            return Result.Failure<ClaimsPrincipal>(AuthenticationErrors.NotAuthenticated);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<ClaimsPrincipal>(AuthenticationErrors.CurrentUserNotFound);
        }

        var principal = await OpenIddictPrincipalFactory.CreateAsync(
            userManager,
            user,
            command.Scopes);

        return Result.Success(principal);
    }
}
