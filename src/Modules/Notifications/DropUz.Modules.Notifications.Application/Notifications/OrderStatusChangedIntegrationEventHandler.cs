using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Notifications.Application.Notifications;

public sealed class OrderStatusChangedIntegrationEventHandler(
    IMainRepository repository,
    INotificationService notificationService,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<OrderStatusChangedIntegrationEvent>
{
    public const string ConsumerName = "notifications.order-status-changed";

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
            if (!string.Equals(
                    integrationEvent.NewStatus,
                    "CargoPaymentExpired",
                    StringComparison.Ordinal))
            {
                string body = $"Order {integrationEvent.OrderNumber} status changed to {integrationEvent.NewStatus}.";
                bool notificationExists = await repository
                    .Query<NotificationMessage>(notification =>
                        notification.OrderId == integrationEvent.OrderId &&
                        notification.Type == NotificationType.OrderStatusChanged &&
                        notification.Body == body)
                    .AnyAsync(cancellationToken);

                if (!notificationExists)
                {
                    await notificationService.EnqueueAsync(
                        integrationEvent.UserId,
                        integrationEvent.OrderId,
                        NotificationType.OrderStatusChanged,
                        "Order status updated",
                        body,
                        cancellationToken);
                }
            }

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
