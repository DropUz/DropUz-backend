using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Admin.Domain.Audit;
using DropUz.Mvp.Tests.Support;
using Xunit;

namespace DropUz.Mvp.Tests.Admin;

public sealed class AdminAuditServiceTests
{
    [Fact]
    public async Task AuditServiceRecordsCurrentAdminAction()
    {
        DateTime nowUtc = new(2026, 06, 23, 11, 0, 0, DateTimeKind.Utc);
        var adminUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var repository = new InMemoryMainRepository();
        var service = new AdminAuditService(
            repository,
            new TestCurrentUser(adminUserId),
            new TestDateTimeProvider(nowUtc));

        await service.RecordAsync(
            AdminAuditActions.Orders.StatusUpdated,
            entityType: "Order",
            entityId: orderId,
            details: "status=Delivered",
            CancellationToken.None);

        AdminAuditLog auditLog = Assert.Single(repository.Entities.OfType<AdminAuditLog>());
        Assert.Equal(adminUserId, auditLog.AdminUserId);
        Assert.Equal(AdminAuditActions.Orders.StatusUpdated, auditLog.Action);
        Assert.Equal("Order", auditLog.EntityType);
        Assert.Equal(orderId, auditLog.EntityId);
        Assert.Equal("status=Delivered", auditLog.Details);
        Assert.Equal(nowUtc, auditLog.CreatedAtUtc);
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid? UserId => userId;

        public string? UserName => "admin-user";

        public bool IsAuthenticated => true;

        public IReadOnlyCollection<string> Roles => ["admin"];
    }

    private sealed class TestDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow => utcNow;

        public DateTimeOffset OffsetUtcNow => new(utcNow);
    }
}
