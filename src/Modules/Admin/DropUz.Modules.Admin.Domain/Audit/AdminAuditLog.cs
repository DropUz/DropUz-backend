using DropUz.Common.Domain;

namespace DropUz.Modules.Admin.Domain.Audit;

public sealed class AdminAuditLog : Entity
{
    private AdminAuditLog()
    {
    }

    private AdminAuditLog(
        Guid id,
        Guid? adminUserId,
        string action,
        string entityType,
        Guid? entityId,
        string? details,
        DateTime createdAtUtc)
        : base(id)
    {
        AdminUserId = adminUserId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Details = details;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid? AdminUserId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public Guid? EntityId { get; private set; }

    public string? Details { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static AdminAuditLog Create(
        Guid? adminUserId,
        string action,
        string entityType,
        Guid? entityId,
        string? details,
        DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        return new AdminAuditLog(
            Guid.NewGuid(),
            adminUserId,
            action.Trim(),
            entityType.Trim(),
            entityId,
            string.IsNullOrWhiteSpace(details) ? null : details.Trim(),
            createdAtUtc);
    }
}
