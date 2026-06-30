using DropUz.Common.Domain;

namespace DropUz.Modules.Sellers.Domain.Sellers;

public sealed record SellerWithdrawalRecordedDomainEvent(
    Guid SellerId,
    decimal Amount,
    string? Note,
    Guid? ActorUserId,
    DateTime RecordedAtUtc) : DomainEvent;
