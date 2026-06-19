using DropUz.Common.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace DropUz.Common.Presentation.Authorization;

public static class EndpointAuthorizationExtensions
{
    public static TBuilder RequireApplicationRole<TBuilder>(
        this TBuilder builder,
        params string[] roles)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy =>
            policy.RequireAuthenticatedUser()
                .RequireAssertion(context => context.User.HasAnyApplicationRole(roles)));
    }

    public static TBuilder RequireUser<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(AuthorizationPolicies.User);
    }

    public static TBuilder RequireSeller<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(AuthorizationPolicies.Seller);
    }

    public static TBuilder RequireAdmin<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(AuthorizationPolicies.Admin);
    }
}
