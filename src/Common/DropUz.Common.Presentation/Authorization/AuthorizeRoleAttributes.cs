using Microsoft.AspNetCore.Authorization;

namespace DropUz.Common.Presentation.Authorization;

public sealed class UserAuthorizeAttribute : AuthorizeAttribute
{
    public UserAuthorizeAttribute()
    {
        Policy = AuthorizationPolicies.User;
    }
}

public sealed class SellerAuthorizeAttribute : AuthorizeAttribute
{
    public SellerAuthorizeAttribute()
    {
        Policy = AuthorizationPolicies.Seller;
    }
}

public sealed class AdminAuthorizeAttribute : AuthorizeAttribute
{
    public AdminAuthorizeAttribute()
    {
        Policy = AuthorizationPolicies.Admin;
    }
}
