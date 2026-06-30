using DropUz.Common.Application.EventBus;
using DropUz.Modules.Orders.IntegrationEvents;
using DropUz.Modules.Sellers.Application.Sellers;
using DropUz.Modules.Sellers.Domain.Sellers;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Sellers;

public sealed class OrderDeliveredIntegrationEventTests
{
    [Fact]
    public async Task ConsumerUsesInboxToReleaseSellerProfitOnce()
    {
        DateTime deliveredAtUtc = new(2026, 06, 28, 18, 0, 0, DateTimeKind.Utc);
        Guid orderId = Guid.NewGuid();
        SellerProfile seller = SellerProfile.Create(
            Guid.NewGuid(),
            "Drop seller",
            "drop-seller-delivered",
            deliveredAtUtc.AddDays(-2));
        seller.RecordProductPayment(orderId, 20m, deliveredAtUtc.AddHours(-2));
        var repository = new InMemoryMainRepository(seller);
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new OrderDeliveredIntegrationEventHandler(repository, inbox);
        var integrationEvent = new OrderDeliveredIntegrationEvent(
            SourceEventId: Guid.NewGuid(),
            orderId,
            UserId: Guid.NewGuid(),
            SellerId: seller.Id,
            SellerProfitTotal: 20m,
            deliveredAtUtc);

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        Assert.Equal(0m, seller.PendingBalance);
        Assert.Equal(20m, seller.AvailableBalance);
        Assert.Equal(20m, seller.TotalEarned);
        Assert.Equal(2, seller.BalanceTransactions.Count);
        Assert.Contains(OrderDeliveredIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
    }

    private sealed class InMemoryIntegrationEventInbox : IIntegrationEventInbox
    {
        private readonly HashSet<(Guid MessageId, string ConsumerName)> _processed = [];

        public HashSet<string> StartedConsumerNames { get; } = [];

        public Task<bool> TryStartProcessingAsync(
            IIntegrationEvent integrationEvent,
            string consumerName,
            CancellationToken cancellationToken = default)
        {
            StartedConsumerNames.Add(consumerName);
            return Task.FromResult(!_processed.Contains((integrationEvent.Id, consumerName)));
        }

        public Task MarkProcessedAsync(
            IIntegrationEvent integrationEvent,
            string consumerName,
            CancellationToken cancellationToken = default)
        {
            _processed.Add((integrationEvent.Id, consumerName));
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(
            IIntegrationEvent integrationEvent,
            string consumerName,
            string error,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
