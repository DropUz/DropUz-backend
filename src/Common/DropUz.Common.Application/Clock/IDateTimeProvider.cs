namespace DropUz.Common.Application.Clock;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateTimeOffset OffsetUtcNow { get; }
}
