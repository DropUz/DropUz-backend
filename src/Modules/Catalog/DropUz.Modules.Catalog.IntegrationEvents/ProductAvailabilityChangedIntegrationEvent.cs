using DropUz.Common.Application.EventBus;

namespace DropUz.Modules.Catalog.IntegrationEvents;

public sealed record ProductAvailabilityChangedIntegrationEvent(
    Guid SourceEventId,
    Guid ProductId,
    string PreviousStatus,
    string NewStatus,
    Guid? ActorUserId,
    DateTime ChangedAtUtc) : IntegrationEvent;
