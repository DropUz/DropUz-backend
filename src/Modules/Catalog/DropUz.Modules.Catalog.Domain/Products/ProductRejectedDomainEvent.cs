using DropUz.Common.Domain;

namespace DropUz.Modules.Catalog.Domain.Products;

public sealed record ProductRejectedDomainEvent(
    Guid ProductId,
    Guid? ActorUserId,
    DateTime RejectedAtUtc) : DomainEvent;
