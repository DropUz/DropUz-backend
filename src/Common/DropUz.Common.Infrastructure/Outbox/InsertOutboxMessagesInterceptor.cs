using System.Text.Json;
using DropUz.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DropUz.Common.Infrastructure.Outbox;

public sealed class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            InsertOutboxMessages(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            InsertOutboxMessages(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private static void InsertOutboxMessages(DbContext context)
    {
        List<Entity> entities = context
            .ChangeTracker
            .Entries<Entity>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        List<OutboxMessage> outboxMessages = entities
            .SelectMany(entity =>
            {
                IReadOnlyCollection<IDomainEvent> domainEvents = entity.DomainEvents.ToArray();
                entity.ClearDomainEvents();
                return domainEvents;
            })
            .Select(OutboxMessage.FromDomainEvent)
            .ToList();

        if (outboxMessages.Count > 0)
        {
            context.Set<OutboxMessage>().AddRange(outboxMessages);
        }
    }

    internal static string Serialize(IDomainEvent domainEvent)
    {
        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions);
    }
}
