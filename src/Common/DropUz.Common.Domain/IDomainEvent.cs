using MediatR;

namespace DropUz.Common.Domain;

public interface IDomainEvent : INotification
{
    Guid Id { get; }

    DateTime OccurredOnUtc { get; }
}
