using DropUz.Common.Domain;

namespace DropUz.Modules.Sellers.Domain.Sellers;

public sealed record SellerProfitPendingCreatedDomainEvent(
    Guid SellerId,
    Guid SellerUserId,
    Guid OrderId,
    decimal Amount,
    DateTime CreatedAtUtc) : DomainEvent;
