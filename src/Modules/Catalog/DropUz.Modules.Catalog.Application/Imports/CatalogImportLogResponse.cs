using DropUz.Modules.Catalog.Domain.Imports;

namespace DropUz.Modules.Catalog.Application.Imports;

public sealed record CatalogImportLogResponse(
    Guid Id,
    string SourcePlatform,
    string SourceProductId,
    string ProviderName,
    CatalogImportStatus Status,
    Guid? CatalogProductId,
    CatalogImportOperation? Operation,
    string? ErrorCode,
    string? ErrorMessage,
    Guid? RequestedByUserId,
    DateTime CompletedAtUtc);
