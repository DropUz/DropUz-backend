using DropUz.Common.Application.Clock;
using DropUz.Common.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DropUz.Common.Infrastructure.Outbox;

public sealed class OutboxMessageProcessor(
    MainDbContext context,
    OutboxMessageDispatcher dispatcher,
    IDateTimeProvider dateTimeProvider,
    ILogger<OutboxMessageProcessor> logger)
{
    public async Task<int> ProcessPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        int normalizedBatchSize = batchSize <= 0 ? 20 : batchSize;
        List<OutboxMessage> messages = await context
            .Set<OutboxMessage>()
            .Where(message => message.ProcessedOnUtc == null)
            .OrderBy(message => message.OccurredOnUtc)
            .Take(normalizedBatchSize)
            .ToListAsync(cancellationToken);

        int processedCount = 0;
        foreach (OutboxMessage message in messages)
        {
            try
            {
                await dispatcher.DispatchAsync(message, cancellationToken);
                message.MarkProcessed(dateTimeProvider.UtcNow);
                processedCount++;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Outbox message {MessageId} processing failed.", message.Id);
                message.MarkFailed(exception.Message, dateTimeProvider.UtcNow);
            }
        }

        if (messages.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return processedCount;
    }
}
