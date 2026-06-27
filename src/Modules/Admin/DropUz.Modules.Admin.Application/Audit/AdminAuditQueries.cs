using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;

namespace DropUz.Modules.Admin.Application.Audit;

public sealed record GetAdminAuditLogsQuery(
    PageRequest Page,
    string? Action = null,
    string? EntityType = null,
    Guid? EntityId = null) : IQuery<PagedResponse<AdminAuditLogResponse>>;
