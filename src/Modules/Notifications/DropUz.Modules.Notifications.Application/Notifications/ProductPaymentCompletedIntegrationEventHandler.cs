using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Payments.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Notifications.Application.Notifications;

public sealed class ProductPaymentCompletedIntegrationEventHandler(
    IMainRepository repository,
    INotificationService notificationService,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<ProductPaymentCompletedIntegrationEvent>
{
    public const string ConsumerName = "notifications.product-payment-completed";

    public async Task Handle(
        ProductPaymentCompletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await inbox.TryStartProcessingAsync(integrationEvent, ConsumerName, cancellationToken))
        {
            return;
        }

        try
        {
            string body = $"Product payment for order {integrationEvent.OrderNumber} was received.";
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
                    "Product payment received",
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
