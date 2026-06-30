using DropUz.Common.Application.EventBus;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Orders.Application.Orders;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Orders;

public sealed class ProductPaymentCompletedIntegrationEventTests
{
    [Fact]
    public async Task ConsumerUsesInboxToMarkOrderProductPaidOnce()
    {
        Guid buyerId = Guid.NewGuid();
        DateTime paidAtUtc = new(2026, 06, 28, 15, 0, 0, DateTimeKind.Utc);
        Order order = CreateOrder(buyerId, Guid.NewGuid(), paidAtUtc.AddHours(-1));
        var repository = new InMemoryMainRepository(order);
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new ProductPaymentCompletedIntegrationEventHandler(repository, inbox);
        ProductPaymentCompletedIntegrationEvent integrationEvent = CreateEvent(order, buyerId, paidAtUtc);

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        Assert.Equal(OrderStatus.ProductPaid, order.Status);
        Assert.Equal(paidAtUtc, order.ProductPaidAtUtc);
        Assert.Single(order.StatusHistory, history => history.Status == OrderStatus.ProductPaid);
        Assert.Contains(ProductPaymentCompletedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
    }

    private static ProductPaymentCompletedIntegrationEvent CreateEvent(
        Order order,
        Guid buyerId,
        DateTime paidAtUtc)
    {
        return new ProductPaymentCompletedIntegrationEvent(
            SourceEventId: Guid.NewGuid(),
            PaymentId: Guid.NewGuid(),
            order.Id,
            buyerId,
            order.ProductTotal,
            order.OrderNumber,
            order.SellerId,
            order.SellerProfitTotal,
            paidAtUtc,
            ProviderTransactionId: "provider-product-1");
    }

    private static Order CreateOrder(Guid buyerId, Guid sellerId, DateTime createdAtUtc)
    {
        return Order.Create(
            buyerId,
            sellerId,
            [
                new OrderItemSnapshot(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Bag",
                    ProductImageUrl: null,
                    VariantName: null,
                    SourcePlatform: "taobao",
                    SourceProductId: "TB-PP-1",
                    SourceUrl: null,
                    ApiPrice: 100m,
                    CurrencyRate: 1m,
                    DropUzMarkup: new Markup(MarkupType.Percent, 10m),
                    DropUzMarkupAmount: 10m,
                    DropUzFinalPrice: 110m,
                    SellerId: sellerId,
                    SellerMarkup: new Markup(MarkupType.Percent, 20m),
                    SellerProfit: 20m,
                    FinalProductPrice: 130m,
                    CargoPrice: 0m,
                    Quantity: 1)
            ],
            createdAtUtc);
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
