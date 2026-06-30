using DropUz.Common.Domain;

namespace DropUz.Modules.Orders.Domain.Orders;

public sealed record OrderStatusChangedDomainEvent(
    Guid OrderId,
    Guid UserId,
    Guid? SellerId,
    string OrderNumber,
    decimal SellerProfitTotal,
    OrderStatus PreviousStatus,
    OrderStatus NewStatus,
    string? Note,
    Guid? ChangedByUserId,
    DateTime ChangedAtUtc) : DomainEvent;
