using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Orders.IntegrationEvents;

namespace DropUz.Modules.Orders.Application.Orders;

public sealed class OrderStatusChangedDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<OrderStatusChangedDomainEvent>
{
    public async Task Handle(
        OrderStatusChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new OrderStatusChangedIntegrationEvent(
            domainEvent.Id,
            domainEvent.OrderId,
            domainEvent.UserId,
            domainEvent.SellerId,
            domainEvent.OrderNumber,
            domainEvent.SellerProfitTotal,
            domainEvent.PreviousStatus.ToString(),
            domainEvent.NewStatus.ToString(),
            domainEvent.Note,
            domainEvent.ChangedByUserId,
            domainEvent.ChangedAtUtc)
        {
            Id = IntegrationEventId.Create<OrderStatusChangedIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
