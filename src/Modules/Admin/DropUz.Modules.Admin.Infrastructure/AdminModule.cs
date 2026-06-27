using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Admin.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AdminApplication = DropUz.Modules.Admin.Application.AssemblyReference;
using AdminPresentation = DropUz.Modules.Admin.Presentation.AssemblyReference;

namespace DropUz.Modules.Admin.Infrastructure;

public static class AdminModule
{
    public static IServiceCollection AddAdminModule(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(AdminApplication.Assembly));

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMainDbContextModelContributor, AdminModelContributor>());
        services.AddScoped<IAdminAuditService, AdminAuditService>();
        services.AddEndpoints(AdminPresentation.Assembly);

        return services;
    }
}
