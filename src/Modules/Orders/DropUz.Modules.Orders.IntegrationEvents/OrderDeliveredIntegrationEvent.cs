using DropUz.Common.Application.EventBus;

namespace DropUz.Modules.Orders.IntegrationEvents;

public sealed record OrderDeliveredIntegrationEvent(
    Guid SourceEventId,
    Guid OrderId,
    Guid UserId,
    Guid SellerId,
    decimal SellerProfitTotal,
    DateTime DeliveredAtUtc) : IntegrationEvent;
