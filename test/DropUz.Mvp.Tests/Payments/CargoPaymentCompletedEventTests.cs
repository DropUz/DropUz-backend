using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Application.Payments;
using DropUz.Modules.Payments.Domain.Payments;
using DropUz.Modules.Payments.IntegrationEvents;
using Xunit;

namespace DropUz.Mvp.Tests.Payments;

public sealed class CargoPaymentCompletedEventTests
{
    [Fact]
    public async Task ConfirmCargoPaymentRaisesEventWithoutDirectOrderMutation()
    {
        Guid buyerId = Guid.NewGuid();
        DateTime nowUtc = new(2026, 06, 25, 11, 0, 0, DateTimeKind.Utc);
        Order order = CreateCargoPaymentOrder(buyerId, nowUtc.AddHours(-2));
        Payment payment = Payment.Start(
            order.Id,
            buyerId,
            PaymentType.CargoPayment,
            PaymentMethod.Humo,
            order.CargoTotal,
            nowUtc.AddMinutes(-10));
        var repository = new Support.InMemoryMainRepository(order, payment);
        var handler = new ConfirmPaymentCommandHandler(
            repository,
            new TestCurrentUser(buyerId),
            new TestDateTimeProvider(nowUtc),
            new PaymentProviderRegistry([new Support.TestPaymentProvider()]));

        await handler.Handle(new ConfirmPaymentCommand(payment.Id, "cargo-provider-tx-1"), CancellationToken.None);

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(OrderStatus.PendingCargoPayment, order.Status);
        Assert.Single(payment.DomainEvents.OfType<CargoPaymentCompletedDomainEvent>());
    }

    [Fact]
    public void CargoPaymentRaisesCompletedEventWhenMarkedPaid()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        DateTime paidAtUtc = new(2026, 06, 25, 9, 30, 0, DateTimeKind.Utc);
        Payment payment = Payment.Start(
            orderId,
            userId,
            PaymentType.CargoPayment,
            PaymentMethod.Humo,
            45m,
            paidAtUtc.AddMinutes(-5));

        payment.MarkPaid("cargo-provider-tx-1", paidAtUtc);

        CargoPaymentCompletedDomainEvent domainEvent = Assert.Single(
            payment.DomainEvents.OfType<CargoPaymentCompletedDomainEvent>());
        Assert.Equal(payment.Id, domainEvent.PaymentId);
        Assert.Equal(orderId, domainEvent.OrderId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(45m, domainEvent.Amount);
        Assert.Equal(paidAtUtc, domainEvent.PaidAtUtc);
        Assert.Equal("cargo-provider-tx-1", domainEvent.ProviderTransactionId);
    }

    [Fact]
    public async Task DomainHandlerPublishesCargoPaymentCompletedIntegrationEventWithoutDirectSideEffects()
    {
        Guid buyerId = Guid.NewGuid();
        DateTime paidAtUtc = new(2026, 06, 25, 10, 0, 0, DateTimeKind.Utc);
        Order order = CreateCargoPaymentOrder(buyerId, paidAtUtc.AddHours(-2));
        var repository = new Support.InMemoryMainRepository(order);
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new CargoPaymentCompletedDomainEventHandler(repository, publisher);
        var domainEvent = new CargoPaymentCompletedDomainEvent(
            Guid.NewGuid(),
            order.Id,
            buyerId,
            order.CargoTotal,
            paidAtUtc,
            "cargo-provider-tx-1");

        await handler.Handle(domainEvent, CancellationToken.None);

        CargoPaymentCompletedIntegrationEvent integrationEvent = Assert.IsType<CargoPaymentCompletedIntegrationEvent>(
            Assert.Single(publisher.PublishedEvents));
        Assert.Equal(
            IntegrationEventId.Create<CargoPaymentCompletedIntegrationEvent>(domainEvent.Id),
            integrationEvent.Id);
        Assert.Equal(domainEvent.Id, integrationEvent.SourceEventId);
        Assert.Equal(domainEvent.PaymentId, integrationEvent.PaymentId);
        Assert.Equal(order.Id, integrationEvent.OrderId);
        Assert.Equal(buyerId, integrationEvent.UserId);
        Assert.Equal(order.CargoTotal, integrationEvent.Amount);
        Assert.Equal(order.OrderNumber, integrationEvent.OrderNumber);
        Assert.Equal(paidAtUtc, integrationEvent.PaidAtUtc);
        Assert.Equal("cargo-provider-tx-1", integrationEvent.ProviderTransactionId);
        Assert.Equal(OrderStatus.PendingCargoPayment, order.Status);
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
                    SourceProductId: "TB-CARGO-1",
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
        order.SetCargoPrice(45m, deadlineDays: 7, createdAtUtc.AddMinutes(20));

        return order;
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
