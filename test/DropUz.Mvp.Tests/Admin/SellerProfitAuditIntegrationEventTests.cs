using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Sellers.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Admin;

public sealed class SellerProfitAuditIntegrationEventTests
{
    [Fact]
    public async Task PendingProfitConsumerRecordsFinancialAuditOnce()
    {
        DateTime createdAtUtc = new(2026, 06, 30, 0, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryMainRepository();
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new SellerProfitPendingCreatedIntegrationEventHandler(repository, inbox);
        var integrationEvent = new SellerProfitPendingCreatedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            120m,
            createdAtUtc);

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        AdminAuditLog auditLog = Assert.Single(repository.Entities.OfType<AdminAuditLog>());
        Assert.Null(auditLog.AdminUserId);
        Assert.Equal(AdminAuditActions.Sellers.ProfitPendingCreated, auditLog.Action);
        Assert.Equal("SellerProfile", auditLog.EntityType);
        Assert.Equal(integrationEvent.SellerId, auditLog.EntityId);
        Assert.Equal($"orderId={integrationEvent.OrderId};amount=120", auditLog.Details);
        Assert.Equal(createdAtUtc, auditLog.CreatedAtUtc);
        Assert.Contains(SellerProfitPendingCreatedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
    }

    [Fact]
    public async Task AvailableProfitConsumerRecordsFinancialAuditOnce()
    {
        DateTime availableAtUtc = new(2026, 06, 30, 1, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryMainRepository();
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new SellerProfitAvailableIntegrationEventHandler(repository, inbox);
        var integrationEvent = new SellerProfitAvailableIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            120m,
            availableAtUtc);

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        AdminAuditLog auditLog = Assert.Single(repository.Entities.OfType<AdminAuditLog>());
        Assert.Null(auditLog.AdminUserId);
        Assert.Equal(AdminAuditActions.Sellers.ProfitAvailable, auditLog.Action);
        Assert.Equal("SellerProfile", auditLog.EntityType);
        Assert.Equal(integrationEvent.SellerId, auditLog.EntityId);
        Assert.Equal($"orderId={integrationEvent.OrderId};amount=120", auditLog.Details);
        Assert.Equal(availableAtUtc, auditLog.CreatedAtUtc);
        Assert.Contains(SellerProfitAvailableIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
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
