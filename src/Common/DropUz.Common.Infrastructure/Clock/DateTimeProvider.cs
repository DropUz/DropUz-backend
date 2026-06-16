using DropUz.Common.Application.Clock;

namespace DropUz.Common.Infrastructure.Clock;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateTimeOffset OffsetUtcNow => DateTimeOffset.UtcNow;
}
