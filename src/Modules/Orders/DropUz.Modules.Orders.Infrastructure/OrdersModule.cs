using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Modules.Orders.Application.Orders;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Orders.Infrastructure.Database;
using DropUz.Modules.Payments.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrdersApplication = DropUz.Modules.Orders.Application.AssemblyReference;
using OrdersPresentation = DropUz.Modules.Orders.Presentation.AssemblyReference;

namespace DropUz.Modules.Orders.Infrastructure;

public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(OrdersApplication.Assembly));

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMainDbContextModelContributor, OrdersModelContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IDomainEventHandler<OrderDeliveredDomainEvent>,
            OrderDeliveredDomainEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IDomainEventHandler<CargoPaymentExpiredDomainEvent>,
            CargoPaymentExpiredDomainEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IDomainEventHandler<CargoPriceAddedDomainEvent>,
            CargoPriceAddedDomainEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IDomainEventHandler<OrderStatusChangedDomainEvent>,
            OrderStatusChangedDomainEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<ProductPaymentCompletedIntegrationEvent>,
            ProductPaymentCompletedIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<CargoPaymentCompletedIntegrationEvent>,
            CargoPaymentCompletedIntegrationEventHandler>());
        services.AddEndpoints(OrdersPresentation.Assembly);

        return services;
    }
}
