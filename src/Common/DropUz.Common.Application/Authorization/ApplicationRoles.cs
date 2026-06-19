namespace DropUz.Common.Application.Authorization;

public static class ApplicationRoles
{
    public const string User = "user";

    public const string Seller = "seller";

    public const string Admin = "admin";

    public static readonly IReadOnlyCollection<string> All = [User, Seller, Admin];
}
