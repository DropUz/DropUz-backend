using DropUz.Modules.Cargo.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DropUz.Mvp.Tests.Cargo;

public sealed class CargoModuleRegistrationTests
{
    [Fact]
    public void CargoModuleRegistersPaymentExpirationBackgroundJob()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddCargoModule();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
    }
}
