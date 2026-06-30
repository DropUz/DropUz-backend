using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Orders.IntegrationEvents;

namespace DropUz.Modules.Orders.Application.Orders;

public sealed class CargoPaymentExpiredDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<CargoPaymentExpiredDomainEvent>
{
    public async Task Handle(
        CargoPaymentExpiredDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        Order order = await repository.GetAsync<Order>(domainEvent.OrderId)
            ?? throw new InvalidOperationException(
                $"Expired order '{domainEvent.OrderId}' was not found.");
        if (order.UserId != domainEvent.UserId ||
            order.SellerId != domainEvent.SellerId ||
            order.SellerProfitTotal != domainEvent.SellerProfitTotal)
        {
            throw new InvalidOperationException(
                $"Cargo expiration event does not match order '{domainEvent.OrderId}'.");
        }

        var integrationEvent = new CargoPaymentExpiredIntegrationEvent(
            domainEvent.Id,
            domainEvent.OrderId,
            domainEvent.UserId,
            order.OrderNumber,
            domainEvent.SellerId,
            domainEvent.SellerProfitTotal,
            domainEvent.ExpiredAtUtc)
        {
            Id = IntegrationEventId.Create<CargoPaymentExpiredIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
