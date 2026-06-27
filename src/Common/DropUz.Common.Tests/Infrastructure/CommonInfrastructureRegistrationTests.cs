using DropUz.Common.Infrastructure;
using DropUz.Common.Infrastructure.Inbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DropUz.Common.Tests.Infrastructure;

public sealed class CommonInfrastructureRegistrationTests
{
    [Fact]
    public void CommonInfrastructureRegistersOutboxProcessorHostedService()
    {
        var configuration = new ConfigurationManager
        {
            ["ConnectionStrings:Database"] = "Host=localhost;Database=dropuz;Username=postgres;Password=postgres"
        };
        var services = new ServiceCollection();

        services.AddDropUzCommonInfrastructure(configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IHostedService) &&
            descriptor.ImplementationType?.Name == "OutboxMessageProcessorHostedService");
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(InboxMessageService));
    }
}
