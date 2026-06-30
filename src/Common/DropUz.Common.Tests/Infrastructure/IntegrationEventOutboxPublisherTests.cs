using DropUz.Common.Application.EventBus;
using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DropUz.Common.Tests.Infrastructure;

public sealed class IntegrationEventOutboxPublisherTests
{
    [Fact]
    public async Task PublishAddsIntegrationEventToCurrentOutboxUnitOfWork()
    {
        await using MainDbContext context = CreateContext();
        var publisher = new OutboxIntegrationEventPublisher(context);
        var integrationEvent = new TestIntegrationEvent(Guid.NewGuid());

        await publisher.PublishAsync(integrationEvent, CancellationToken.None);

        OutboxMessage message = Assert.Single(context.OutboxMessages.Local);
        Assert.Equal(integrationEvent.Id, message.Id);
        Assert.Equal(typeof(TestIntegrationEvent).FullName, message.Type);
        Assert.Equal(EntityState.Added, context.Entry(message).State);
    }

    [Fact]
    public async Task PublishIsIdempotentForSameIntegrationEvent()
    {
        await using MainDbContext context = CreateContext();
        var publisher = new OutboxIntegrationEventPublisher(context);
        var integrationEvent = new TestIntegrationEvent(Guid.NewGuid());

        await publisher.PublishAsync(integrationEvent, CancellationToken.None);
        await publisher.PublishAsync(integrationEvent, CancellationToken.None);

        Assert.Single(context.OutboxMessages.Local);
    }

    private static MainDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MainDbContext(options, [new CommonTestModelContributor()]);
    }

    private sealed record TestIntegrationEvent(Guid EntityId) : IntegrationEvent;
}
