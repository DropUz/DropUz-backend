using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Notifications;

public sealed class CargoPriceAddedIntegrationEventTests
{
    [Fact]
    public async Task ConsumerUsesInboxToCreateNotificationOnce()
    {
        DateTime addedAtUtc = new(2026, 06, 27, 21, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryMainRepository();
        var inbox = new InMemoryIntegrationEventInbox();
        var notificationService = new InMemoryNotificationService(
            repository,
            new TestDateTimeProvider(addedAtUtc));
        var handler = new CargoPriceAddedIntegrationEventHandler(
            repository,
            notificationService,
            inbox);
        var integrationEvent = new CargoPriceAddedIntegrationEvent(
            SourceEventId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            CargoPrice: 31m,
            DeadlineAtUtc: addedAtUtc.AddDays(6),
            addedAtUtc);

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        NotificationMessage notification = Assert.Single(repository.Entities.OfType<NotificationMessage>());
        Assert.Equal(integrationEvent.UserId, notification.UserId);
        Assert.Equal(integrationEvent.OrderId, notification.OrderId);
        Assert.Equal(NotificationType.CargoPriceAdded, notification.Type);
        Assert.Contains("31", notification.Body);
        Assert.Contains(integrationEvent.DeadlineAtUtc.ToString("O"), notification.Body);
        Assert.Contains(integrationEvent.Id, inbox.ProcessedMessageIds);
        Assert.Contains(CargoPriceAddedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
        Assert.Empty(inbox.FailedMessageIds);
    }

    private sealed class InMemoryIntegrationEventInbox : IIntegrationEventInbox
    {
        public HashSet<Guid> ProcessedMessageIds { get; } = [];

        public HashSet<Guid> FailedMessageIds { get; } = [];

        public HashSet<string> StartedConsumerNames { get; } = [];

        public Task<bool> TryStartProcessingAsync(
            IIntegrationEvent integrationEvent,
            string consumerName,
            CancellationToken cancellationToken = default)
        {
            StartedConsumerNames.Add(consumerName);
            return Task.FromResult(!ProcessedMessageIds.Contains(integrationEvent.Id));
        }

        public Task MarkProcessedAsync(
            IIntegrationEvent integrationEvent,
            string consumerName,
            CancellationToken cancellationToken = default)
        {
            ProcessedMessageIds.Add(integrationEvent.Id);
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(
            IIntegrationEvent integrationEvent,
            string consumerName,
            string error,
            CancellationToken cancellationToken = default)
        {
            FailedMessageIds.Add(integrationEvent.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
    }

    private sealed class InMemoryNotificationService(
        IMainRepository repository,
        IDateTimeProvider dateTimeProvider) : INotificationService
    {
        public async Task EnqueueAsync(
            Guid userId,
            Guid? orderId,
            NotificationType type,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            await repository.AddAsync(NotificationMessage.Create(
                userId,
                orderId,
                type,
                NotificationChannel.Email,
                userId.ToString(),
                subject,
                body,
                dateTimeProvider.UtcNow));
        }
    }
}
