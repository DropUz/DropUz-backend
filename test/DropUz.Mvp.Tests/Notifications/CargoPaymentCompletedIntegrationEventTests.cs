using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Notifications.Application.Notifications;
using DropUz.Modules.Notifications.Domain.Notifications;
using DropUz.Modules.Payments.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Notifications;

public sealed class CargoPaymentCompletedIntegrationEventTests
{
    [Fact]
    public async Task ConsumerUsesInboxToCreateCargoPaymentNotificationOnce()
    {
        DateTime paidAtUtc = new(2026, 06, 28, 17, 30, 0, DateTimeKind.Utc);
        var repository = new InMemoryMainRepository();
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new CargoPaymentCompletedIntegrationEventHandler(
            repository,
            new InMemoryNotificationService(repository, new TestDateTimeProvider(paidAtUtc)),
            inbox);
        var integrationEvent = new CargoPaymentCompletedIntegrationEvent(
            SourceEventId: Guid.NewGuid(),
            PaymentId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Amount: 45m,
            OrderNumber: "DUZ-CARGO-PAYMENT-1",
            paidAtUtc,
            ProviderTransactionId: "cargo-provider-2");

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        NotificationMessage notification = Assert.Single(repository.Entities.OfType<NotificationMessage>());
        Assert.Equal(integrationEvent.UserId, notification.UserId);
        Assert.Equal(integrationEvent.OrderId, notification.OrderId);
        Assert.Equal(NotificationType.PaymentReceived, notification.Type);
        Assert.Contains(integrationEvent.OrderNumber, notification.Body);
        Assert.Contains(CargoPaymentCompletedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
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
