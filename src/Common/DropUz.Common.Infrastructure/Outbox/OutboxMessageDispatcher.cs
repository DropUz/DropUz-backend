using System.Text.Json;
using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DropUz.Common.Infrastructure.Outbox;

public sealed class OutboxMessageDispatcher(
    IServiceProvider serviceProvider,
    OutboxMessageTypeResolver typeResolver)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        Type eventType = typeResolver.Resolve(message.Type);
        object @event = JsonSerializer.Deserialize(message.Content, eventType, SerializerOptions)
            ?? throw new InvalidOperationException($"Outbox message '{message.Id}' could not be deserialized.");

        if (@event is IDomainEvent)
        {
            await DispatchToHandlersAsync(typeof(IDomainEventHandler<>), eventType, @event, cancellationToken);
            return;
        }

        if (@event is IIntegrationEvent)
        {
            await DispatchToHandlersAsync(typeof(IIntegrationEventHandler<>), eventType, @event, cancellationToken);
            return;
        }

        throw new InvalidOperationException($"Outbox message '{message.Id}' is not a supported event.");
    }

    private async Task DispatchToHandlersAsync(
        Type openHandlerType,
        Type eventType,
        object @event,
        CancellationToken cancellationToken)
    {
        Type handlerType = openHandlerType.MakeGenericType(eventType);
        object[] handlers = serviceProvider
            .GetServices(handlerType)
            .Where(handler => handler is not null)
            .Cast<object>()
            .ToArray();

        if (handlers.Length == 0)
        {
            throw new InvalidOperationException($"No handlers registered for event '{eventType.FullName}'.");
        }

        foreach (object handler in handlers)
        {
            MethodInfo handleMethod = handler
                .GetType()
                .GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.Handle), [eventType, typeof(CancellationToken)])
                ?? throw new InvalidOperationException($"Handler '{handler.GetType().FullName}' has no compatible Handle method.");

            try
            {
                await (Task)handleMethod.Invoke(handler, [@event, cancellationToken])!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                throw exception.InnerException;
            }
        }
    }
}
