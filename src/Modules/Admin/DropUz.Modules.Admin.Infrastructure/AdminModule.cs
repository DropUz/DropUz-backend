using DropUz.Common.Application.EventBus;
using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Admin.Infrastructure.Database;
using DropUz.Modules.Catalog.IntegrationEvents;
using DropUz.Modules.Orders.IntegrationEvents;
using DropUz.Modules.Sellers.IntegrationEvents;
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
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<OrderStatusChangedIntegrationEvent>,
            OrderStatusChangedIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<ProductImportedIntegrationEvent>,
            ProductImportedIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<ProductApprovedIntegrationEvent>,
            ProductApprovedIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<ProductRejectedIntegrationEvent>,
            ProductRejectedIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<ProductAvailabilityChangedIntegrationEvent>,
            ProductAvailabilityChangedIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<SellerWithdrawalRecordedIntegrationEvent>,
            SellerWithdrawalRecordedIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<SellerProfitPendingCreatedIntegrationEvent>,
            SellerProfitPendingCreatedIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<SellerProfitAvailableIntegrationEvent>,
            SellerProfitAvailableIntegrationEventHandler>());
        services.AddEndpoints(AdminPresentation.Assembly);

        return services;
    }
}
