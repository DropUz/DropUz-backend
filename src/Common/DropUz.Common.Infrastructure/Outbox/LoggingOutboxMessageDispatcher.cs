using Microsoft.Extensions.Logging;

namespace DropUz.Common.Infrastructure.Outbox;

public sealed class LoggingOutboxMessageDispatcher(
    ILogger<LoggingOutboxMessageDispatcher> logger) : IOutboxMessageDispatcher
{
    public Task DispatchAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        LogOutboxMessageDispatched(logger, message.Id, message.Type, null);

        return Task.CompletedTask;
    }

    private static readonly Action<ILogger, Guid, string, Exception?> LogOutboxMessageDispatched =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(1, nameof(LogOutboxMessageDispatched)),
            "Outbox message {OutboxMessageId} with type {OutboxMessageType} was dispatched by the baseline dispatcher.");
}
