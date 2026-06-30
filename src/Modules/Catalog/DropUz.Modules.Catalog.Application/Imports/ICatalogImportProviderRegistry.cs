namespace DropUz.Modules.Catalog.Application.Imports;

public interface ICatalogImportProviderRegistry
{
    ICatalogImportProvider? GetProvider(string sourcePlatform);
}
