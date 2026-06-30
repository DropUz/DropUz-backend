using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;
using DropUz.Modules.Catalog.Domain.Imports;

namespace DropUz.Modules.Catalog.Application.Imports;

public sealed record GetCatalogImportLogsQuery(
    PageRequest Page,
    string? SourcePlatform = null,
    CatalogImportStatus? Status = null)
    : IQuery<PagedResponse<CatalogImportLogResponse>>;
