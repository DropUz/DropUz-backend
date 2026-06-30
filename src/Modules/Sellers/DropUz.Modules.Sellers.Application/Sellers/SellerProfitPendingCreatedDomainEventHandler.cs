using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Sellers.Domain.Sellers;
using DropUz.Modules.Sellers.IntegrationEvents;

namespace DropUz.Modules.Sellers.Application.Sellers;

public sealed class SellerProfitPendingCreatedDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<SellerProfitPendingCreatedDomainEvent>
{
    public async Task Handle(
        SellerProfitPendingCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new SellerProfitPendingCreatedIntegrationEvent(
            domainEvent.Id,
            domainEvent.SellerId,
            domainEvent.SellerUserId,
            domainEvent.OrderId,
            domainEvent.Amount,
            domainEvent.CreatedAtUtc)
        {
            Id = IntegrationEventId.Create<SellerProfitPendingCreatedIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
