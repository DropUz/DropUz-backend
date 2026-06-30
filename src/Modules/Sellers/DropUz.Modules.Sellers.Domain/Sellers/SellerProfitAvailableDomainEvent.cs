using DropUz.Common.Domain;

namespace DropUz.Modules.Sellers.Domain.Sellers;

public sealed record SellerProfitAvailableDomainEvent(
    Guid SellerId,
    Guid SellerUserId,
    Guid OrderId,
    decimal Amount,
    DateTime AvailableAtUtc) : DomainEvent;
