using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Application.Payments;
using DropUz.Modules.Payments.Domain.Payments;
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
        var notificationService = new CapturingNotificationService();
        var repository = new Support.InMemoryMainRepository(order, payment);
        var handler = new ConfirmPaymentCommandHandler(
            repository,
            new TestCurrentUser(buyerId),
            new TestDateTimeProvider(nowUtc));

        await handler.Handle(new ConfirmPaymentCommand(payment.Id, "cargo-provider-tx-1"), CancellationToken.None);

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(OrderStatus.PendingCargoPayment, order.Status);
        Assert.Empty(notificationService.Notifications);
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
    public async Task CargoPaymentCompletedHandlerUpdatesOrderAndNotification()
    {
        Guid buyerId = Guid.NewGuid();
        DateTime paidAtUtc = new(2026, 06, 25, 10, 0, 0, DateTimeKind.Utc);
        Order order = CreateCargoPaymentOrder(buyerId, paidAtUtc.AddHours(-2));
        var notificationService = new CapturingNotificationService();
        var repository = new Support.InMemoryMainRepository(order);
        var handler = new CargoPaymentCompletedDomainEventHandler(repository, notificationService);

        await handler.Handle(
            new CargoPaymentCompletedDomainEvent(
                Guid.NewGuid(),
                order.Id,
                buyerId,
                order.CargoTotal,
                paidAtUtc,
                "cargo-provider-tx-1"),
            CancellationToken.None);

        Assert.Equal(OrderStatus.CargoPaid, order.Status);
        Assert.Equal(paidAtUtc, order.CargoPaidAtUtc);

        CapturedNotification notification = Assert.Single(notificationService.Notifications);
        Assert.Equal(buyerId, notification.UserId);
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal(NotificationType.PaymentReceived, notification.Type);
    }

    [Fact]
    public async Task CargoPaymentCompletedHandlerIsIdempotent()
    {
        Guid buyerId = Guid.NewGuid();
        DateTime paidAtUtc = new(2026, 06, 25, 10, 0, 0, DateTimeKind.Utc);
        Order order = CreateCargoPaymentOrder(buyerId, paidAtUtc.AddHours(-2));
        var notificationService = new CapturingNotificationService();
        var repository = new Support.InMemoryMainRepository(order);
        var handler = new CargoPaymentCompletedDomainEventHandler(repository, notificationService);
        var domainEvent = new CargoPaymentCompletedDomainEvent(
            Guid.NewGuid(),
            order.Id,
            buyerId,
            order.CargoTotal,
            paidAtUtc,
            "cargo-provider-tx-1");

        await handler.Handle(domainEvent, CancellationToken.None);
        await handler.Handle(domainEvent, CancellationToken.None);

        Assert.Equal(OrderStatus.CargoPaid, order.Status);
        Assert.Single(notificationService.Notifications);
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

    private sealed class CapturingNotificationService : INotificationService
    {
        public List<CapturedNotification> Notifications { get; } = [];

        public Task EnqueueAsync(
            Guid userId,
            Guid? orderId,
            NotificationType type,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            Notifications.Add(new CapturedNotification(userId, orderId, type, subject, body));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedNotification(
        Guid UserId,
        Guid? OrderId,
        NotificationType Type,
        string Subject,
        string Body);

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
