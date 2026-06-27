using DropUz.Common.Application.EventBus;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using DropUz.Common.Infrastructure.Data;
using DropUz.Common.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DropUz.Common.Tests.Infrastructure;

public sealed class OutboxProcessorTests
{
    [Fact]
    public async Task ProcessorDispatchesPendingDomainEventAndMarksMessageProcessed()
    {
        await using MainDbContext context = CreateContext();
        var domainEvent = new TestOutboxDomainEvent(Guid.NewGuid());
        context.Set<OutboxMessage>().Add(OutboxMessage.FromDomainEvent(domainEvent));
        await context.SaveChangesAsync();
        var handler = new CapturingDomainEventHandler();
        var processor = CreateProcessor(context, services =>
        {
            services.AddSingleton<IDomainEventHandler<TestOutboxDomainEvent>>(handler);
        });

        int processedCount = await processor.ProcessPendingAsync(batchSize: 10, CancellationToken.None);

        OutboxMessage message = Assert.Single(context.Set<OutboxMessage>());
        Assert.Equal(1, processedCount);
        Assert.Single(handler.HandledEvents);
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.Null(message.Error);
    }

    [Fact]
    public async Task ProcessorDispatchesPendingIntegrationEventAndMarksMessageProcessed()
    {
        await using MainDbContext context = CreateContext();
        var integrationEvent = new TestOutboxIntegrationEvent(Guid.NewGuid());
        context.Set<OutboxMessage>().Add(OutboxMessage.FromIntegrationEvent(integrationEvent));
        await context.SaveChangesAsync();
        var handler = new CapturingIntegrationEventHandler();
        var processor = CreateProcessor(context, services =>
        {
            services.AddSingleton<IIntegrationEventHandler<TestOutboxIntegrationEvent>>(handler);
        });

        int processedCount = await processor.ProcessPendingAsync(batchSize: 10, CancellationToken.None);

        OutboxMessage message = Assert.Single(context.Set<OutboxMessage>());
        Assert.Equal(1, processedCount);
        Assert.Single(handler.HandledEvents);
        Assert.NotNull(message.ProcessedOnUtc);
        Assert.Null(message.Error);
    }

    [Fact]
    public async Task ProcessorMarksFailedMessageAndContinuesWhenHandlerThrows()
    {
        await using MainDbContext context = CreateContext();
        context.Set<OutboxMessage>().Add(OutboxMessage.FromDomainEvent(new TestOutboxDomainEvent(Guid.NewGuid())));
        await context.SaveChangesAsync();
        var processor = CreateProcessor(context, services =>
        {
            services.AddSingleton<IDomainEventHandler<TestOutboxDomainEvent>>(new ThrowingDomainEventHandler());
        });

        int processedCount = await processor.ProcessPendingAsync(batchSize: 10, CancellationToken.None);

        OutboxMessage message = Assert.Single(context.Set<OutboxMessage>());
        Assert.Equal(0, processedCount);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(1, message.RetryCount);
        Assert.Contains("boom", message.Error);
    }

    [Fact]
    public async Task ProcessorMarksFailedMessageWhenHandlerIsNotRegistered()
    {
        await using MainDbContext context = CreateContext();
        context.Set<OutboxMessage>().Add(OutboxMessage.FromDomainEvent(new TestOutboxDomainEvent(Guid.NewGuid())));
        await context.SaveChangesAsync();
        var processor = CreateProcessor(context, _ => { });

        int processedCount = await processor.ProcessPendingAsync(batchSize: 10, CancellationToken.None);

        OutboxMessage message = Assert.Single(context.Set<OutboxMessage>());
        Assert.Equal(0, processedCount);
        Assert.Null(message.ProcessedOnUtc);
        Assert.Equal(1, message.RetryCount);
        Assert.Contains("No handlers registered", message.Error);
    }

    private static MainDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MainDbContext(options, [new CommonTestModelContributor()]);
    }

    private static OutboxMessageProcessor CreateProcessor(
        MainDbContext context,
        Action<IServiceCollection> configureServices)
    {
        var services = new ServiceCollection();
        configureServices(services);
        ServiceProvider provider = services.BuildServiceProvider();

        return new OutboxMessageProcessor(
            context,
            new OutboxMessageDispatcher(provider, new OutboxMessageTypeResolver()),
            new TestDateTimeProvider(new DateTime(2026, 06, 26, 10, 0, 0, DateTimeKind.Utc)),
            NullLogger<OutboxMessageProcessor>.Instance);
    }

    public sealed record TestOutboxDomainEvent(Guid EntityId) : DomainEvent;

    public sealed record TestOutboxIntegrationEvent(Guid EntityId) : IntegrationEvent;

    private sealed class CapturingDomainEventHandler : IDomainEventHandler<TestOutboxDomainEvent>
    {
        public List<TestOutboxDomainEvent> HandledEvents { get; } = [];

        public Task Handle(TestOutboxDomainEvent notification, CancellationToken cancellationToken)
        {
            HandledEvents.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingDomainEventHandler : IDomainEventHandler<TestOutboxDomainEvent>
    {
        public Task Handle(TestOutboxDomainEvent notification, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("boom");
        }
    }

    private sealed class CapturingIntegrationEventHandler : IIntegrationEventHandler<TestOutboxIntegrationEvent>
    {
        public List<TestOutboxIntegrationEvent> HandledEvents { get; } = [];

        public Task Handle(TestOutboxIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            HandledEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }
}
