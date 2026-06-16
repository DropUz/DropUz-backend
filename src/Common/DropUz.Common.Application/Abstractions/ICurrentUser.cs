namespace DropUz.Common.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }

    string? UserName { get; }

    bool IsAuthenticated { get; }

    IReadOnlyCollection<string> Roles { get; }
}
