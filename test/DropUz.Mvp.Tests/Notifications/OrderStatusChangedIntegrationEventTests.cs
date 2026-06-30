using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Orders.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Notifications;

public sealed class OrderStatusChangedIntegrationEventTests
{
    [Fact]
    public async Task ConsumerUsesInboxToCreateStatusNotificationOnce()
    {
        DateTime changedAtUtc = new(2026, 06, 28, 11, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryMainRepository();
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new OrderStatusChangedIntegrationEventHandler(
            repository,
            new InMemoryNotificationService(repository, new TestDateTimeProvider(changedAtUtc)),
            inbox);
        OrderStatusChangedIntegrationEvent integrationEvent = CreateEvent(changedAtUtc);

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        NotificationMessage notification = Assert.Single(repository.Entities.OfType<NotificationMessage>());
        Assert.Equal(integrationEvent.UserId, notification.UserId);
        Assert.Equal(integrationEvent.OrderId, notification.OrderId);
        Assert.Equal(NotificationType.OrderStatusChanged, notification.Type);
        Assert.Contains(integrationEvent.OrderNumber, notification.Body);
        Assert.Contains("Purchasing", notification.Body);
        Assert.Contains(OrderStatusChangedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
    }

    private static OrderStatusChangedIntegrationEvent CreateEvent(DateTime changedAtUtc)
    {
        return new OrderStatusChangedIntegrationEvent(
            SourceEventId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            SellerId: Guid.NewGuid(),
            OrderNumber: "DUZ-STATUS-1",
            SellerProfitTotal: 20m,
            PreviousStatus: "ProductPaid",
            NewStatus: "Purchasing",
            Note: "Buying product",
            ChangedByUserId: Guid.NewGuid(),
            changedAtUtc);
    }

    private sealed class InMemoryIntegrationEventInbox : IIntegrationEventInbox
    {
        private readonly HashSet<(Guid MessageId, string ConsumerName)> _processed = [];

        public HashSet<string> StartedConsumerNames { get; } = [];

        public Task<bool> TryStartProcessingAsync(
            IIntegrationEvent integrationEvent,
            string consumerName,
            CancellationToken cancellationToken = default)
        {
            StartedConsumerNames.Add(consumerName);
            return Task.FromResult(!_processed.Contains((integrationEvent.Id, consumerName)));
        }

        public Task MarkProcessedAsync(
            IIntegrationEvent integrationEvent,
            string consumerName,
            CancellationToken cancellationToken = default)
        {
            _processed.Add((integrationEvent.Id, consumerName));
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(
            IIntegrationEvent integrationEvent,
            string consumerName,
            string error,
            CancellationToken cancellationToken = default)
        {
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
