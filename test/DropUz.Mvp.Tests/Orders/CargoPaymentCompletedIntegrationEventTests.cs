using DropUz.Common.Application.EventBus;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Orders.Application.Orders;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Orders;

public sealed class CargoPaymentCompletedIntegrationEventTests
{
    [Fact]
    public async Task ConsumerUsesInboxToMarkOrderCargoPaidOnce()
    {
        Guid buyerId = Guid.NewGuid();
        DateTime paidAtUtc = new(2026, 06, 28, 17, 0, 0, DateTimeKind.Utc);
        Order order = CreateCargoPaymentOrder(buyerId, paidAtUtc.AddHours(-2));
        var repository = new InMemoryMainRepository(order);
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new CargoPaymentCompletedIntegrationEventHandler(repository, inbox);
        var integrationEvent = new CargoPaymentCompletedIntegrationEvent(
            SourceEventId: Guid.NewGuid(),
            PaymentId: Guid.NewGuid(),
            order.Id,
            buyerId,
            order.CargoTotal,
            order.OrderNumber,
            paidAtUtc,
            ProviderTransactionId: "cargo-provider-1");

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        Assert.Equal(OrderStatus.CargoPaid, order.Status);
        Assert.Equal(paidAtUtc, order.CargoPaidAtUtc);
        Assert.Single(order.StatusHistory, history => history.Status == OrderStatus.CargoPaid);
        Assert.Contains(CargoPaymentCompletedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
    }

    private static Order CreateCargoPaymentOrder(Guid buyerId, DateTime createdAtUtc)
    {
        Order order = Order.Create(
            buyerId,
            sellerId: null,
            [
                new OrderItemSnapshot(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Shoes",
                    ProductImageUrl: null,
                    VariantName: "42",
                    SourcePlatform: "taobao",
                    SourceProductId: "TB-CARGO-IE-1",
                    SourceUrl: null,
                    ApiPrice: 100m,
                    CurrencyRate: 1m,
                    DropUzMarkup: new Markup(MarkupType.Fixed, 10m),
                    DropUzMarkupAmount: 10m,
                    DropUzFinalPrice: 110m,
                    SellerId: null,
                    SellerMarkup: null,
                    SellerProfit: 0m,
                    FinalProductPrice: 110m,
                    CargoPrice: 0m,
                    Quantity: 1)
            ],
            createdAtUtc);
        order.MarkProductPaid(createdAtUtc.AddMinutes(10));
        order.SetCargoPrice(45m, 7, createdAtUtc.AddMinutes(20));
        return order;
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
