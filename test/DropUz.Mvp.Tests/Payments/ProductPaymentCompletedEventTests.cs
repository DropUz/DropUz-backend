using DropUz.Common.Domain;
using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Application.Payments;
using DropUz.Modules.Payments.Domain.Payments;
using DropUz.Modules.Payments.IntegrationEvents;
using DropUz.Modules.Sellers.Domain.Sellers;
using Xunit;

namespace DropUz.Mvp.Tests.Payments;

public sealed class ProductPaymentCompletedEventTests
{
    [Fact]
    public async Task ConfirmProductPaymentRaisesEventWithoutDirectOrderMutation()
    {
        Guid buyerId = Guid.NewGuid();
        DateTime nowUtc = new(2026, 06, 24, 11, 0, 0, DateTimeKind.Utc);
        Order order = CreateOrder(buyerId, Guid.NewGuid(), nowUtc.AddHours(-1));
        Payment payment = Payment.Start(
            order.Id,
            buyerId,
            PaymentType.ProductPayment,
            PaymentMethod.Uzcard,
            order.ProductTotal,
            nowUtc.AddMinutes(-10));
        var repository = new Support.InMemoryMainRepository(order, payment);
        var handler = new ConfirmPaymentCommandHandler(
            repository,
            new TestCurrentUser(buyerId),
            new TestDateTimeProvider(nowUtc),
            new PaymentProviderRegistry([new Support.TestPaymentProvider()]));

        await handler.Handle(new ConfirmPaymentCommand(payment.Id, "provider-tx-1"), CancellationToken.None);

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(OrderStatus.PendingProductPayment, order.Status);
        Assert.Single(payment.DomainEvents.OfType<ProductPaymentCompletedDomainEvent>());
    }

    [Fact]
    public void ProductPaymentRaisesCompletedEventWhenMarkedPaid()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        DateTime paidAtUtc = new(2026, 06, 24, 9, 30, 0, DateTimeKind.Utc);
        Payment payment = Payment.Start(
            orderId,
            userId,
            PaymentType.ProductPayment,
            PaymentMethod.Uzcard,
            120m,
            paidAtUtc.AddMinutes(-5));

        payment.MarkPaid("provider-tx-1", paidAtUtc);

        ProductPaymentCompletedDomainEvent domainEvent = Assert.Single(
            payment.DomainEvents.OfType<ProductPaymentCompletedDomainEvent>());
        Assert.Equal(payment.Id, domainEvent.PaymentId);
        Assert.Equal(orderId, domainEvent.OrderId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(120m, domainEvent.Amount);
        Assert.Equal(paidAtUtc, domainEvent.PaidAtUtc);
        Assert.Equal("provider-tx-1", domainEvent.ProviderTransactionId);
    }

    [Fact]
    public async Task DomainHandlerPublishesProductPaymentCompletedIntegrationEventWithoutDirectSideEffects()
    {
        Guid buyerId = Guid.NewGuid();
        DateTime paidAtUtc = new(2026, 06, 28, 14, 0, 0, DateTimeKind.Utc);
        SellerProfile seller = SellerProfile.Create(Guid.NewGuid(), "Drop seller", "drop-seller-payment", paidAtUtc.AddDays(-1));
        Order order = CreateOrder(buyerId, seller.Id, paidAtUtc.AddHours(-1));
        var repository = new Support.InMemoryMainRepository(order, seller);
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new ProductPaymentCompletedDomainEventHandler(repository, publisher);
        var domainEvent = new ProductPaymentCompletedDomainEvent(
            Guid.NewGuid(),
            order.Id,
            buyerId,
            order.ProductTotal,
            paidAtUtc,
            "provider-tx-2");

        await handler.Handle(domainEvent, CancellationToken.None);

        ProductPaymentCompletedIntegrationEvent integrationEvent = Assert.IsType<ProductPaymentCompletedIntegrationEvent>(
            Assert.Single(publisher.PublishedEvents));
        Assert.Equal(
            IntegrationEventId.Create<ProductPaymentCompletedIntegrationEvent>(domainEvent.Id),
            integrationEvent.Id);
        Assert.Equal(domainEvent.Id, integrationEvent.SourceEventId);
        Assert.Equal(domainEvent.PaymentId, integrationEvent.PaymentId);
        Assert.Equal(order.Id, integrationEvent.OrderId);
        Assert.Equal(buyerId, integrationEvent.UserId);
        Assert.Equal(order.ProductTotal, integrationEvent.Amount);
        Assert.Equal(order.OrderNumber, integrationEvent.OrderNumber);
        Assert.Equal(seller.Id, integrationEvent.SellerId);
        Assert.Equal(order.SellerProfitTotal, integrationEvent.SellerProfitTotal);
        Assert.Equal(paidAtUtc, integrationEvent.PaidAtUtc);
        Assert.Equal("provider-tx-2", integrationEvent.ProviderTransactionId);
        Assert.Equal(OrderStatus.PendingProductPayment, order.Status);
        Assert.Equal(0m, seller.PendingBalance);
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
                    SourceProductId: "TB-5",
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

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid? UserId => userId;

        public string? UserName => "test-user";

        public bool IsAuthenticated => true;

        public IReadOnlyCollection<string> Roles => ["user"];
    }

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
    }
}
