using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Application.Orders;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Sellers.Domain.Sellers;
using DropUz.Mvp.Tests.Support;
using Xunit;
using CargoExpireCargoPaymentsCommand = DropUz.Modules.Cargo.Application.Cargo.ExpireCargoPaymentsCommand;
using CargoExpireCargoPaymentsCommandHandler = DropUz.Modules.Cargo.Application.Cargo.ExpireCargoPaymentsCommandHandler;

namespace DropUz.Mvp.Tests.Orders;

public sealed class CargoPaymentExpiredEventTests
{
    [Fact]
    public void OrderRaisesCargoPaymentExpiredEventWhenDeadlineExpires()
    {
        DateTime nowUtc = new(2026, 06, 27, 10, 0, 0, DateTimeKind.Utc);
        SellerProfile seller = SellerProfile.Create(Guid.NewGuid(), "Drop seller", "drop-seller", nowUtc.AddDays(-3));
        Order order = CreatePendingCargoPaymentOrder(Guid.NewGuid(), seller.Id, nowUtc.AddDays(-2));

        order.ExpireCargoPayment(nowUtc);

        Assert.Equal(OrderStatus.CargoPaymentExpired, order.Status);
        CargoPaymentExpiredDomainEvent domainEvent = Assert.Single(
            order.DomainEvents.OfType<CargoPaymentExpiredDomainEvent>());
        Assert.Equal(order.Id, domainEvent.OrderId);
        Assert.Equal(order.UserId, domainEvent.UserId);
        Assert.Equal(seller.Id, domainEvent.SellerId);
        Assert.Equal(order.SellerProfitTotal, domainEvent.SellerProfitTotal);
        Assert.Equal(nowUtc, domainEvent.ExpiredAtUtc);
    }

    [Fact]
    public async Task CargoExpirationCommandRaisesEventWithoutDirectSellerMutationOrNotification()
    {
        DateTime nowUtc = new(2026, 06, 27, 11, 0, 0, DateTimeKind.Utc);
        SellerProfile seller = SellerProfile.Create(Guid.NewGuid(), "Drop seller", "drop-seller", nowUtc.AddDays(-3));
        Order order = CreatePendingCargoPaymentOrder(Guid.NewGuid(), seller.Id, nowUtc.AddDays(-2));
        seller.RecordProductPayment(order.Id, order.SellerProfitTotal, nowUtc.AddDays(-1));
        var repository = new InMemoryMainRepository(order, seller);
        var handler = new CargoExpireCargoPaymentsCommandHandler(
            repository,
            new TestDateTimeProvider(nowUtc),
            new NoOpAdminAuditService());

        Result<int> result = await handler.Handle(new CargoExpireCargoPaymentsCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        Assert.Equal(OrderStatus.CargoPaymentExpired, order.Status);
        Assert.Equal(order.SellerProfitTotal, seller.PendingBalance);
        Assert.Equal(order.SellerProfitTotal, seller.TotalEarned);
        Assert.Empty(repository.Entities.OfType<NotificationMessage>());
        Assert.Single(order.DomainEvents.OfType<CargoPaymentExpiredDomainEvent>());
    }

    [Fact]
    public async Task CargoPaymentExpiredHandlerReversesSellerProfitAndCreatesNotification()
    {
        DateTime nowUtc = new(2026, 06, 27, 12, 0, 0, DateTimeKind.Utc);
        SellerProfile seller = SellerProfile.Create(Guid.NewGuid(), "Drop seller", "drop-seller", nowUtc.AddDays(-3));
        Order order = CreatePendingCargoPaymentOrder(Guid.NewGuid(), seller.Id, nowUtc.AddDays(-2));
        seller.RecordProductPayment(order.Id, order.SellerProfitTotal, nowUtc.AddDays(-1));
        order.ExpireCargoPayment(nowUtc);
        var repository = new InMemoryMainRepository(order, seller);
        var notificationService = new InMemoryNotificationService(repository, new TestDateTimeProvider(nowUtc));
        var handler = new CargoPaymentExpiredDomainEventHandler(repository, notificationService);

        await handler.Handle(
            new CargoPaymentExpiredDomainEvent(
                order.Id,
                order.UserId,
                seller.Id,
                order.SellerProfitTotal,
                nowUtc),
            CancellationToken.None);

        Assert.Equal(0m, seller.PendingBalance);
        Assert.Equal(0m, seller.TotalEarned);
        NotificationMessage notification = Assert.Single(repository.Entities.OfType<NotificationMessage>());
        Assert.Equal(order.UserId, notification.UserId);
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal(NotificationType.CargoExpired, notification.Type);
    }

    [Fact]
    public async Task CargoPaymentExpiredHandlerIsIdempotent()
    {
        DateTime nowUtc = new(2026, 06, 27, 12, 0, 0, DateTimeKind.Utc);
        SellerProfile seller = SellerProfile.Create(Guid.NewGuid(), "Drop seller", "drop-seller", nowUtc.AddDays(-3));
        Order order = CreatePendingCargoPaymentOrder(Guid.NewGuid(), seller.Id, nowUtc.AddDays(-2));
        seller.RecordProductPayment(order.Id, order.SellerProfitTotal, nowUtc.AddDays(-1));
        order.ExpireCargoPayment(nowUtc);
        var repository = new InMemoryMainRepository(order, seller);
        var notificationService = new InMemoryNotificationService(repository, new TestDateTimeProvider(nowUtc));
        var handler = new CargoPaymentExpiredDomainEventHandler(repository, notificationService);
        var domainEvent = new CargoPaymentExpiredDomainEvent(
            order.Id,
            order.UserId,
            seller.Id,
            order.SellerProfitTotal,
            nowUtc);

        await handler.Handle(domainEvent, CancellationToken.None);
        await handler.Handle(domainEvent, CancellationToken.None);

        Assert.Equal(0m, seller.PendingBalance);
        Assert.Equal(0m, seller.TotalEarned);
        Assert.Equal(2, seller.BalanceTransactions.Count);
        Assert.Single(repository.Entities.OfType<NotificationMessage>());
    }

    private static Order CreatePendingCargoPaymentOrder(Guid buyerId, Guid sellerId, DateTime createdAtUtc)
    {
        Order order = Order.Create(
            buyerId,
            sellerId,
            [
                new OrderItemSnapshot(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Bag",
                    ProductImageUrl: null,
                    VariantName: null,
                    SourcePlatform: "taobao",
                    SourceProductId: "TB-EXPIRED-1",
                    SourceUrl: null,
                    ApiPrice: 100m,
                    CurrencyRate: 1m,
                    DropUzMarkup: new Markup(MarkupType.Percent, 10m),
                    DropUzMarkupAmount: 10m,
                    DropUzFinalPrice: 110m,
                    SellerId: sellerId,
                    SellerMarkup: new Markup(MarkupType.Fixed, 20m),
                    SellerProfit: 20m,
                    FinalProductPrice: 130m,
                    CargoPrice: 0m,
                    Quantity: 1)
            ],
            createdAtUtc);

        order.MarkProductPaid(createdAtUtc.AddMinutes(10));
        order.SetCargoPrice(30m, deadlineDays: 1, createdAtUtc.AddMinutes(20));

        return order;
    }

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
    }

    private sealed class NoOpAdminAuditService : IAdminAuditService
    {
        public Task RecordAsync(
            string action,
            string entityType,
            Guid? entityId = null,
            string? details = null,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryNotificationService(
        IMainRepository repository,
        IDateTimeProvider dateTimeProvider) : INotificationService
    {
        public async Task EnqueueAsync(
            Guid userId,
            Guid? orderId,
            NotificationType type,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            await repository.AddAsync(NotificationMessage.Create(
                userId,
                orderId,
                type,
                NotificationChannel.Email,
                userId.ToString(),
                subject,
                body,
                dateTimeProvider.UtcNow));
        }
    }
}
