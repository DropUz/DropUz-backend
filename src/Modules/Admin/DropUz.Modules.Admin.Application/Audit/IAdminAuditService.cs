namespace DropUz.Modules.Admin.Application.Audit;

public interface IAdminAuditService
{
    Task RecordAsync(
        string action,
        string entityType,
        Guid? entityId = null,
        string? details = null,
        CancellationToken cancellationToken = default);
}
