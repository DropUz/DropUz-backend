using DropUz.Common.Application.EventBus;
using DropUz.Common.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Common.Infrastructure.Outbox;

public sealed class OutboxIntegrationEventPublisher(MainDbContext context) : IIntegrationEventPublisher
{
    public async Task PublishAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        bool exists = context.OutboxMessages.Local.Any(message => message.Id == integrationEvent.Id) ||
                      await context.OutboxMessages.AnyAsync(
                          message => message.Id == integrationEvent.Id,
                          cancellationToken);

        if (exists)
        {
            return;
        }

        await context.OutboxMessages.AddAsync(
            OutboxMessage.FromIntegrationEvent(integrationEvent),
            cancellationToken);
    }
}
