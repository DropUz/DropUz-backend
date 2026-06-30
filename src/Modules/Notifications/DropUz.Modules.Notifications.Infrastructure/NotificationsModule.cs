using DropUz.Common.Application.EventBus;
using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Modules.Notifications.Application.Delivery;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Infrastructure.Database;
using DropUz.Modules.Notifications.Infrastructure.Delivery;
using DropUz.Modules.Orders.IntegrationEvents;
using DropUz.Modules.Payments.IntegrationEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NotificationsApplication = DropUz.Modules.Notifications.Application.AssemblyReference;
using NotificationsPresentation = DropUz.Modules.Notifications.Presentation.AssemblyReference;

namespace DropUz.Modules.Notifications.Infrastructure;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(NotificationsApplication.Assembly));

        services.TryAddScoped<INotificationService, NotificationService>();
        services.TryAddScoped<INotificationDeliveryProviderRegistry, NotificationDeliveryProviderRegistry>();
        services.TryAddScoped<NotificationDeliveryProcessor>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<INotificationDeliveryProvider, MockNotificationDeliveryProvider>());
        if (configuration is null)
        {
            services.AddOptions<NotificationDeliveryOptions>();
        }
        else
        {
            services.Configure<NotificationDeliveryOptions>(
                configuration.GetSection(NotificationDeliveryOptions.SectionName));
        }

        services.AddHostedService<NotificationDeliveryHostedService>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<CargoPriceAddedIntegrationEvent>,
            CargoPriceAddedIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<OrderStatusChangedIntegrationEvent>,
            OrderStatusChangedIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<ProductPaymentCompletedIntegrationEvent>,
            ProductPaymentCompletedIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<CargoPaymentCompletedIntegrationEvent>,
            CargoPaymentCompletedIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IIntegrationEventHandler<CargoPaymentExpiredIntegrationEvent>,
            CargoPaymentExpiredIntegrationEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMainDbContextModelContributor, NotificationsModelContributor>());
        services.AddEndpoints(NotificationsPresentation.Assembly);

        return services;
    }
}
