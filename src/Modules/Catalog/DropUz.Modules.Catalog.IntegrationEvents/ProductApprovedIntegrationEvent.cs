using DropUz.Common.Application.EventBus;

namespace DropUz.Modules.Catalog.IntegrationEvents;

public sealed record ProductApprovedIntegrationEvent(
    Guid SourceEventId,
    Guid ProductId,
    Guid? ActorUserId,
    DateTime ApprovedAtUtc) : IntegrationEvent;
