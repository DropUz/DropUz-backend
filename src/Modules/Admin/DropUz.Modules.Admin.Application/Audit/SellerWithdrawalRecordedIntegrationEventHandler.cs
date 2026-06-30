using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Sellers.IntegrationEvents;

namespace DropUz.Modules.Admin.Application.Audit;

public sealed class SellerWithdrawalRecordedIntegrationEventHandler(
    IMainRepository repository,
    IIntegrationEventInbox inbox)
    : IIntegrationEventHandler<SellerWithdrawalRecordedIntegrationEvent>
{
    public const string ConsumerName = "admin.seller-withdrawal-recorded-audit";

    public async Task Handle(
        SellerWithdrawalRecordedIntegrationEvent integrationEvent,
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
                AdminAuditActions.Sellers.WithdrawalRecorded,
                "SellerProfile",
                integrationEvent.SellerId,
                $"amount={integrationEvent.Amount};note={integrationEvent.Note}",
                integrationEvent.RecordedAtUtc));

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
