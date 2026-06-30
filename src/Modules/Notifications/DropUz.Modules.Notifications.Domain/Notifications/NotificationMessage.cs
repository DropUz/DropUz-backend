using DropUz.Common.Domain;

namespace DropUz.Modules.Notifications.Domain.Notifications;

public sealed class NotificationMessage : Entity
{
    private NotificationMessage()
    {
    }

    private NotificationMessage(
        Guid id,
        Guid userId,
        Guid? orderId,
        NotificationType type,
        NotificationChannel channel,
        string recipient,
        string subject,
        string body,
        DateTime createdAtUtc)
        : base(id)
    {
        UserId = userId;
        OrderId = orderId;
        Type = type;
        Channel = channel;
        Recipient = recipient;
        Subject = subject;
        Body = body;
        Status = NotificationStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }

    public Guid? OrderId { get; private set; }

    public NotificationType Type { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public string Recipient { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public NotificationStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? SentAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTime? LastAttemptAtUtc { get; private set; }

    public string? ProviderName { get; private set; }

    public string? ProviderMessageId { get; private set; }

    public static NotificationMessage Create(
        Guid userId,
        Guid? orderId,
        NotificationType type,
        NotificationChannel channel,
        string recipient,
        string subject,
        string body,
        DateTime createdAtUtc)
    {
        return new NotificationMessage(Guid.NewGuid(), userId, orderId, type, channel, recipient, subject, body, createdAtUtc);
    }

    public void MarkSent(DateTime nowUtc)
    {
        MarkSent(nowUtc, "unknown", null);
    }

    public void MarkSent(DateTime nowUtc, string providerName, string? providerMessageId)
    {
        Status = NotificationStatus.Sent;
        SentAtUtc = nowUtc;
        FailureReason = null;
        AttemptCount++;
        LastAttemptAtUtc = nowUtc;
        ProviderName = providerName;
        ProviderMessageId = providerMessageId;
    }

    public void MarkFailed(string failureReason)
    {
        MarkFailed(failureReason, CreatedAtUtc, null);
    }

    public void MarkFailed(string failureReason, DateTime nowUtc, string? providerName)
    {
        Status = NotificationStatus.Failed;
        FailureReason = failureReason.Trim();
        AttemptCount++;
        LastAttemptAtUtc = nowUtc;
        ProviderName = providerName;
        ProviderMessageId = null;
    }

    public bool Retry()
    {
        if (Status != NotificationStatus.Failed)
        {
            return false;
        }

        Status = NotificationStatus.Pending;
        FailureReason = null;
        return true;
    }
}
