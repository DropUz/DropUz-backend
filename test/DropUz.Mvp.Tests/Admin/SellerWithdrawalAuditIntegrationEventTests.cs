using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Sellers.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Admin;

public sealed class SellerWithdrawalAuditIntegrationEventTests
{
    [Fact]
    public async Task ConsumerRecordsAttributedWithdrawalAuditOnce()
    {
        DateTime recordedAtUtc = new(2026, 06, 29, 19, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var repository = new InMemoryMainRepository();
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new SellerWithdrawalRecordedIntegrationEventHandler(repository, inbox);
        var integrationEvent = new SellerWithdrawalRecordedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            80m,
            "Manual payout",
            actorUserId,
            recordedAtUtc);

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        AdminAuditLog auditLog = Assert.Single(repository.Entities.OfType<AdminAuditLog>());
        Assert.Equal(actorUserId, auditLog.AdminUserId);
        Assert.Equal(AdminAuditActions.Sellers.WithdrawalRecorded, auditLog.Action);
        Assert.Equal("SellerProfile", auditLog.EntityType);
        Assert.Equal(integrationEvent.SellerId, auditLog.EntityId);
        Assert.Equal("amount=80;note=Manual payout", auditLog.Details);
        Assert.Equal(recordedAtUtc, auditLog.CreatedAtUtc);
        Assert.Contains(SellerWithdrawalRecordedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
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
}
