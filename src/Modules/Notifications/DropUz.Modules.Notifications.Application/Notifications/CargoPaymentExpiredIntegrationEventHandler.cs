using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Notifications.Application.Notifications;

public sealed class CargoPaymentExpiredIntegrationEventHandler(
    IMainRepository repository,
    INotificationService notificationService,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<CargoPaymentExpiredIntegrationEvent>
{
    public const string ConsumerName = "notifications.cargo-payment-expired";

    public async Task Handle(
        CargoPaymentExpiredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await inbox.TryStartProcessingAsync(integrationEvent, ConsumerName, cancellationToken))
        {
            return;
        }

        try
        {
            string body = $"Cargo payment deadline for order {integrationEvent.OrderNumber} expired.";
            bool notificationExists = await repository
                .Query<NotificationMessage>(notification =>
                    notification.OrderId == integrationEvent.OrderId &&
                    notification.Type == NotificationType.CargoExpired &&
                    notification.Body == body)
                .AnyAsync(cancellationToken);

            if (!notificationExists)
            {
                await notificationService.EnqueueAsync(
                    integrationEvent.UserId,
                    integrationEvent.OrderId,
                    NotificationType.CargoExpired,
                    "Cargo payment expired",
                    body,
                    cancellationToken);
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
