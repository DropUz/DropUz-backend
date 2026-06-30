using DropUz.Common.Application.Messaging;
using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Presentation.Endpoints;
using DropUz.Modules.Payments.Application.Payments;
using DropUz.Modules.Payments.Domain.Payments;
using DropUz.Modules.Payments.Infrastructure.Database;
using DropUz.Modules.Payments.Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PaymentsApplication = DropUz.Modules.Payments.Application.AssemblyReference;
using PaymentsPresentation = DropUz.Modules.Payments.Presentation.AssemblyReference;

namespace DropUz.Modules.Payments.Infrastructure;

public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(PaymentsApplication.Assembly));

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IMainDbContextModelContributor, PaymentsModelContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPaymentProvider, ManualPaymentProvider>());
        services.TryAddScoped<IPaymentProviderRegistry, PaymentProviderRegistry>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IDomainEventHandler<ProductPaymentCompletedDomainEvent>,
            ProductPaymentCompletedDomainEventHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IDomainEventHandler<CargoPaymentCompletedDomainEvent>,
            CargoPaymentCompletedDomainEventHandler>());
        services.AddEndpoints(PaymentsPresentation.Assembly);

        return services;
    }
}
