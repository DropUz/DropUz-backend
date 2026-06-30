using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Sellers.IntegrationEvents;

namespace DropUz.Modules.Admin.Application.Audit;

public sealed class SellerProfitAvailableIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<SellerProfitAvailableIntegrationEvent>
{
    public const string ConsumerName = "admin.seller-profit-available-audit";

    public async Task Handle(
        SellerProfitAvailableIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await inbox.TryStartProcessingAsync(integrationEvent, ConsumerName, cancellationToken))
        {
            return;
        }

        try
        {
            await repository.AddAsync(AdminAuditLog.Create(
                adminUserId: null,
                AdminAuditActions.Sellers.ProfitAvailable,
                "SellerProfile",
                integrationEvent.SellerId,
                $"orderId={integrationEvent.OrderId};amount={integrationEvent.Amount}",
                integrationEvent.AvailableAtUtc));

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
