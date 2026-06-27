using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Application.Orders;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Orders;

public sealed class AdminOrderStatusNotificationTests
{
    [Fact]
    public async Task AdminStatusUpdateEnqueuesOrderStatusChangedNotification()
    {
        DateTime nowUtc = new(2026, 06, 23, 13, 0, 0, DateTimeKind.Utc);
        Order order = CreateProductPaidOrder(userId: Guid.NewGuid(), paidAtUtc: nowUtc);
        var repository = new InMemoryMainRepository(order);
        var handler = new AdminUpdateOrderStatusCommandHandler(
            repository,
            new TestDateTimeProvider(nowUtc),
            new NoOpAdminAuditService(),
            new InMemoryNotificationService(repository, new TestDateTimeProvider(nowUtc)));

        Result<OrderResponse> result = await handler.Handle(
            new AdminUpdateOrderStatusCommand(order.Id, OrderStatus.Purchasing, "Buying product"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        NotificationMessage notification = Assert.Single(repository.Entities.OfType<NotificationMessage>());
        Assert.Equal(order.UserId, notification.UserId);
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal(NotificationType.OrderStatusChanged, notification.Type);
    }

    private static Order CreateProductPaidOrder(Guid userId, DateTime paidAtUtc)
    {
        Order order = Order.Create(
            userId,
            sellerId: null,
            [
                new OrderItemSnapshot(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Bag",
                    ProductImageUrl: null,
                    VariantName: null,
                    SourcePlatform: "taobao",
                    SourceProductId: "TB-5",
                    SourceUrl: null,
                    ApiPrice: 50m,
                    CurrencyRate: 1m,
                    DropUzMarkup: new Markup(MarkupType.Percent, 10m),
                    DropUzMarkupAmount: 5m,
                    DropUzFinalPrice: 55m,
                    SellerId: null,
                    SellerMarkup: null,
                    SellerProfit: 0m,
                    FinalProductPrice: 55m,
                    CargoPrice: 0m,
                    Quantity: 1)
            ],
            createdAtUtc: paidAtUtc.AddMinutes(-10));

        order.MarkProductPaid(paidAtUtc);

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
