using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Orders.IntegrationEvents;
using DropUz.Modules.Sellers.Domain.Sellers;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Sellers.Application.Sellers;

public sealed class CargoPaymentExpiredIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<CargoPaymentExpiredIntegrationEvent>
{
    public const string ConsumerName = "sellers.cargo-payment-expired-balance";

    public async Task Handle(
        CargoPaymentExpiredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await inbox.TryStartProcessingAsync(integrationEvent, ConsumerName, cancellationToken))
        {
            return;
        }

        try
        {
            if (integrationEvent.SellerId.HasValue && integrationEvent.SellerProfitTotal > 0m)
            {
                SellerProfile seller = await repository
                    .Query<SellerProfile>(profile => profile.Id == integrationEvent.SellerId.Value)
                    .Include(profile => profile.BalanceTransactions)
                    .SingleOrDefaultAsync(cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Seller '{integrationEvent.SellerId}' for expired order '{integrationEvent.OrderId}' was not found.");

                seller.ReversePendingProfit(
                    integrationEvent.OrderId,
                    integrationEvent.SellerProfitTotal,
                    "Cargo payment expired.",
                    integrationEvent.ExpiredAtUtc);
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
