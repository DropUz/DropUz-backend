using DropUz.Common.Application.EventBus;

namespace DropUz.Modules.Catalog.IntegrationEvents;

public sealed record ProductRejectedIntegrationEvent(
    Guid SourceEventId,
    Guid ProductId,
    Guid? ActorUserId,
    DateTime RejectedAtUtc) : IntegrationEvent;
