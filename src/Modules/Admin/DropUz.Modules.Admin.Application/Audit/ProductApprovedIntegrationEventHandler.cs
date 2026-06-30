using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Catalog.IntegrationEvents;

namespace DropUz.Modules.Admin.Application.Audit;

public sealed class ProductApprovedIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<ProductApprovedIntegrationEvent>
{
    public const string ConsumerName = "admin.product-approved-audit";

    public async Task Handle(
        ProductApprovedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await inbox.TryStartProcessingAsync(integrationEvent, ConsumerName, cancellationToken))
        {
            return;
        }

        try
        {
            await repository.AddAsync(AdminAuditLog.Create(
                integrationEvent.ActorUserId,
                AdminAuditActions.Catalog.ProductApproved,
                "CatalogProduct",
                integrationEvent.ProductId,
                details: null,
                integrationEvent.ApprovedAtUtc));

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
