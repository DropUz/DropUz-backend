using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Payments.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Notifications.Application.Notifications;

public sealed class CargoPaymentCompletedIntegrationEventHandler(
    IMainRepository repository,
    INotificationService notificationService,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<CargoPaymentCompletedIntegrationEvent>
{
    public const string ConsumerName = "notifications.cargo-payment-completed";

    public async Task Handle(
        CargoPaymentCompletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await inbox.TryStartProcessingAsync(integrationEvent, ConsumerName, cancellationToken))
        {
            return;
        }

        try
        {
            string body = $"Cargo payment for order {integrationEvent.OrderNumber} was received.";
            bool notificationExists = await repository
                .Query<NotificationMessage>(notification =>
                    notification.OrderId == integrationEvent.OrderId &&
                    notification.Type == NotificationType.PaymentReceived &&
                    notification.Body == body)
                .AnyAsync(cancellationToken);

            if (!notificationExists)
            {
                await notificationService.EnqueueAsync(
                    integrationEvent.UserId,
                    integrationEvent.OrderId,
                    NotificationType.PaymentReceived,
                    "Cargo payment received",
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
