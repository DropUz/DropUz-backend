namespace DropUz.Common.Infrastructure.Inbox;

using System.Text.Json;
using DropUz.Common.Application.EventBus;

public sealed class InboxMessage
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private InboxMessage()
    {
    }

    public InboxMessage(Guid id, string consumerName, string type, string content, DateTime occurredOnUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);

        Id = id;
        ConsumerName = consumerName.Trim();
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid Id { get; private set; }

    public string ConsumerName { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public DateTime OccurredOnUtc { get; private set; }

    public DateTime? ProcessedOnUtc { get; private set; }

    public DateTime? LastAttemptedOnUtc { get; private set; }

    public int RetryCount { get; private set; }

    public string? Error { get; private set; }

    public static InboxMessage FromIntegrationEvent(
        IIntegrationEvent integrationEvent,
        string consumerName)
    {
        Type eventType = integrationEvent.GetType();

        return new InboxMessage(
            integrationEvent.Id,
            consumerName,
            eventType.FullName ?? eventType.Name,
            JsonSerializer.Serialize(integrationEvent, eventType, SerializerOptions),
            integrationEvent.OccurredOnUtc);
    }

    public void MarkProcessed(DateTime processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc;
        LastAttemptedOnUtc = processedOnUtc;
        Error = null;
    }

    public void MarkFailed(string error, DateTime attemptedOnUtc)
    {
        LastAttemptedOnUtc = attemptedOnUtc;
        RetryCount++;
        Error = error;
    }
}
