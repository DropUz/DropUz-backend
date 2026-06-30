using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Notifications.Application.Notifications;

public sealed class CargoPriceAddedIntegrationEventHandler(
    IMainRepository repository,
    INotificationService notificationService,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<CargoPriceAddedIntegrationEvent>
{
    public const string ConsumerName = "notifications.cargo-price-added";

    public async Task Handle(
        CargoPriceAddedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await inbox.TryStartProcessingAsync(
                integrationEvent,
                ConsumerName,
                cancellationToken))
        {
            return;
        }

        try
        {
            string body = FormattableString.Invariant(
                $"Cargo price for order {integrationEvent.OrderId} is {integrationEvent.CargoPrice}. Payment deadline: {integrationEvent.DeadlineAtUtc:O}.");

            bool notificationExists = await repository
                .Query<NotificationMessage>(notification =>
                    notification.OrderId == integrationEvent.OrderId &&
                    notification.Type == NotificationType.CargoPriceAdded &&
                    notification.Body == body)
                .AnyAsync(cancellationToken);

            if (!notificationExists)
            {
                await notificationService.EnqueueAsync(
                    integrationEvent.UserId,
                    integrationEvent.OrderId,
                    NotificationType.CargoPriceAdded,
                    "Cargo price added",
                    body,
                    cancellationToken);
            }

            await inbox.MarkProcessedAsync(
                integrationEvent,
                ConsumerName,
                cancellationToken);
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
