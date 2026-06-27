using DropUz.Common.Application.Clock;

namespace DropUz.Common.Tests.Infrastructure;

internal sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow => utcNow;

    public DateTimeOffset OffsetUtcNow => new(utcNow);
}
