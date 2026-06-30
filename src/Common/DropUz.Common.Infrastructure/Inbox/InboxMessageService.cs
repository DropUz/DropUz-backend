using DropUz.Common.Application.Clock;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Common.Infrastructure.Inbox;

public sealed class InboxMessageService(
    MainDbContext context,
    IDateTimeProvider dateTimeProvider) : IIntegrationEventInbox
{
    public async Task<bool> TryStartProcessingAsync(
        IIntegrationEvent integrationEvent,
        string consumerName,
        CancellationToken cancellationToken = default)
    {
        consumerName = NormalizeConsumerName(consumerName);

        InboxMessage? existingMessage = await context
            .Set<InboxMessage>()
            .SingleOrDefaultAsync(
                message => message.Id == integrationEvent.Id && message.ConsumerName == consumerName,
                cancellationToken);

        if (existingMessage is not null)
        {
            return existingMessage.ProcessedOnUtc is null;
        }

        await context.Set<InboxMessage>().AddAsync(
            InboxMessage.FromIntegrationEvent(integrationEvent, consumerName),
            cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task MarkProcessedAsync(
        IIntegrationEvent integrationEvent,
        string consumerName,
        CancellationToken cancellationToken = default)
    {
        consumerName = NormalizeConsumerName(consumerName);
        InboxMessage message = await GetRequiredMessageAsync(
            integrationEvent.Id,
            consumerName,
            cancellationToken);
        message.MarkProcessed(dateTimeProvider.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        IIntegrationEvent integrationEvent,
        string consumerName,
        string error,
        CancellationToken cancellationToken = default)
    {
        consumerName = NormalizeConsumerName(consumerName);
        DiscardPendingConsumerChanges();
        InboxMessage message = await GetRequiredMessageAsync(
            integrationEvent.Id,
            consumerName,
            cancellationToken);
        message.MarkFailed(error, dateTimeProvider.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
    }

    private void DiscardPendingConsumerChanges()
    {
        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry in context.ChangeTracker
                     .Entries()
                     .Where(entry => entry.Entity is not InboxMessage)
                     .ToArray())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;
                case EntityState.Modified:
                    entry.CurrentValues.SetValues(entry.OriginalValues);
                    entry.State = EntityState.Unchanged;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Unchanged;
                    break;
            }
        }
    }

    private async Task<InboxMessage> GetRequiredMessageAsync(
        Guid id,
        string consumerName,
        CancellationToken cancellationToken)
    {
        return await context.Set<InboxMessage>().FindAsync([id, consumerName], cancellationToken)
            ?? throw new InvalidOperationException(
                $"Inbox message '{id}' for consumer '{consumerName}' was not found.");
    }

    private static string NormalizeConsumerName(string consumerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);
        return consumerName.Trim();
    }
}
