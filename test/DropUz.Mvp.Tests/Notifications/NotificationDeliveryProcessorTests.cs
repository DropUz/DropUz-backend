using DropUz.Common.Application.Clock;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Notifications.Application;
using DropUz.Modules.Notifications.Application.Delivery;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Notifications.Infrastructure;
using DropUz.Modules.Notifications.Infrastructure.Delivery;
using DropUz.Mvp.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DropUz.Mvp.Tests.Notifications;

public sealed class NotificationDeliveryProcessorTests
{
    [Fact]
    public async Task ProcessorSendsPendingMessageAndStoresProviderMetadata()
    {
        DateTime nowUtc = new(2026, 06, 30, 23, 0, 0, DateTimeKind.Utc);
        NotificationMessage message = CreateMessage(nowUtc.AddMinutes(-1));
        var repository = new InMemoryMainRepository(message);
        var provider = new StubDeliveryProvider(NotificationDeliveryResult.Succeeded("provider-message-1"));
        var processor = new NotificationDeliveryProcessor(
            repository,
            new TestDateTimeProvider(nowUtc),
            new StubDeliveryProviderRegistry(provider),
            NullLogger<NotificationDeliveryProcessor>.Instance);

        int processed = await processor.ProcessPendingAsync(20, CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal(1, provider.CallCount);
        Assert.Equal(message.Id, provider.LastRequest?.IdempotencyKey);
        Assert.Equal(NotificationStatus.Sent, message.Status);
        Assert.Equal(1, message.AttemptCount);
        Assert.Equal(nowUtc, message.LastAttemptAtUtc);
        Assert.Equal("stub", message.ProviderName);
        Assert.Equal("provider-message-1", message.ProviderMessageId);
    }

    [Fact]
    public async Task ProcessorMarksProviderFailureAndManualRetryRequeuesOnlyFailedMessage()
    {
        DateTime nowUtc = new(2026, 06, 30, 23, 30, 0, DateTimeKind.Utc);
        NotificationMessage message = CreateMessage(nowUtc.AddMinutes(-1));
        var processor = new NotificationDeliveryProcessor(
            new InMemoryMainRepository(message),
            new TestDateTimeProvider(nowUtc),
            new StubDeliveryProviderRegistry(
                new StubDeliveryProvider(NotificationDeliveryResult.Failed("smtp unavailable"))),
            NullLogger<NotificationDeliveryProcessor>.Instance);

        await processor.ProcessPendingAsync(20, CancellationToken.None);
        bool retried = message.Retry();
        bool retriedAgain = message.Retry();

        Assert.True(retried);
        Assert.False(retriedAgain);
        Assert.Equal(NotificationStatus.Pending, message.Status);
        Assert.Equal(1, message.AttemptCount);
        Assert.Null(message.FailureReason);
    }

    [Fact]
    public void SentMessageCannotBeRetried()
    {
        DateTime nowUtc = new(2026, 06, 30, 23, 45, 0, DateTimeKind.Utc);
        NotificationMessage message = CreateMessage(nowUtc.AddMinutes(-1));
        message.MarkSent(nowUtc, "stub", "provider-message-2");

        bool retried = message.Retry();

        Assert.False(retried);
        Assert.Equal(NotificationStatus.Sent, message.Status);
    }

    [Fact]
    public async Task RetryCommandRejectsSentMessage()
    {
        DateTime nowUtc = new(2026, 06, 30, 23, 50, 0, DateTimeKind.Utc);
        NotificationMessage message = CreateMessage(nowUtc.AddMinutes(-1));
        message.MarkSent(nowUtc, "stub", "provider-message-3");
        var handler = new RetryNotificationCommandHandler(
            new InMemoryMainRepository(message),
            new NoOpAdminAuditService());

        Result<NotificationResponse> result = await handler.Handle(
            new RetryNotificationCommand(message.Id),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(NotificationErrors.NotificationRetryInvalid, result.Error);
        Assert.Equal(NotificationStatus.Sent, message.Status);
    }

    [Theory]
    [InlineData(NotificationChannel.Telegram)]
    [InlineData(NotificationChannel.Email)]
    public void NotificationsModuleRegistersMockFallbackProvider(NotificationChannel channel)
    {
        var services = new ServiceCollection();
        services.AddNotificationsModule();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        INotificationDeliveryProviderRegistry registry =
            scope.ServiceProvider.GetRequiredService<INotificationDeliveryProviderRegistry>();

        INotificationDeliveryProvider provider = Assert.IsAssignableFrom<INotificationDeliveryProvider>(
            registry.GetProvider(channel));
        Assert.Equal("mock", provider.Name);
    }

    private static NotificationMessage CreateMessage(DateTime createdAtUtc)
    {
        return NotificationMessage.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationType.OrderStatusChanged,
            NotificationChannel.Email,
            "user@example.test",
            "Order updated",
            "Your order status changed.",
            createdAtUtc);
    }

    private sealed class StubDeliveryProvider(NotificationDeliveryResult result)
        : INotificationDeliveryProvider
    {
        public string Name => "stub";

        public NotificationChannel? Channel => NotificationChannel.Email;

        public int CallCount { get; private set; }

        public NotificationDeliveryRequest? LastRequest { get; private set; }

        public Task<NotificationDeliveryResult> SendAsync(
            NotificationDeliveryRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(result);
        }
    }

    private sealed class StubDeliveryProviderRegistry(INotificationDeliveryProvider provider)
        : INotificationDeliveryProviderRegistry
    {
        public INotificationDeliveryProvider? GetProvider(NotificationChannel channel) => provider;
    }

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
    }

    private sealed class NoOpAdminAuditService : IAdminAuditService
    {
        public Task RecordAsync(
            string action,
            string entityType,
            Guid? entityId = null,
            string? details = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
