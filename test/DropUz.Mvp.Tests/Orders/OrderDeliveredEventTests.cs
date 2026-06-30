using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Orders.Application.Orders;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Orders.IntegrationEvents;
using DropUz.Modules.Sellers.Domain.Sellers;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Orders;

public sealed class OrderDeliveredEventTests
{
    [Fact]
    public void OrderRaisesDeliveredEventWhenStatusChangesToDelivered()
    {
        DateTime deliveredAtUtc = new(2026, 06, 26, 12, 0, 0, DateTimeKind.Utc);
        SellerProfile seller = SellerProfile.Create(Guid.NewGuid(), "Drop seller", "drop-seller", deliveredAtUtc.AddDays(-2));
        Order order = CreateCargoPaidSellerOrder(Guid.NewGuid(), seller.Id, deliveredAtUtc.AddDays(-1));

        bool changed = order.UpdateStatus(OrderStatus.Delivered, "Delivered to user", deliveredAtUtc);

        Assert.True(changed);
        OrderDeliveredDomainEvent domainEvent = Assert.Single(order.DomainEvents.OfType<OrderDeliveredDomainEvent>());
        Assert.Equal(order.Id, domainEvent.OrderId);
        Assert.Equal(order.UserId, domainEvent.UserId);
        Assert.Equal(seller.Id, domainEvent.SellerId);
        Assert.Equal(order.SellerProfitTotal, domainEvent.SellerProfitTotal);
        Assert.Equal(deliveredAtUtc, domainEvent.DeliveredAtUtc);
    }

    [Fact]
    public async Task AdminDeliveredStatusRaisesEventWithoutDirectSellerBalanceMutation()
    {
        DateTime nowUtc = new(2026, 06, 26, 13, 0, 0, DateTimeKind.Utc);
        SellerProfile seller = SellerProfile.Create(Guid.NewGuid(), "Drop seller", "drop-seller", nowUtc.AddDays(-2));
        Order order = CreateCargoPaidSellerOrder(Guid.NewGuid(), seller.Id, nowUtc.AddDays(-1));
        seller.RecordProductPayment(order.Id, order.SellerProfitTotal, nowUtc.AddHours(-1));
        var repository = new InMemoryMainRepository(order, seller);
        var handler = new AdminUpdateOrderStatusCommandHandler(
            repository,
            new TestDateTimeProvider(nowUtc),
            new TestCurrentUser(Guid.NewGuid()));

        Result<OrderResponse> result = await handler.Handle(
            new AdminUpdateOrderStatusCommand(order.Id, OrderStatus.Delivered, "Delivered"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(order.SellerProfitTotal, seller.PendingBalance);
        Assert.Equal(0m, seller.AvailableBalance);
        Assert.Single(order.DomainEvents.OfType<OrderDeliveredDomainEvent>());
    }

    [Fact]
    public async Task DomainHandlerPublishesOrderDeliveredIntegrationEventWithoutDirectSellerMutation()
    {
        DateTime deliveredAtUtc = new(2026, 06, 26, 14, 0, 0, DateTimeKind.Utc);
        SellerProfile seller = SellerProfile.Create(Guid.NewGuid(), "Drop seller", "drop-seller", deliveredAtUtc.AddDays(-2));
        Order order = CreateCargoPaidSellerOrder(Guid.NewGuid(), seller.Id, deliveredAtUtc.AddDays(-1));
        seller.RecordProductPayment(order.Id, order.SellerProfitTotal, deliveredAtUtc.AddHours(-2));
        var repository = new InMemoryMainRepository(order, seller);
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new OrderDeliveredDomainEventHandler(repository, publisher);
        var domainEvent = new OrderDeliveredDomainEvent(
            order.Id,
            order.UserId,
            seller.Id,
            order.SellerProfitTotal,
            deliveredAtUtc);

        await handler.Handle(domainEvent, CancellationToken.None);

        OrderDeliveredIntegrationEvent integrationEvent = Assert.IsType<OrderDeliveredIntegrationEvent>(
            Assert.Single(publisher.PublishedEvents));
        Assert.Equal(
            IntegrationEventId.Create<OrderDeliveredIntegrationEvent>(domainEvent.Id),
            integrationEvent.Id);
        Assert.Equal(domainEvent.Id, integrationEvent.SourceEventId);
        Assert.Equal(order.Id, integrationEvent.OrderId);
        Assert.Equal(order.UserId, integrationEvent.UserId);
        Assert.Equal(seller.Id, integrationEvent.SellerId);
        Assert.Equal(order.SellerProfitTotal, integrationEvent.SellerProfitTotal);
        Assert.Equal(deliveredAtUtc, integrationEvent.DeliveredAtUtc);
        Assert.Equal(order.SellerProfitTotal, seller.PendingBalance);
        Assert.Equal(0m, seller.AvailableBalance);
        Assert.Equal(order.SellerProfitTotal, seller.TotalEarned);
    }

    private static Order CreateCargoPaidSellerOrder(Guid buyerId, Guid sellerId, DateTime createdAtUtc)
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
                    SourceProductId: "TB-DELIVERED-1",
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
        order.SetCargoPrice(30m, deadlineDays: 7, createdAtUtc.AddMinutes(20));
        order.MarkCargoPaid(createdAtUtc.AddMinutes(30));

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
}
