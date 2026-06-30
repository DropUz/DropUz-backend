using DropUz.Common.Application.Data;
using DropUz.Common.Application.EventBus;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Modules.Orders.IntegrationEvents;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Admin;

public sealed class OrderStatusChangedAuditIntegrationEventTests
{
    [Fact]
    public async Task ConsumerUsesInboxToRecordAttributedAuditOnce()
    {
        DateTime changedAtUtc = new(2026, 06, 28, 12, 0, 0, DateTimeKind.Utc);
        Guid adminUserId = Guid.NewGuid();
        var repository = new InMemoryMainRepository();
        var inbox = new InMemoryIntegrationEventInbox();
        var handler = new OrderStatusChangedIntegrationEventHandler(repository, inbox);
        var integrationEvent = new OrderStatusChangedIntegrationEvent(
            SourceEventId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            SellerId: Guid.NewGuid(),
            OrderNumber: "DUZ-STATUS-2",
            SellerProfitTotal: 20m,
            PreviousStatus: "ProductPaid",
            NewStatus: "Purchasing",
            Note: "Buying product",
            ChangedByUserId: adminUserId,
            changedAtUtc);

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        AdminAuditLog auditLog = Assert.Single(repository.Entities.OfType<AdminAuditLog>());
        Assert.Equal(adminUserId, auditLog.AdminUserId);
        Assert.Equal(AdminAuditActions.Orders.StatusUpdated, auditLog.Action);
        Assert.Equal("Order", auditLog.EntityType);
        Assert.Equal(integrationEvent.OrderId, auditLog.EntityId);
        Assert.Equal("from=ProductPaid;to=Purchasing;note=Buying product", auditLog.Details);
        Assert.Equal(changedAtUtc, auditLog.CreatedAtUtc);
        Assert.Contains(OrderStatusChangedIntegrationEventHandler.ConsumerName, inbox.StartedConsumerNames);
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
