using DropUz.Common.Domain;

namespace DropUz.Modules.Catalog.Domain.Products;

public sealed record ProductApprovedDomainEvent(
    Guid ProductId,
    Guid? ActorUserId,
    DateTime ApprovedAtUtc) : DomainEvent;
