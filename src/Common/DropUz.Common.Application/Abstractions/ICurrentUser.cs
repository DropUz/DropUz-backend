namespace DropUz.Common.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Subject { get; }

    string? Email { get; }

    string? UserName { get; }

    IReadOnlySet<string> Roles { get; }

    IReadOnlySet<string> Permissions { get; }
}
