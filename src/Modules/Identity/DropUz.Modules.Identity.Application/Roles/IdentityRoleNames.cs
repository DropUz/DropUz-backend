using DropUz.Common.Application.Authorization;

namespace DropUz.Modules.Identity.Application.Roles;

public static class IdentityRoleNames
{
    public static string User => RoleNameNormalizer.ToStorageRoleName(ApplicationRoles.User);

    public static string Seller => RoleNameNormalizer.ToStorageRoleName(ApplicationRoles.Seller);

    public static string Admin => RoleNameNormalizer.ToStorageRoleName(ApplicationRoles.Admin);

    public static IReadOnlyCollection<string> All => [User, Seller, Admin];
}
