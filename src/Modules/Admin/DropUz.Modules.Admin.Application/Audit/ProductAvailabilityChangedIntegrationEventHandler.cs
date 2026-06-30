using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Catalog.IntegrationEvents;

namespace DropUz.Modules.Admin.Application.Audit;

public sealed class ProductAvailabilityChangedIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<ProductAvailabilityChangedIntegrationEvent>
{
    public const string ConsumerName = "admin.product-availability-changed-audit";

    public async Task Handle(
        ProductAvailabilityChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!await inbox.TryStartProcessingAsync(integrationEvent, ConsumerName, cancellationToken))
        {
            return;
        }

        try
        {
            string action = integrationEvent.NewStatus switch
            {
                "Approved" => AdminAuditActions.Catalog.ProductActivated,
                "Inactive" => AdminAuditActions.Catalog.ProductDeactivated,
                "Deleted" => AdminAuditActions.Catalog.ProductDeleted,
                _ => throw new InvalidOperationException(
                    $"Unsupported catalog product availability status '{integrationEvent.NewStatus}'.")
            };

            await repository.AddAsync(AdminAuditLog.Create(
                integrationEvent.ActorUserId,
                action,
                "CatalogProduct",
                integrationEvent.ProductId,
                $"from={integrationEvent.PreviousStatus};to={integrationEvent.NewStatus}",
                integrationEvent.ChangedAtUtc));

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
