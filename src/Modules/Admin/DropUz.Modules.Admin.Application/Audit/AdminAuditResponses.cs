namespace DropUz.Modules.Admin.Application.Audit;

public sealed record AdminAuditLogResponse(
    Guid Id,
    Guid? AdminUserId,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? Details,
    DateTime CreatedAtUtc);
