namespace DropUz.Common.Infrastructure.Inbox;

using System.Text.Json;
using DropUz.Common.Application.EventBus;

public sealed class InboxMessage
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private InboxMessage()
    {
    }

    public InboxMessage(Guid id, string type, string content, DateTime occurredOnUtc)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public DateTime OccurredOnUtc { get; private set; }

    public DateTime? ProcessedOnUtc { get; private set; }

    public DateTime? LastAttemptedOnUtc { get; private set; }

    public int RetryCount { get; private set; }

    public string? Error { get; private set; }

    public static InboxMessage FromIntegrationEvent(IIntegrationEvent integrationEvent)
    {
        Type eventType = integrationEvent.GetType();

        return new InboxMessage(
            integrationEvent.Id,
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
