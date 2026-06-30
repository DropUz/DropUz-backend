using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Catalog.IntegrationEvents;

namespace DropUz.Modules.Catalog.Application.Products;

public sealed class ProductRejectedDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<ProductRejectedDomainEvent>
{
    public async Task Handle(
        ProductRejectedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new ProductRejectedIntegrationEvent(
            domainEvent.Id,
            domainEvent.ProductId,
            domainEvent.ActorUserId,
            domainEvent.RejectedAtUtc)
        {
            Id = IntegrationEventId.Create<ProductRejectedIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
