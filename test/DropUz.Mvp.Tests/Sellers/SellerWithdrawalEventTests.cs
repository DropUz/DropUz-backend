using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Domain;
using DropUz.Modules.Sellers.Application.Sellers;
using DropUz.Modules.Sellers.Domain.Sellers;
using DropUz.Modules.Sellers.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Sellers;

public sealed class SellerWithdrawalEventTests
{
    [Fact]
    public async Task DomainHandlerPublishesDeterministicWithdrawalIntegrationEvent()
    {
        DateTime recordedAtUtc = new(2026, 06, 29, 17, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var domainEvent = new SellerWithdrawalRecordedDomainEvent(
            Guid.NewGuid(),
            80m,
            "Manual payout",
            actorUserId,
            recordedAtUtc)
        {
            OccurredOnUtc = recordedAtUtc
        };
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new SellerWithdrawalRecordedDomainEventHandler(
            new InMemoryMainRepository(),
            publisher);

        await handler.Handle(domainEvent, CancellationToken.None);

        SellerWithdrawalRecordedIntegrationEvent integrationEvent =
            Assert.IsType<SellerWithdrawalRecordedIntegrationEvent>(
                Assert.Single(publisher.PublishedEvents));
        Assert.Equal(
            IntegrationEventId.Create<SellerWithdrawalRecordedIntegrationEvent>(domainEvent.Id),
            integrationEvent.Id);
        Assert.Equal(domainEvent.Id, integrationEvent.SourceEventId);
        Assert.Equal(domainEvent.SellerId, integrationEvent.SellerId);
        Assert.Equal(80m, integrationEvent.Amount);
        Assert.Equal("Manual payout", integrationEvent.Note);
        Assert.Equal(actorUserId, integrationEvent.ActorUserId);
        Assert.Equal(recordedAtUtc, integrationEvent.RecordedAtUtc);
        Assert.Equal(recordedAtUtc, integrationEvent.OccurredOnUtc);
    }

    [Fact]
    public async Task WithdrawalCommandSnapshotsActorWithoutDirectAuditDependency()
    {
        DateTime nowUtc = new(2026, 06, 29, 18, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var seller = SellerProfile.Create(Guid.NewGuid(), "Ali Shop", "ali-shop", nowUtc.AddDays(-1));
        Guid orderId = Guid.NewGuid();
        seller.RecordProductPayment(orderId, 120m, nowUtc.AddHours(-2));
        seller.ReleaseDeliveredProfit(orderId, 120m, nowUtc.AddHours(-1));
        seller.ClearDomainEvents();
        var handler = new RecordSellerWithdrawalCommandHandler(
            new InMemoryMainRepository(seller),
            new TestDateTimeProvider(nowUtc),
            new TestCurrentUser(actorUserId));

        Result<SellerBalanceResponse> result = await handler.Handle(
            new RecordSellerWithdrawalCommand(seller.Id, 80m, "Manual payout"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        SellerWithdrawalRecordedDomainEvent domainEvent = Assert.Single(
            seller.DomainEvents.OfType<SellerWithdrawalRecordedDomainEvent>());
        Assert.Equal(actorUserId, domainEvent.ActorUserId);
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

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public string? UserName => "admin";

        public bool IsAuthenticated => true;

        public IReadOnlyCollection<string> Roles => ["admin"];
    }
}
