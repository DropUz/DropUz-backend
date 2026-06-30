using DropUz.Common.Domain;

namespace DropUz.Modules.Catalog.Domain.Products;

public sealed record ProductImportedDomainEvent(
    Guid ProductId,
    string SourcePlatform,
    string SourceProductId,
    Guid? ActorUserId,
    DateTime ImportedAtUtc) : DomainEvent;
