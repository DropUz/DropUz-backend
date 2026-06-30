namespace DropUz.Common.Application.EventBus;

public interface IIntegrationEventInbox
{
    Task<bool> TryStartProcessingAsync(
        IIntegrationEvent integrationEvent,
        string consumerName,
        CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(
        IIntegrationEvent integrationEvent,
        string consumerName,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        IIntegrationEvent integrationEvent,
        string consumerName,
        string error,
        CancellationToken cancellationToken = default);
}
