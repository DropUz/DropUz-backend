using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Modules.Notifications.Application.Delivery;
using DropUz.Modules.Notifications.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropUz.Modules.Notifications.Infrastructure.Delivery;

public sealed class NotificationDeliveryProcessor(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    INotificationDeliveryProviderRegistry providerRegistry,
    ILogger<NotificationDeliveryProcessor> logger)
{
    public async Task<int> ProcessPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        int normalizedBatchSize = batchSize <= 0 ? 20 : batchSize;
        List<NotificationMessage> messages = await repository
            .Query<NotificationMessage>(message => message.Status == NotificationStatus.Pending)
            .OrderBy(message => message.CreatedAtUtc)
            .Take(normalizedBatchSize)
            .ToListAsync(cancellationToken);

        foreach (NotificationMessage message in messages)
        {
            INotificationDeliveryProvider? provider = providerRegistry.GetProvider(message.Channel);
            if (provider is null)
            {
                message.MarkFailed(
                    $"No delivery provider is registered for channel '{message.Channel}'.",
                    dateTimeProvider.UtcNow,
                    providerName: null);
                continue;
            }

            try
            {
                NotificationDeliveryResult result = await provider.SendAsync(
                    new NotificationDeliveryRequest(
                        message.Id,
                        message.Channel,
                        message.Recipient,
                        message.Subject,
                        message.Body),
                    cancellationToken);

                if (result.IsSuccess)
                {
                    message.MarkSent(dateTimeProvider.UtcNow, provider.Name, result.ProviderMessageId);
                }
                else
                {
                    message.MarkFailed(
                        result.FailureReason ?? "Notification provider rejected the message.",
                        dateTimeProvider.UtcNow,
                        provider.Name);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Notification {NotificationId} delivery through {ProviderName} failed.",
                    message.Id,
                    provider.Name);
                message.MarkFailed(exception.Message, dateTimeProvider.UtcNow, provider.Name);
            }
        }

        if (messages.Count > 0)
        {
            await repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }
}
