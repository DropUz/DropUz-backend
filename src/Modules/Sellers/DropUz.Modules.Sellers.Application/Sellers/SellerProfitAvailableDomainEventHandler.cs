using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Sellers.Domain.Sellers;
using DropUz.Modules.Sellers.IntegrationEvents;

namespace DropUz.Modules.Sellers.Application.Sellers;

public sealed class SellerProfitAvailableDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<SellerProfitAvailableDomainEvent>
{
    public async Task Handle(
        SellerProfitAvailableDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new SellerProfitAvailableIntegrationEvent(
            domainEvent.Id,
            domainEvent.SellerId,
            domainEvent.SellerUserId,
            domainEvent.OrderId,
            domainEvent.Amount,
            domainEvent.AvailableAtUtc)
        {
            Id = IntegrationEventId.Create<SellerProfitAvailableIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
