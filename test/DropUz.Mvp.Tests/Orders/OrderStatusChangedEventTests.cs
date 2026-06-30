using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Orders.Application.Orders;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Orders.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Orders;

public sealed class OrderStatusChangedEventTests
{
    [Fact]
    public void AdminStatusTransitionRaisesOrderStatusChangedDomainEvent()
    {
        DateTime changedAtUtc = new(2026, 06, 28, 9, 0, 0, DateTimeKind.Utc);
        Guid adminUserId = Guid.NewGuid();
        Guid sellerId = Guid.NewGuid();
        Order order = CreateProductPaidOrder(Guid.NewGuid(), sellerId, changedAtUtc.AddHours(-1));

        bool changed = order.UpdateStatus(
            OrderStatus.Purchasing,
            "Buying product",
            changedAtUtc,
            adminUserId);

        Assert.True(changed);
        OrderStatusChangedDomainEvent domainEvent = Assert.Single(
            order.DomainEvents.OfType<OrderStatusChangedDomainEvent>());
        Assert.Equal(order.Id, domainEvent.OrderId);
        Assert.Equal(order.UserId, domainEvent.UserId);
        Assert.Equal(sellerId, domainEvent.SellerId);
        Assert.Equal(order.OrderNumber, domainEvent.OrderNumber);
        Assert.Equal(order.SellerProfitTotal, domainEvent.SellerProfitTotal);
        Assert.Equal(OrderStatus.ProductPaid, domainEvent.PreviousStatus);
        Assert.Equal(OrderStatus.Purchasing, domainEvent.NewStatus);
        Assert.Equal("Buying product", domainEvent.Note);
        Assert.Equal(adminUserId, domainEvent.ChangedByUserId);
        Assert.Equal(changedAtUtc, domainEvent.ChangedAtUtc);
    }

    [Fact]
    public async Task DomainHandlerPublishesOrderStatusChangedIntegrationEvent()
    {
        DateTime changedAtUtc = new(2026, 06, 28, 10, 0, 0, DateTimeKind.Utc);
        Guid adminUserId = Guid.NewGuid();
        Guid sellerId = Guid.NewGuid();
        Order order = CreateProductPaidOrder(Guid.NewGuid(), sellerId, changedAtUtc.AddHours(-1));
        var repository = new InMemoryMainRepository(order);
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new OrderStatusChangedDomainEventHandler(repository, publisher);
        var domainEvent = new OrderStatusChangedDomainEvent(
            order.Id,
            order.UserId,
            sellerId,
            order.OrderNumber,
            order.SellerProfitTotal,
            OrderStatus.ProductPaid,
            OrderStatus.Purchasing,
            "Buying product",
            adminUserId,
            changedAtUtc);

        await handler.Handle(domainEvent, CancellationToken.None);

        OrderStatusChangedIntegrationEvent integrationEvent = Assert.IsType<OrderStatusChangedIntegrationEvent>(
            Assert.Single(publisher.PublishedEvents));
        Assert.Equal(
            IntegrationEventId.Create<OrderStatusChangedIntegrationEvent>(domainEvent.Id),
            integrationEvent.Id);
        Assert.Equal(domainEvent.Id, integrationEvent.SourceEventId);
        Assert.Equal(order.Id, integrationEvent.OrderId);
        Assert.Equal(order.UserId, integrationEvent.UserId);
        Assert.Equal(sellerId, integrationEvent.SellerId);
        Assert.Equal(order.OrderNumber, integrationEvent.OrderNumber);
        Assert.Equal(order.SellerProfitTotal, integrationEvent.SellerProfitTotal);
        Assert.Equal("ProductPaid", integrationEvent.PreviousStatus);
        Assert.Equal("Purchasing", integrationEvent.NewStatus);
        Assert.Equal("Buying product", integrationEvent.Note);
        Assert.Equal(adminUserId, integrationEvent.ChangedByUserId);
        Assert.Equal(changedAtUtc, integrationEvent.ChangedAtUtc);
    }

    private static Order CreateProductPaidOrder(Guid userId, Guid sellerId, DateTime createdAtUtc)
    {
        Order order = Order.Create(
            userId,
            sellerId,
            [
                new OrderItemSnapshot(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Bag",
                    ProductImageUrl: null,
                    VariantName: null,
                    SourcePlatform: "taobao",
                    SourceProductId: "TB-STATUS-1",
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
}
