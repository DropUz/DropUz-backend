using DropUz.Modules.Notifications.Application.Delivery;
using DropUz.Modules.Notifications.Domain.Notifications;

namespace DropUz.Modules.Notifications.Infrastructure.Delivery;

internal sealed class MockNotificationDeliveryProvider : INotificationDeliveryProvider
{
    public string Name => "mock";

    public NotificationChannel? Channel => null;

    public Task<NotificationDeliveryResult> SendAsync(
        NotificationDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        string providerMessageId = $"mock:{request.IdempotencyKey:N}";
        return Task.FromResult(NotificationDeliveryResult.Succeeded(providerMessageId));
    }
}
