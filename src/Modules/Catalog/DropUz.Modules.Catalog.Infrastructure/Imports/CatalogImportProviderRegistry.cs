using DropUz.Modules.Catalog.Application.Imports;

namespace DropUz.Modules.Catalog.Infrastructure.Imports;

internal sealed class CatalogImportProviderRegistry(IEnumerable<ICatalogImportProvider> providers)
    : ICatalogImportProviderRegistry
{
    private readonly IReadOnlyCollection<ICatalogImportProvider> _providers = providers.ToArray();

    public ICatalogImportProvider? GetProvider(string sourcePlatform)
    {
        ICatalogImportProvider? exactProvider = _providers.FirstOrDefault(provider =>
            string.Equals(provider.SourcePlatform, sourcePlatform.Trim(), StringComparison.OrdinalIgnoreCase));

        return exactProvider ?? _providers.FirstOrDefault(provider => provider.SourcePlatform == "*");
    }
}
