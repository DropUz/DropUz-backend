using System.Text.Json;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Domain;

namespace DropUz.Common.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private OutboxMessage()
    {
    }

    private OutboxMessage(Guid id, string type, string content, DateTime occurredOnUtc)
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

    public static OutboxMessage FromDomainEvent(IDomainEvent domainEvent)
    {
        return new OutboxMessage(
            domainEvent.Id,
            domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
            InsertOutboxMessagesInterceptor.Serialize(domainEvent),
            domainEvent.OccurredOnUtc);
    }

    public static OutboxMessage FromIntegrationEvent(IIntegrationEvent integrationEvent)
    {
        Type eventType = integrationEvent.GetType();

        return new OutboxMessage(
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
