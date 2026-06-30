using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Catalog.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Admin;

public sealed class CatalogProductLifecycleAuditIntegrationEventTests
{
    [Fact]
    public async Task ImportedConsumerRecordsAttributedAuditOnce()
    {
        DateTime importedAtUtc = new(2026, 06, 29, 12, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var repository = new InMemoryMainRepository();
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new ProductImportedIntegrationEventHandler(repository, inbox);
        var integrationEvent = new ProductImportedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "taobao",
            "TB-AUDIT-1",
            actorUserId,
            importedAtUtc);

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        AdminAuditLog auditLog = Assert.Single(repository.Entities.OfType<AdminAuditLog>());
        Assert.Equal(actorUserId, auditLog.AdminUserId);
        Assert.Equal(AdminAuditActions.Catalog.ProductImported, auditLog.Action);
        Assert.Equal("CatalogProduct", auditLog.EntityType);
        Assert.Equal(integrationEvent.ProductId, auditLog.EntityId);
        Assert.Equal("source=taobao:TB-AUDIT-1", auditLog.Details);
        Assert.Equal(importedAtUtc, auditLog.CreatedAtUtc);
        Assert.Contains(ProductImportedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
    }

    [Fact]
    public async Task ApprovedConsumerRecordsAttributedAuditOnce()
    {
        DateTime approvedAtUtc = new(2026, 06, 29, 13, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var repository = new InMemoryMainRepository();
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new ProductApprovedIntegrationEventHandler(repository, inbox);
        var integrationEvent = new ProductApprovedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            actorUserId,
            approvedAtUtc);

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        AdminAuditLog auditLog = Assert.Single(repository.Entities.OfType<AdminAuditLog>());
        Assert.Equal(actorUserId, auditLog.AdminUserId);
        Assert.Equal(AdminAuditActions.Catalog.ProductApproved, auditLog.Action);
        Assert.Equal("CatalogProduct", auditLog.EntityType);
        Assert.Equal(integrationEvent.ProductId, auditLog.EntityId);
        Assert.Null(auditLog.Details);
        Assert.Equal(approvedAtUtc, auditLog.CreatedAtUtc);
        Assert.Contains(ProductApprovedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
    }

    [Fact]
    public async Task RejectedConsumerRecordsAttributedAuditOnce()
    {
        DateTime rejectedAtUtc = new(2026, 06, 29, 14, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var repository = new InMemoryMainRepository();
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new ProductRejectedIntegrationEventHandler(repository, inbox);
        var integrationEvent = new ProductRejectedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            actorUserId,
            rejectedAtUtc);

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        AdminAuditLog auditLog = Assert.Single(repository.Entities.OfType<AdminAuditLog>());
        Assert.Equal(actorUserId, auditLog.AdminUserId);
        Assert.Equal(AdminAuditActions.Catalog.ProductRejected, auditLog.Action);
        Assert.Equal("CatalogProduct", auditLog.EntityType);
        Assert.Equal(integrationEvent.ProductId, auditLog.EntityId);
        Assert.Null(auditLog.Details);
        Assert.Equal(rejectedAtUtc, auditLog.CreatedAtUtc);
        Assert.Contains(ProductRejectedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
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
