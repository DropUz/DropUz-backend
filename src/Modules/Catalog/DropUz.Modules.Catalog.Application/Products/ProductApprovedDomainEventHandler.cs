using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Catalog.IntegrationEvents;

namespace DropUz.Modules.Catalog.Application.Products;

public sealed class ProductApprovedDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<ProductApprovedDomainEvent>
{
    public async Task Handle(
        ProductApprovedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new ProductApprovedIntegrationEvent(
            domainEvent.Id,
            domainEvent.ProductId,
            domainEvent.ActorUserId,
            domainEvent.ApprovedAtUtc)
        {
            Id = IntegrationEventId.Create<ProductApprovedIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
