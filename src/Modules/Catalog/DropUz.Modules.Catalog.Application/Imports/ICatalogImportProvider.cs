using DropUz.Common.Domain;

namespace DropUz.Modules.Catalog.Application.Imports;

public interface ICatalogImportProvider
{
    string Name { get; }

    string SourcePlatform { get; }

    Task<Result<CatalogImportProductData>> ImportAsync(
        CatalogImportProviderRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record CatalogImportProviderRequest(
    Guid? CategoryId,
    string Name,
    string? Description,
    string? ImageUrl,
    string SourcePlatform,
    string SourceProductId,
    string? SourceUrl,
    decimal ApiPrice,
    string CurrencyCode,
    decimal CurrencyRate);

public sealed record CatalogImportProductData(
    Guid? CategoryId,
    string Name,
    string? Description,
    string? ImageUrl,
    string SourcePlatform,
    string SourceProductId,
    string? SourceUrl,
    decimal ApiPrice,
    string CurrencyCode,
    decimal CurrencyRate);
