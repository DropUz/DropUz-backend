using DropUz.Common.Application.EventBus;

namespace DropUz.Modules.Orders.IntegrationEvents;

public sealed record CargoPriceAddedIntegrationEvent(
    Guid SourceEventId,
    Guid OrderId,
    Guid UserId,
    decimal CargoPrice,
    DateTime DeadlineAtUtc,
    DateTime AddedAtUtc) : IntegrationEvent;
