using DropUz.Common.Domain;

namespace DropUz.Modules.Orders.Domain.Orders;

public sealed record CargoPaymentExpiredDomainEvent(
    Guid OrderId,
    Guid UserId,
    Guid? SellerId,
    decimal SellerProfitTotal,
    DateTime ExpiredAtUtc) : DomainEvent;
