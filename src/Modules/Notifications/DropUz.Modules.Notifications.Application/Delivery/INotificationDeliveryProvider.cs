using DropUz.Modules.Notifications.Domain.Notifications;

namespace DropUz.Modules.Notifications.Application.Delivery;

public interface INotificationDeliveryProvider
{
    string Name { get; }

    NotificationChannel? Channel { get; }

    Task<NotificationDeliveryResult> SendAsync(
        NotificationDeliveryRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record NotificationDeliveryRequest(
    Guid IdempotencyKey,
    NotificationChannel Channel,
    string Recipient,
    string Subject,
    string Body);

public sealed record NotificationDeliveryResult(
    bool IsSuccess,
    string? ProviderMessageId,
    string? FailureReason)
{
    public static NotificationDeliveryResult Succeeded(string? providerMessageId = null) =>
        new(true, providerMessageId, null);

    public static NotificationDeliveryResult Failed(string failureReason) =>
        new(false, null, failureReason);
}
