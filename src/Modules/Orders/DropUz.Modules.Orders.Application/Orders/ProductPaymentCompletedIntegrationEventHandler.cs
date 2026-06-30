using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Payments.IntegrationEvents;

namespace DropUz.Modules.Orders.Application.Orders;

public sealed class ProductPaymentCompletedIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<ProductPaymentCompletedIntegrationEvent>
{
    public const string ConsumerName = "orders.product-payment-completed";

    public async Task Handle(
        ProductPaymentCompletedIntegrationEvent integrationEvent,
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
                order.ProductTotal != integrationEvent.Amount)
            {
                throw new InvalidOperationException(
                    $"Product payment '{integrationEvent.PaymentId}' does not match order '{integrationEvent.OrderId}'.");
            }

            order.MarkProductPaid(integrationEvent.PaidAtUtc);
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
