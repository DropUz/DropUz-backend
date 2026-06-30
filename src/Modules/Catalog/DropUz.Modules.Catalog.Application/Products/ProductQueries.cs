using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;

namespace DropUz.Modules.Catalog.Application.Products;

public sealed record GetCatalogProductsQuery(
    string? Search,
    Guid? CategoryId,
    bool ApprovedOnly,
    PageRequest Page,
    CatalogProductSort Sort = CatalogProductSort.NameAscending)
    : IQuery<PagedResponse<CatalogProductResponse>>;

public sealed record GetCatalogProductQuery(Guid ProductId) : IQuery<CatalogProductResponse>;
