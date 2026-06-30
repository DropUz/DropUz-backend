using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Catalog.IntegrationEvents;

namespace DropUz.Modules.Admin.Application.Audit;

public sealed class ProductImportedIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<ProductImportedIntegrationEvent>
{
    public const string ConsumerName = "admin.product-imported-audit";

    public async Task Handle(
        ProductImportedIntegrationEvent integrationEvent,
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
                AdminAuditActions.Catalog.ProductImported,
                "CatalogProduct",
                integrationEvent.ProductId,
                $"source={integrationEvent.SourcePlatform}:{integrationEvent.SourceProductId}",
                integrationEvent.ImportedAtUtc));

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
