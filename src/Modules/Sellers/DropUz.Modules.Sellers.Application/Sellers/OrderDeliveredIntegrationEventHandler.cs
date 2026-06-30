using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Orders.IntegrationEvents;
using DropUz.Modules.Sellers.Domain.Sellers;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Sellers.Application.Sellers;

public sealed class OrderDeliveredIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<OrderDeliveredIntegrationEvent>
{
    public const string ConsumerName = "sellers.order-delivered-balance";

    public async Task Handle(
        OrderDeliveredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await inbox.TryStartProcessingAsync(integrationEvent, ConsumerName, cancellationToken))
        {
            return;
        }

        try
        {
            SellerProfile seller = await repository
                .Query<SellerProfile>(profile => profile.Id == integrationEvent.SellerId)
                .Include(profile => profile.BalanceTransactions)
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Seller '{integrationEvent.SellerId}' for delivered order '{integrationEvent.OrderId}' was not found.");

            seller.ReleaseDeliveredProfit(
                integrationEvent.OrderId,
                integrationEvent.SellerProfitTotal,
                integrationEvent.DeliveredAtUtc);

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
