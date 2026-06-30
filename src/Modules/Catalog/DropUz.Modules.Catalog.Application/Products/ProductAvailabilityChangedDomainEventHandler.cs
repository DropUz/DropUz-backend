using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Catalog.IntegrationEvents;

namespace DropUz.Modules.Catalog.Application.Products;

public sealed class ProductAvailabilityChangedDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<ProductAvailabilityChangedDomainEvent>
{
    public async Task Handle(
        ProductAvailabilityChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new ProductAvailabilityChangedIntegrationEvent(
            domainEvent.Id,
            domainEvent.ProductId,
            domainEvent.PreviousStatus.ToString(),
            domainEvent.NewStatus.ToString(),
            domainEvent.ActorUserId,
            domainEvent.ChangedAtUtc)
        {
            Id = IntegrationEventId.Create<ProductAvailabilityChangedIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
