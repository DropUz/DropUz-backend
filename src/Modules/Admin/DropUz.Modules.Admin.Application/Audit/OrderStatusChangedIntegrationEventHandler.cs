using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Orders.IntegrationEvents;

namespace DropUz.Modules.Admin.Application.Audit;

public sealed class OrderStatusChangedIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<OrderStatusChangedIntegrationEvent>
{
    public const string ConsumerName = "admin.order-status-changed-audit";

    public async Task Handle(
        OrderStatusChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await inbox.TryStartProcessingAsync(integrationEvent, ConsumerName, cancellationToken))
        {
            return;
        }

        try
        {
            await repository.AddAsync(AdminAuditLog.Create(
                integrationEvent.ChangedByUserId,
                AdminAuditActions.Orders.StatusUpdated,
                "Order",
                integrationEvent.OrderId,
                $"from={integrationEvent.PreviousStatus};to={integrationEvent.NewStatus};note={integrationEvent.Note}",
                integrationEvent.ChangedAtUtc));

            await inbox.MarkProcessedAsync(integrationEvent, ConsumerName, cancellationToken);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await inbox.MarkFailedAsync(
                integrationEvent,
                ConsumerName,
                exception.Message,
                cancellationToken);
            throw;
        }
    }
}
