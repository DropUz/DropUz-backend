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

        bool firstStart = await service.TryStartProcessingAsync(integrationEvent, "test-consumer", CancellationToken.None);
        await service.MarkProcessedAsync(integrationEvent, "test-consumer", CancellationToken.None);
        bool secondStart = await service.TryStartProcessingAsync(integrationEvent, "test-consumer", CancellationToken.None);

        InboxMessage message = Assert.Single(context.Set<InboxMessage>());
        Assert.True(firstStart);
        Assert.False(secondStart);
        Assert.Equal("test-consumer", message.ConsumerName);
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.Null(message.Error);
    }

    [Fact]
    public async Task TryStartProcessingReturnsTrueForFailedIntegrationEventRetry()
    {
        await using MainDbContext context = CreateContext();
        var service = new InboxMessageService(
            context,
            new TestDateTimeProvider(new DateTime(2026, 06, 27, 19, 0, 0, DateTimeKind.Utc)));
        var integrationEvent = new TestIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, OrderId: Guid.NewGuid());

        bool firstStart = await service.TryStartProcessingAsync(integrationEvent, "test-consumer", CancellationToken.None);
        await service.MarkFailedAsync(integrationEvent, "test-consumer", "temporary failure", CancellationToken.None);
        bool retryStart = await service.TryStartProcessingAsync(integrationEvent, "test-consumer", CancellationToken.None);

        InboxMessage message = Assert.Single(context.Set<InboxMessage>());
        Assert.True(firstStart);
        Assert.True(retryStart);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(1, message.RetryCount);
        Assert.Equal("temporary failure", message.Error);
    }

    [Fact]
    public async Task MarkFailedDoesNotPersistPendingConsumerChanges()
    {
        await using MainDbContext context = CreateContext();
        var service = new InboxMessageService(
            context,
            new TestDateTimeProvider(new DateTime(2026, 06, 27, 20, 0, 0, DateTimeKind.Utc)));
        var integrationEvent = new TestIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, OrderId: Guid.NewGuid());
        await service.TryStartProcessingAsync(integrationEvent, "test-consumer", CancellationToken.None);
        context.Set<OutboxTestEntity>().Add(new OutboxTestEntity(Guid.NewGuid()));

        await service.MarkFailedAsync(integrationEvent, "test-consumer", "consumer failed", CancellationToken.None);

        context.ChangeTracker.Clear();
        Assert.Empty(await context.Set<OutboxTestEntity>().ToListAsync());
        InboxMessage message = Assert.Single(await context.Set<InboxMessage>().ToListAsync());
        Assert.Equal("consumer failed", message.Error);
        Assert.Equal(1, message.RetryCount);
    }

    [Fact]
    public async Task SameIntegrationEventCanBeProcessedByDifferentConsumers()
    {
        await using MainDbContext context = CreateContext();
        var service = new InboxMessageService(
            context,
            new TestDateTimeProvider(new DateTime(2026, 06, 27, 21, 30, 0, DateTimeKind.Utc)));
        var integrationEvent = new TestIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, OrderId: Guid.NewGuid());

        bool notificationsStart = await service.TryStartProcessingAsync(
            integrationEvent,
            "notifications.cargo-price-added",
            CancellationToken.None);
        await service.MarkProcessedAsync(
            integrationEvent,
            "notifications.cargo-price-added",
            CancellationToken.None);
        bool analyticsStart = await service.TryStartProcessingAsync(
            integrationEvent,
            "analytics.cargo-price-added",
            CancellationToken.None);
        await service.MarkProcessedAsync(
            integrationEvent,
            "analytics.cargo-price-added",
            CancellationToken.None);

        Assert.True(notificationsStart);
        Assert.True(analyticsStart);
        Assert.Equal(2, await context.Set<InboxMessage>().CountAsync());
        Assert.Equal(
            ["analytics.cargo-price-added", "notifications.cargo-price-added"],
            await context.Set<InboxMessage>()
                .OrderBy(message => message.ConsumerName)
                .Select(message => message.ConsumerName)
                .ToArrayAsync());
    }

    [Fact]
    public async Task ConsumerNameIsNormalizedForDeduplication()
    {
        await using MainDbContext context = CreateContext();
        var service = new InboxMessageService(
            context,
            new TestDateTimeProvider(new DateTime(2026, 06, 27, 22, 0, 0, DateTimeKind.Utc)));
        var integrationEvent = new TestIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, OrderId: Guid.NewGuid());

        bool firstStart = await service.TryStartProcessingAsync(
            integrationEvent,
            "  notifications.cargo-price-added  ",
            CancellationToken.None);
        await service.MarkProcessedAsync(
            integrationEvent,
            "notifications.cargo-price-added",
            CancellationToken.None);
        bool duplicateStart = await service.TryStartProcessingAsync(
            integrationEvent,
            " notifications.cargo-price-added ",
            CancellationToken.None);

        Assert.True(firstStart);
        Assert.False(duplicateStart);
        InboxMessage message = Assert.Single(await context.Set<InboxMessage>().ToListAsync());
        Assert.Equal("notifications.cargo-price-added", message.ConsumerName);
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
