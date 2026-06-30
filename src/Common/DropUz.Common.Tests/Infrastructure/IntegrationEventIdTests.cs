using DropUz.Common.Application.EventBus;
using Xunit;

namespace DropUz.Common.Tests.Infrastructure;

public sealed class IntegrationEventIdTests
{
    [Fact]
    public void CreateIsDeterministicPerSourceEventAndIntegrationEventType()
    {
        Guid sourceEventId = Guid.NewGuid();

        Guid first = IntegrationEventId.Create<FirstIntegrationEvent>(sourceEventId);
        Guid second = IntegrationEventId.Create<FirstIntegrationEvent>(sourceEventId);
        Guid differentType = IntegrationEventId.Create<SecondIntegrationEvent>(sourceEventId);
        Guid differentSource = IntegrationEventId.Create<FirstIntegrationEvent>(Guid.NewGuid());

        Assert.Equal(first, second);
        Assert.NotEqual(sourceEventId, first);
        Assert.NotEqual(first, differentType);
        Assert.NotEqual(first, differentSource);
    }

    private sealed record FirstIntegrationEvent : IntegrationEvent;

    private sealed record SecondIntegrationEvent : IntegrationEvent;
}
