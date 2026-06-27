using DropUz.Common.Application.EventBus;
using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DropUz.Common.Tests.Infrastructure;

public sealed class InboxMessageServiceTests
{
    [Fact]
    public async Task TryStartProcessingReturnsFalseForAlreadyProcessedIntegrationEvent()
    {
        await using MainDbContext context = CreateContext();
        var service = new InboxMessageService(
            context,
            new TestDateTimeProvider(new DateTime(2026, 06, 26, 10, 0, 0, DateTimeKind.Utc)));
        var integrationEvent = new TestIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, OrderId: Guid.NewGuid());

        bool firstStart = await service.TryStartProcessingAsync(integrationEvent, CancellationToken.None);
        await service.MarkProcessedAsync(integrationEvent, CancellationToken.None);
        bool secondStart = await service.TryStartProcessingAsync(integrationEvent, CancellationToken.None);

        InboxMessage message = Assert.Single(context.Set<InboxMessage>());
        Assert.True(firstStart);
        Assert.False(secondStart);
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.Null(message.Error);
    }

    private static MainDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MainDbContext(options, [new CommonTestModelContributor()]);
    }

    private sealed record TestIntegrationEvent(
        Guid Id,
        DateTime OccurredOnUtc,
        Guid OrderId) : IIntegrationEvent;
}
