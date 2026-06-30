using DropUz.Modules.Notifications.Domain.Notifications;

namespace DropUz.Modules.Notifications.Application.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    Guid UserId,
    Guid? OrderId,
    NotificationType Type,
    NotificationChannel Channel,
    string Recipient,
    string Subject,
    string Body,
    NotificationStatus Status,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc,
    string? FailureReason,
    int AttemptCount,
    DateTime? LastAttemptAtUtc,
    string? ProviderName,
    string? ProviderMessageId);
