using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.IntegrationEvents;

namespace DropUz.Modules.Orders.Application.Orders;

public sealed class CargoPaymentCompletedIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<CargoPaymentCompletedIntegrationEvent>
{
    public const string ConsumerName = "orders.cargo-payment-completed";

    public async Task Handle(
        CargoPaymentCompletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await inbox.TryStartProcessingAsync(integrationEvent, ConsumerName, cancellationToken))
        {
            return;
        }

        try
        {
            Order? order = await repository.GetAsync<Order>(integrationEvent.OrderId);
            if (order is null ||
                order.UserId != integrationEvent.UserId ||
                order.CargoTotal != integrationEvent.Amount)
            {
                throw new InvalidOperationException(
                    $"Cargo payment '{integrationEvent.PaymentId}' does not match order '{integrationEvent.OrderId}'.");
            }

            order.MarkCargoPaid(integrationEvent.PaidAtUtc);
            await inbox.MarkProcessedAsync(integrationEvent, ConsumerName, cancellationToken);
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await inbox.MarkFailedAsync(
                integrationEvent,
                ConsumerName,
                exception.Message,
                cancellationToken);
            throw;
        }
    }
}
