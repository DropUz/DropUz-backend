using DropUz.Common.Application.Clock;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Common.Infrastructure.Inbox;

public sealed class InboxMessageService(
    MainDbContext context,
    IDateTimeProvider dateTimeProvider)
{
    public async Task<bool> TryStartProcessingAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        bool exists = await context
            .Set<InboxMessage>()
            .AnyAsync(message => message.Id == integrationEvent.Id, cancellationToken);

        if (exists)
        {
            return false;
        }

        await context.Set<InboxMessage>().AddAsync(InboxMessage.FromIntegrationEvent(integrationEvent), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task MarkProcessedAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        InboxMessage message = await GetRequiredMessageAsync(integrationEvent.Id, cancellationToken);
        message.MarkProcessed(dateTimeProvider.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        IIntegrationEvent integrationEvent,
        string error,
        CancellationToken cancellationToken = default)
    {
        InboxMessage message = await GetRequiredMessageAsync(integrationEvent.Id, cancellationToken);
        message.MarkFailed(error, dateTimeProvider.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<InboxMessage> GetRequiredMessageAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Set<InboxMessage>().FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Inbox message '{id}' was not found.");
    }
}
