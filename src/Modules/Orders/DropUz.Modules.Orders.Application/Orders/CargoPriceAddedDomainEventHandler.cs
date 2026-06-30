using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Orders.IntegrationEvents;

namespace DropUz.Modules.Orders.Application.Orders;

public sealed class CargoPriceAddedDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<CargoPriceAddedDomainEvent>
{
    public async Task Handle(
        CargoPriceAddedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new CargoPriceAddedIntegrationEvent(
            domainEvent.Id,
            domainEvent.OrderId,
            domainEvent.UserId,
            domainEvent.CargoPrice,
            domainEvent.DeadlineAtUtc,
            domainEvent.AddedAtUtc)
        {
            Id = IntegrationEventId.Create<CargoPriceAddedIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(
            integrationEvent,
            cancellationToken);

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
