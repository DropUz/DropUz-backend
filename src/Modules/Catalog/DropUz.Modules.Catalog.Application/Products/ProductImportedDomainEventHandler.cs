using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Catalog.Domain.Products;
using DropUz.Modules.Catalog.IntegrationEvents;

namespace DropUz.Modules.Catalog.Application.Products;

public sealed class ProductImportedDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<ProductImportedDomainEvent>
{
    public async Task Handle(
        ProductImportedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new ProductImportedIntegrationEvent(
            domainEvent.Id,
            domainEvent.ProductId,
            domainEvent.SourcePlatform,
            domainEvent.SourceProductId,
            domainEvent.ActorUserId,
            domainEvent.ImportedAtUtc)
        {
            Id = IntegrationEventId.Create<ProductImportedIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
