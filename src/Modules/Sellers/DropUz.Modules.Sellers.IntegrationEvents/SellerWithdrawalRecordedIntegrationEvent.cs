using DropUz.Common.Application.EventBus;

namespace DropUz.Modules.Sellers.IntegrationEvents;

public sealed record SellerWithdrawalRecordedIntegrationEvent(
    Guid SourceEventId,
    Guid SellerId,
    decimal Amount,
    string? Note,
    Guid? ActorUserId,
    DateTime RecordedAtUtc) : IntegrationEvent;
