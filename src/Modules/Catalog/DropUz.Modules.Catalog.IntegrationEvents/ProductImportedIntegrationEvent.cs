using DropUz.Common.Application.EventBus;

namespace DropUz.Modules.Catalog.IntegrationEvents;

public sealed record ProductImportedIntegrationEvent(
    Guid SourceEventId,
    Guid ProductId,
    string SourcePlatform,
    string SourceProductId,
    Guid? ActorUserId,
    DateTime ImportedAtUtc) : IntegrationEvent;
