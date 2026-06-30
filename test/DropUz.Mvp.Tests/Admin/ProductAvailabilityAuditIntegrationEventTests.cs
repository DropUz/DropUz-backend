using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Catalog.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Admin;

public sealed class ProductAvailabilityAuditIntegrationEventTests
{
    [Theory]
    [InlineData("Approved", "Inactive", AdminAuditActions.Catalog.ProductDeactivated)]
    [InlineData("Inactive", "Approved", AdminAuditActions.Catalog.ProductActivated)]
    [InlineData("Approved", "Deleted", AdminAuditActions.Catalog.ProductDeleted)]
    public async Task ConsumerRecordsAttributedAvailabilityAuditOnce(
        string previousStatus,
        string newStatus,
        string expectedAction)
    {
        DateTime changedAtUtc = new(2026, 06, 30, 22, 0, 0, DateTimeKind.Utc);
        Guid actorUserId = Guid.NewGuid();
        var repository = new InMemoryMainRepository();
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new ProductAvailabilityChangedIntegrationEventHandler(repository, inbox);
        var integrationEvent = new ProductAvailabilityChangedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            previousStatus,
            newStatus,
            actorUserId,
            changedAtUtc);

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        AdminAuditLog auditLog = Assert.Single(repository.Entities.OfType<AdminAuditLog>());
        Assert.Equal(actorUserId, auditLog.AdminUserId);
        Assert.Equal(expectedAction, auditLog.Action);
        Assert.Equal("CatalogProduct", auditLog.EntityType);
        Assert.Equal(integrationEvent.ProductId, auditLog.EntityId);
        Assert.Equal($"from={previousStatus};to={newStatus}", auditLog.Details);
        Assert.Equal(changedAtUtc, auditLog.CreatedAtUtc);
        Assert.Contains(ProductAvailabilityChangedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
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
