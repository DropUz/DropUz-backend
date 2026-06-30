using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Sellers.IntegrationEvents;

namespace DropUz.Modules.Admin.Application.Audit;

public sealed class SellerProfitPendingCreatedIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<SellerProfitPendingCreatedIntegrationEvent>
{
    public const string ConsumerName = "admin.seller-profit-pending-created-audit";

    public async Task Handle(
        SellerProfitPendingCreatedIntegrationEvent integrationEvent,
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
                AdminAuditActions.Sellers.ProfitPendingCreated,
                "SellerProfile",
                integrationEvent.SellerId,
                $"orderId={integrationEvent.OrderId};amount={integrationEvent.Amount}",
                integrationEvent.CreatedAtUtc));

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
