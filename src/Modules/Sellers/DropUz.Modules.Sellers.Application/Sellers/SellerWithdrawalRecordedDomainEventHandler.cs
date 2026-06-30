using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Modules.Sellers.Domain.Sellers;
using DropUz.Modules.Sellers.IntegrationEvents;

namespace DropUz.Modules.Sellers.Application.Sellers;

public sealed class SellerWithdrawalRecordedDomainEventHandler(
    IMainRepository repository,
    IIntegrationEventPublisher integrationEventPublisher)
    : IDomainEventHandler<SellerWithdrawalRecordedDomainEvent>
{
    public async Task Handle(
        SellerWithdrawalRecordedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new SellerWithdrawalRecordedIntegrationEvent(
            domainEvent.Id,
            domainEvent.SellerId,
            domainEvent.Amount,
            domainEvent.Note,
            domainEvent.ActorUserId,
            domainEvent.RecordedAtUtc)
        {
            Id = IntegrationEventId.Create<SellerWithdrawalRecordedIntegrationEvent>(domainEvent.Id),
            OccurredOnUtc = domainEvent.OccurredOnUtc
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
