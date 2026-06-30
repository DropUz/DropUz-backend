using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Catalog.IntegrationEvents;

namespace DropUz.Modules.Admin.Application.Audit;

public sealed class ProductRejectedIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<ProductRejectedIntegrationEvent>
{
    public const string ConsumerName = "admin.product-rejected-audit";

    public async Task Handle(
        ProductRejectedIntegrationEvent integrationEvent,
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
                AdminAuditActions.Catalog.ProductRejected,
                "CatalogProduct",
                integrationEvent.ProductId,
                details: null,
                integrationEvent.RejectedAtUtc));

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
