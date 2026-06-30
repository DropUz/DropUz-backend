using DropUz.Common.Domain;

namespace DropUz.Modules.Catalog.Domain.Products;

public sealed record ProductAvailabilityChangedDomainEvent(
    Guid ProductId,
    ProductStatus PreviousStatus,
    ProductStatus NewStatus,
    Guid? ActorUserId,
    DateTime ChangedAtUtc) : DomainEvent;
