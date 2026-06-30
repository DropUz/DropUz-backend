using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Orders.IntegrationEvents;
using DropUz.Modules.Sellers.Domain.Sellers;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Sellers.Application.Sellers;

public sealed class OrderStatusChangedIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<OrderStatusChangedIntegrationEvent>
{
    public const string ConsumerName = "sellers.order-status-changed-balance";

    public async Task Handle(
        OrderStatusChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await inbox.TryStartProcessingAsync(integrationEvent, ConsumerName, cancellationToken))
        {
            return;
        }

        try
        {
            bool reversesProfit = string.Equals(
                                      integrationEvent.NewStatus,
                                      "Cancelled",
                                      StringComparison.Ordinal) ||
                                  string.Equals(
                                      integrationEvent.NewStatus,
                                      "Refunded",
                                      StringComparison.Ordinal);

            if (reversesProfit &&
                integrationEvent.SellerId.HasValue &&
                integrationEvent.SellerProfitTotal > 0m)
            {
                SellerProfile? seller = await repository
                    .Query<SellerProfile>(profile => profile.Id == integrationEvent.SellerId.Value)
                    .Include(profile => profile.BalanceTransactions)
                    .FirstOrDefaultAsync(cancellationToken);

                seller?.ReversePendingProfit(
                    integrationEvent.OrderId,
                    integrationEvent.SellerProfitTotal,
                    "Order is not payable to seller.",
                    integrationEvent.ChangedAtUtc);
            }

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
