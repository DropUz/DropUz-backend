using DropUz.Common.Domain;
using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.Application.Payments;
using DropUz.Modules.Payments.Domain.Payments;
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
        var notificationService = new CapturingNotificationService();
        var repository = new Support.InMemoryMainRepository(order, payment);
        var handler = new ConfirmPaymentCommandHandler(
            repository,
            new TestCurrentUser(buyerId),
            new TestDateTimeProvider(nowUtc));

        await handler.Handle(new ConfirmPaymentCommand(payment.Id, "provider-tx-1"), CancellationToken.None);

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.Equal(OrderStatus.PendingProductPayment, order.Status);
        Assert.Empty(notificationService.Notifications);
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
    public async Task ProductPaymentCompletedHandlerUpdatesOrderSellerBalanceAndNotification()
    {
        Guid buyerId = Guid.NewGuid();
        DateTime paidAtUtc = new(2026, 06, 24, 10, 0, 0, DateTimeKind.Utc);
        SellerProfile seller = SellerProfile.Create(Guid.NewGuid(), "Drop seller", "drop-seller", paidAtUtc.AddDays(-1));
        Order order = CreateOrder(buyerId, seller.Id, paidAtUtc.AddHours(-1));
        var notificationService = new CapturingNotificationService();
        var repository = new Support.InMemoryMainRepository(order, seller);
        var handler = new ProductPaymentCompletedDomainEventHandler(repository, notificationService);

        await handler.Handle(
            new ProductPaymentCompletedDomainEvent(
                Guid.NewGuid(),
                order.Id,
                buyerId,
                order.ProductTotal,
                paidAtUtc,
                "provider-tx-1"),
            CancellationToken.None);

        Assert.Equal(OrderStatus.ProductPaid, order.Status);
        Assert.Equal(paidAtUtc, order.ProductPaidAtUtc);
        Assert.Equal(order.SellerProfitTotal, seller.PendingBalance);
        Assert.Equal(order.SellerProfitTotal, seller.TotalEarned);

        CapturedNotification notification = Assert.Single(notificationService.Notifications);
        Assert.Equal(buyerId, notification.UserId);
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal(NotificationType.PaymentReceived, notification.Type);
    }

    [Fact]
    public async Task ProductPaymentCompletedHandlerIsIdempotent()
    {
        Guid buyerId = Guid.NewGuid();
        DateTime paidAtUtc = new(2026, 06, 24, 10, 0, 0, DateTimeKind.Utc);
        SellerProfile seller = SellerProfile.Create(Guid.NewGuid(), "Drop seller", "drop-seller", paidAtUtc.AddDays(-1));
        Order order = CreateOrder(buyerId, seller.Id, paidAtUtc.AddHours(-1));
        var notificationService = new CapturingNotificationService();
        var repository = new Support.InMemoryMainRepository(order, seller);
        var handler = new ProductPaymentCompletedDomainEventHandler(repository, notificationService);
        var domainEvent = new ProductPaymentCompletedDomainEvent(
            Guid.NewGuid(),
            order.Id,
            buyerId,
            order.ProductTotal,
            paidAtUtc,
            "provider-tx-1");

        await handler.Handle(domainEvent, CancellationToken.None);
        await handler.Handle(domainEvent, CancellationToken.None);

        Assert.Equal(order.SellerProfitTotal, seller.PendingBalance);
        Assert.Single(seller.BalanceTransactions);
        Assert.Single(notificationService.Notifications);
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
