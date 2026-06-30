using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Application.Imports;

namespace DropUz.Modules.Catalog.Infrastructure.Imports;

internal sealed class ManualCatalogImportProvider : ICatalogImportProvider
{
    public string Name => "manual";

    public string SourcePlatform => "*";

    public Task<Result<CatalogImportProductData>> ImportAsync(
        CatalogImportProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = new CatalogImportProductData(
            request.CategoryId,
            request.Name.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            request.SourcePlatform.Trim(),
            request.SourceProductId.Trim(),
            string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim(),
            request.ApiPrice,
            request.CurrencyCode.Trim().ToUpperInvariant(),
            request.CurrencyRate);

        return Task.FromResult(Result.Success(product));
    }
}
