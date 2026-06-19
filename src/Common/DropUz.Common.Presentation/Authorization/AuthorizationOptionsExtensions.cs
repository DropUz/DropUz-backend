using DropUz.Common.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace DropUz.Common.Presentation.Authorization;

public static class AuthorizationOptionsExtensions
{
    public static void AddDropUzRolePolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(AuthorizationPolicies.User, policy =>
            policy.RequireAuthenticatedUser()
                .RequireAssertion(context => context.User.HasAnyApplicationRole(
                    ApplicationRoles.User,
                    ApplicationRoles.Seller,
                    ApplicationRoles.Admin)));

        options.AddPolicy(AuthorizationPolicies.Seller, policy =>
            policy.RequireAuthenticatedUser()
                .RequireAssertion(context => context.User.HasAnyApplicationRole(
                    ApplicationRoles.Seller,
                    ApplicationRoles.Admin)));

        options.AddPolicy(AuthorizationPolicies.Admin, policy =>
            policy.RequireAuthenticatedUser()
                .RequireAssertion(context => context.User.HasAnyApplicationRole(ApplicationRoles.Admin)));
    }
}
