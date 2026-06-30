using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Application.Orders;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Orders;

public sealed class AdminOrderStatusNotificationTests
{
    [Fact]
    public async Task AdminStatusUpdateRaisesEventWithoutDirectNotification()
    {
        DateTime nowUtc = new(2026, 06, 23, 13, 0, 0, DateTimeKind.Utc);
        Guid adminUserId = Guid.NewGuid();
        Order order = CreateProductPaidOrder(userId: Guid.NewGuid(), paidAtUtc: nowUtc);
        var repository = new InMemoryMainRepository(order);
        var handler = new AdminUpdateOrderStatusCommandHandler(
            repository,
            new TestDateTimeProvider(nowUtc),
            new TestCurrentUser(adminUserId));

        Result<OrderResponse> result = await handler.Handle(
            new AdminUpdateOrderStatusCommand(order.Id, OrderStatus.Purchasing, "Buying product"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.Entities.OfType<NotificationMessage>());
        OrderStatusChangedDomainEvent domainEvent = Assert.Single(
            order.DomainEvents.OfType<OrderStatusChangedDomainEvent>());
        Assert.Equal(adminUserId, domainEvent.ChangedByUserId);
        Assert.Equal(OrderStatus.ProductPaid, domainEvent.PreviousStatus);
        Assert.Equal(OrderStatus.Purchasing, domainEvent.NewStatus);
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

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public string? UserName => "admin";

        public bool IsAuthenticated => true;

        public IReadOnlyCollection<string> Roles => ["admin"];
    }
}
