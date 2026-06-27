using DropUz.Common.Domain;

namespace DropUz.Modules.Orders.Domain.Orders;

public sealed record OrderDeliveredDomainEvent(
    Guid OrderId,
    Guid UserId,
    Guid SellerId,
    decimal SellerProfitTotal,
    DateTime DeliveredAtUtc) : DomainEvent;
