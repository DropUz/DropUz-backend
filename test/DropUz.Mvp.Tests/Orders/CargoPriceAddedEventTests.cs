using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Catalog.Domain.Pricing;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.Application.Orders;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Orders.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Orders;

public sealed class CargoPriceAddedEventTests
{
    [Fact]
    public void OrderRaisesCargoPriceAddedEventWhenCargoPriceIsSet()
    {
        DateTime addedAtUtc = new(2026, 06, 27, 15, 0, 0, DateTimeKind.Utc);
        Order order = CreateProductPaidOrder(Guid.NewGuid(), addedAtUtc.AddHours(-1));

        bool changed = order.SetCargoPrice(24m, deadlineDays: 5, addedAtUtc);

        Assert.True(changed);
        CargoPriceAddedDomainEvent domainEvent = Assert.Single(
            order.DomainEvents.OfType<CargoPriceAddedDomainEvent>());
        Assert.Equal(order.Id, domainEvent.OrderId);
        Assert.Equal(order.UserId, domainEvent.UserId);
        Assert.Equal(24m, domainEvent.CargoPrice);
        Assert.Equal(addedAtUtc.AddDays(5), domainEvent.DeadlineAtUtc);
        Assert.Equal(addedAtUtc, domainEvent.AddedAtUtc);
    }

    [Fact]
    public async Task DomainHandlerPublishesCargoPriceAddedIntegrationEvent()
    {
        DateTime addedAtUtc = new(2026, 06, 27, 16, 0, 0, DateTimeKind.Utc);
        Order order = CreateProductPaidOrder(Guid.NewGuid(), addedAtUtc.AddHours(-1));
        var repository = new InMemoryMainRepository(order);
        var publisher = new CapturingIntegrationEventPublisher();
        var handler = new CargoPriceAddedDomainEventHandler(repository, publisher);
        var domainEvent = new CargoPriceAddedDomainEvent(
            order.Id,
            order.UserId,
            CargoPrice: 24m,
            DeadlineAtUtc: addedAtUtc.AddDays(5),
            addedAtUtc);

        await handler.Handle(domainEvent, CancellationToken.None);

        CargoPriceAddedIntegrationEvent integrationEvent = Assert.IsType<CargoPriceAddedIntegrationEvent>(
            Assert.Single(publisher.PublishedEvents));
        Assert.Equal(
            IntegrationEventId.Create<CargoPriceAddedIntegrationEvent>(domainEvent.Id),
            integrationEvent.Id);
        Assert.Equal(domainEvent.OccurredOnUtc, integrationEvent.OccurredOnUtc);
        Assert.Equal(domainEvent.Id, integrationEvent.SourceEventId);
        Assert.Equal(order.Id, integrationEvent.OrderId);
        Assert.Equal(order.UserId, integrationEvent.UserId);
        Assert.Equal(24m, integrationEvent.CargoPrice);
        Assert.Equal(addedAtUtc.AddDays(5), integrationEvent.DeadlineAtUtc);
        Assert.Empty(repository.Entities.OfType<NotificationMessage>());
    }

    private static Order CreateProductPaidOrder(Guid userId, DateTime createdAtUtc)
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
                    SourceProductId: "TB-CARGO-PRICE-1",
                    SourceUrl: null,
                    ApiPrice: 100m,
                    CurrencyRate: 1m,
                    DropUzMarkup: new Markup(MarkupType.Percent, 10m),
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
