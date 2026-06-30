using DropUz.Common.Application.EventBus;

namespace DropUz.Modules.Orders.IntegrationEvents;

public sealed record OrderStatusChangedIntegrationEvent(
    Guid SourceEventId,
    Guid OrderId,
    Guid UserId,
    Guid? SellerId,
    string OrderNumber,
    decimal SellerProfitTotal,
    string PreviousStatus,
    string NewStatus,
    string? Note,
    Guid? ChangedByUserId,
    DateTime ChangedAtUtc) : IntegrationEvent;
