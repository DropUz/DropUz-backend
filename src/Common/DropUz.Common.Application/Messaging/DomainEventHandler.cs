using DropUz.Common.Domain;

namespace DropUz.Common.Application.Messaging;

public abstract class DomainEventHandler<TDomainEvent> : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    public abstract Task Handle(TDomainEvent notification, CancellationToken cancellationToken);
}
