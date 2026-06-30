using DropUz.Common.Application.EventBus;

namespace DropUz.Modules.Orders.IntegrationEvents;

public sealed record CargoPaymentExpiredIntegrationEvent(
    Guid SourceEventId,
    Guid OrderId,
    Guid UserId,
    string OrderNumber,
    Guid? SellerId,
    decimal SellerProfitTotal,
    DateTime ExpiredAtUtc) : IntegrationEvent;
