namespace DropUz.Common.Infrastructure.Outbox;

public interface IOutboxMessageDispatcher
{
    Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
