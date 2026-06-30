using DropUz.Common.Application.EventBus;
using DropUz.Modules.Payments.IntegrationEvents;
using DropUz.Modules.Sellers.Application.Sellers;
using DropUz.Modules.Sellers.Domain.Sellers;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Sellers;

public sealed class ProductPaymentCompletedIntegrationEventTests
{
    [Fact]
    public async Task ConsumerUsesInboxToCreatePendingSellerProfitOnce()
    {
        DateTime paidAtUtc = new(2026, 06, 28, 15, 30, 0, DateTimeKind.Utc);
        Guid orderId = Guid.NewGuid();
        SellerProfile seller = SellerProfile.Create(
            Guid.NewGuid(),
            "Drop seller",
            "drop-seller-product-payment",
            paidAtUtc.AddDays(-1));
        var repository = new InMemoryMainRepository(seller);
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new ProductPaymentCompletedIntegrationEventHandler(repository, inbox);
        var integrationEvent = new ProductPaymentCompletedIntegrationEvent(
            SourceEventId: Guid.NewGuid(),
            PaymentId: Guid.NewGuid(),
            orderId,
            UserId: Guid.NewGuid(),
            Amount: 130m,
            OrderNumber: "DUZ-PRODUCT-1",
            SellerId: seller.Id,
            SellerProfitTotal: 20m,
            paidAtUtc,
            ProviderTransactionId: "provider-product-2");

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        Assert.Equal(20m, seller.PendingBalance);
        Assert.Equal(20m, seller.TotalEarned);
        Assert.Single(seller.BalanceTransactions);
        Assert.Contains(ProductPaymentCompletedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
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
