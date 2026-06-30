using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Orders.IntegrationEvents;

namespace DropUz.Modules.Orders.Application.Orders;

public sealed class OrderDeliveredDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<OrderDeliveredDomainEvent>
{
    public async Task Handle(
        OrderDeliveredDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new OrderDeliveredIntegrationEvent(
            domainEvent.Id,
            domainEvent.OrderId,
            domainEvent.UserId,
            domainEvent.SellerId,
            domainEvent.SellerProfitTotal,
            domainEvent.DeliveredAtUtc)
        {
            Id = IntegrationEventId.Create<OrderDeliveredIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
