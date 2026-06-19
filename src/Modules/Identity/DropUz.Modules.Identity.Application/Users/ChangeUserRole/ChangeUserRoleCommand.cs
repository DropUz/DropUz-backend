using DropUz.Common.Application.Messaging;

namespace DropUz.Modules.Identity.Application.Users.ChangeUserRole;

public sealed record ChangeUserRoleCommand(
    Guid UserId,
    string? Role) : ICommand<UserResponse>;
