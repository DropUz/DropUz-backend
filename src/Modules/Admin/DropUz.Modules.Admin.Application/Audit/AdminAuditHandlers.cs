using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Admin.Application.Audit;

public sealed class GetAdminAuditLogsQueryHandler(IMainRepository repository)
    : IQueryHandler<GetAdminAuditLogsQuery, PagedResponse<AdminAuditLogResponse>>
{
    public async Task<Result<PagedResponse<AdminAuditLogResponse>>> Handle(
        GetAdminAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<AdminAuditLog> query = repository.Query<AdminAuditLog>();

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            string action = request.Action.Trim();
            query = query.Where(auditLog => auditLog.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            string entityType = request.EntityType.Trim();
            query = query.Where(auditLog => auditLog.EntityType == entityType);
        }

        if (request.EntityId.HasValue)
        {
            Guid entityId = request.EntityId.Value;
            query = query.Where(auditLog => auditLog.EntityId == entityId);
        }

        PageRequest pageRequest = request.Page;
        int totalCount = await query.CountAsync(cancellationToken);
        List<AdminAuditLogResponse> items = await query
            .OrderByDescending(auditLog => auditLog.CreatedAtUtc)
            .Skip(pageRequest.Skip)
            .Take(pageRequest.NormalizedPageSize)
            .Select(auditLog => new AdminAuditLogResponse(
                auditLog.Id,
                auditLog.AdminUserId,
                auditLog.Action,
                auditLog.EntityType,
                auditLog.EntityId,
                auditLog.Details,
                auditLog.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResponse<AdminAuditLogResponse>(
            items,
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize,
            totalCount));
    }
}
