using DropUz.Common.Application.EventBus;
using DropUz.Modules.Sellers.Application.Sellers;
using DropUz.Modules.Sellers.Domain.Sellers;
using DropUz.Modules.Sellers.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Sellers;

public sealed class SellerProfitLifecycleEventTests
{
    [Fact]
    public async Task PendingProfitHandlerPublishesDeterministicIntegrationEvent()
    {
        DateTime createdAtUtc = new(2026, 06, 29, 22, 0, 0, DateTimeKind.Utc);
        var domainEvent = new SellerProfitPendingCreatedDomainEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            120m,
            createdAtUtc)
        {
            OccurredOnUtc = createdAtUtc
        };
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new SellerProfitPendingCreatedDomainEventHandler(
            new InMemoryMainRepository(),
            publisher);

        await handler.Handle(domainEvent, CancellationToken.None);

        SellerProfitPendingCreatedIntegrationEvent integrationEvent =
            Assert.IsType<SellerProfitPendingCreatedIntegrationEvent>(
                Assert.Single(publisher.PublishedEvents));
        Assert.Equal(
            IntegrationEventId.Create<SellerProfitPendingCreatedIntegrationEvent>(domainEvent.Id),
            integrationEvent.Id);
        Assert.Equal(domainEvent.Id, integrationEvent.SourceEventId);
        Assert.Equal(domainEvent.SellerId, integrationEvent.SellerId);
        Assert.Equal(domainEvent.SellerUserId, integrationEvent.SellerUserId);
        Assert.Equal(domainEvent.OrderId, integrationEvent.OrderId);
        Assert.Equal(domainEvent.Amount, integrationEvent.Amount);
        Assert.Equal(createdAtUtc, integrationEvent.CreatedAtUtc);
        Assert.Equal(createdAtUtc, integrationEvent.OccurredOnUtc);
    }

    [Fact]
    public async Task AvailableProfitHandlerPublishesDeterministicIntegrationEvent()
    {
        DateTime availableAtUtc = new(2026, 06, 29, 23, 0, 0, DateTimeKind.Utc);
        var domainEvent = new SellerProfitAvailableDomainEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            120m,
            availableAtUtc)
        {
            OccurredOnUtc = availableAtUtc
        };
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new SellerProfitAvailableDomainEventHandler(
            new InMemoryMainRepository(),
            publisher);

        await handler.Handle(domainEvent, CancellationToken.None);

        SellerProfitAvailableIntegrationEvent integrationEvent =
            Assert.IsType<SellerProfitAvailableIntegrationEvent>(
                Assert.Single(publisher.PublishedEvents));
        Assert.Equal(
            IntegrationEventId.Create<SellerProfitAvailableIntegrationEvent>(domainEvent.Id),
            integrationEvent.Id);
        Assert.Equal(domainEvent.Id, integrationEvent.SourceEventId);
        Assert.Equal(domainEvent.SellerId, integrationEvent.SellerId);
        Assert.Equal(domainEvent.SellerUserId, integrationEvent.SellerUserId);
        Assert.Equal(domainEvent.OrderId, integrationEvent.OrderId);
        Assert.Equal(domainEvent.Amount, integrationEvent.Amount);
        Assert.Equal(availableAtUtc, integrationEvent.AvailableAtUtc);
        Assert.Equal(availableAtUtc, integrationEvent.OccurredOnUtc);
    }

    private sealed class CapturingIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public List<IIntegrationEvent> PublishedEvents { get; } = [];

        public Task PublishAsync(
            IIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            PublishedEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
