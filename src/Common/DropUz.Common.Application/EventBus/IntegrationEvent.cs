namespace DropUz.Common.Application.EventBus;

public abstract record IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
    }

    protected IntegrationEvent(Guid id, DateTime occurredOnUtc)
    {
        Id = id;
        OccurredOnUtc = occurredOnUtc;
    }

    public Guid Id { get; init; }

    public DateTime OccurredOnUtc { get; init; }
}
