using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Modules.Cargo.Infrastructure.BackgroundJobs;
using DropUz.Modules.Cargo.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CargoApplication = DropUz.Modules.Cargo.Application.AssemblyReference;
using CargoPresentation = DropUz.Modules.Cargo.Presentation.AssemblyReference;

namespace DropUz.Modules.Cargo.Infrastructure;

public static class CargoModule
{
    public static IServiceCollection AddCargoModule(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddMediatR(mediatRConfiguration =>
            mediatRConfiguration.RegisterServicesFromAssembly(CargoApplication.Assembly));

        services.AddOptions<CargoPaymentExpirationOptions>();
        if (configuration is not null)
        {
            services.Configure<CargoPaymentExpirationOptions>(
                configuration.GetSection(CargoPaymentExpirationOptions.SectionName));
        }

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMainDbContextModelContributor, CargoModelContributor>());
        services.AddEndpoints(CargoPresentation.Assembly);
        services.AddHostedService<CargoPaymentExpirationBackgroundService>();

        return services;
    }
}
