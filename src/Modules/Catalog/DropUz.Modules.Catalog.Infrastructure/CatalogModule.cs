using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Modules.Catalog.Application.Imports;
using DropUz.Modules.Catalog.Application.Products;
using DropUz.Modules.Catalog.Infrastructure.Database;
using DropUz.Modules.Catalog.Infrastructure.Imports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CatalogApplication = DropUz.Modules.Catalog.Application.AssemblyReference;
using CatalogPresentation = DropUz.Modules.Catalog.Presentation.AssemblyReference;

namespace DropUz.Modules.Catalog.Infrastructure;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(CatalogApplication.Assembly));

        services.TryAddScoped<ICatalogPricingService, CatalogPricingService>();
        services.TryAddScoped<ICatalogImportProviderRegistry, CatalogImportProviderRegistry>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<ICatalogImportProvider, ManualCatalogImportProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMainDbContextModelContributor, CatalogModelContributor>());
        services.AddEndpoints(CatalogPresentation.Assembly);

        return services;
    }
}
