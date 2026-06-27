using DropUz.Common.Application.Messaging;
using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Modules.Orders.Application.Orders;
using DropUz.Modules.Orders.Domain.Orders;
using DropUz.Modules.Orders.Infrastructure.Database;
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
        services.AddEndpoints(OrdersPresentation.Assembly);

        return services;
    }
}
