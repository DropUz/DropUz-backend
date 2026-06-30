using DropUz.Common.Application.EventBus;

namespace DropUz.Modules.Sellers.IntegrationEvents;

public sealed record SellerProfitAvailableIntegrationEvent(
    Guid SourceEventId,
    Guid SellerId,
    Guid SellerUserId,
    Guid OrderId,
    decimal Amount,
    DateTime AvailableAtUtc) : IntegrationEvent;
