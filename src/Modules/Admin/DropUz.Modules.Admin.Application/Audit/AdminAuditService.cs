using DropUz.Common.Application.Abstractions;
using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Modules.Admin.Domain.Audit;

namespace DropUz.Modules.Admin.Application.Audit;

public sealed class AdminAuditService(
    IMainRepository repository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider) : IAdminAuditService
{
    public async Task RecordAsync(
        string action,
        string entityType,
        Guid? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        await repository.AddAsync(AdminAuditLog.Create(
            currentUser.UserId,
            action,
            entityType,
            entityId,
            details,
            dateTimeProvider.UtcNow));
    }
}
