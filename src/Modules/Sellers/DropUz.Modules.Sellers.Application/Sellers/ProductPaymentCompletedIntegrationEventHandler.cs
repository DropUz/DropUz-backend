using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Payments.IntegrationEvents;
using DropUz.Modules.Sellers.Domain.Sellers;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Sellers.Application.Sellers;

public sealed class ProductPaymentCompletedIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<ProductPaymentCompletedIntegrationEvent>
{
    public const string ConsumerName = "sellers.product-payment-completed-balance";

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
            if (integrationEvent.SellerId.HasValue && integrationEvent.SellerProfitTotal > 0m)
            {
                SellerProfile seller = await repository
                    .Query<SellerProfile>(profile => profile.Id == integrationEvent.SellerId.Value)
                    .Include(profile => profile.BalanceTransactions)
                    .SingleOrDefaultAsync(cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Seller '{integrationEvent.SellerId}' for order '{integrationEvent.OrderId}' was not found.");

                seller.RecordProductPayment(
                    integrationEvent.OrderId,
                    integrationEvent.SellerProfitTotal,
                    integrationEvent.PaidAtUtc);
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
