using DropUz.Common.Domain;
using DropUz.Modules.Identity.Application.Authentication.Token;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DropUz.Modules.Identity.Presentation.Controllers;

[AllowAnonymous]
[ApiController]
[Route("/api/identity/oauth")]
[Produces("application/json")]
public sealed class OAuthAuthorizationController(ISender sender) : ControllerBase
{
    [HttpPost("token")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        OpenIddictRequest request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            var result = await sender.Send(
                new IssueTokenCommand(
                    request.Username,
                    request.Password,
                    [.. request.GetScopes()]),
                cancellationToken);

            return result.IsSuccess
                ? SignIn(result.Value, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
                : ToOpenIddictError(result.Error);
        }

        if (request.IsRefreshTokenGrantType())
        {
            AuthenticateResult authenticateResult = await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            string? userId = authenticateResult.Principal?.GetClaim(Claims.Subject);
            IReadOnlyCollection<string> scopes = authenticateResult.Principal?.GetScopes().ToArray() ?? [];

            var result = await sender.Send(
                new RefreshTokenCommand(userId, scopes),
                cancellationToken);

            return result.IsSuccess
                ? SignIn(result.Value, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
                : ToOpenIddictError(result.Error);
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.UnsupportedGrantType,
            ErrorDescription = "The specified grant type is not supported."
        });
    }

    private IActionResult ToOpenIddictError(Error error)
    {
        string openIddictError = error.Type switch
        {
            ErrorType.Unauthorized => Errors.InvalidGrant,
            ErrorType.NotFound => Errors.InvalidGrant,
            ErrorType.Validation => Errors.InvalidRequest,
            _ => Errors.InvalidRequest
        };

        var response = new OpenIddictResponse
        {
            Error = openIddictError,
            ErrorDescription = error.Description
        };

        return error.Type is ErrorType.Unauthorized or ErrorType.NotFound
            ? Unauthorized(response)
            : BadRequest(response);
    }
}
