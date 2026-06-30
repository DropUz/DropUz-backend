using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;
using DropUz.Common.Domain;
using DropUz.Modules.Catalog.Domain.Imports;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Catalog.Application.Imports;

public sealed class GetCatalogImportLogsQueryHandler(IMainRepository repository)
    : IQueryHandler<GetCatalogImportLogsQuery, PagedResponse<CatalogImportLogResponse>>
{
    public async Task<Result<PagedResponse<CatalogImportLogResponse>>> Handle(
        GetCatalogImportLogsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<CatalogImportLog> query = repository.Query<CatalogImportLog>();

        if (!string.IsNullOrWhiteSpace(request.SourcePlatform))
        {
            string sourcePlatform = request.SourcePlatform.Trim();
            query = query.Where(importLog => importLog.SourcePlatform == sourcePlatform);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(importLog => importLog.Status == request.Status.Value);
        }

        PageRequest pageRequest = request.Page;
        int totalCount = await query.CountAsync(cancellationToken);
        List<CatalogImportLogResponse> items = await query
            .OrderByDescending(importLog => importLog.CompletedAtUtc)
            .Skip(pageRequest.Skip)
            .Take(pageRequest.NormalizedPageSize)
            .Select(importLog => new CatalogImportLogResponse(
                importLog.Id,
                importLog.SourcePlatform,
                importLog.SourceProductId,
                importLog.ProviderName,
                importLog.Status,
                importLog.CatalogProductId,
                importLog.Operation,
                importLog.ErrorCode,
                importLog.ErrorMessage,
                importLog.RequestedByUserId,
                importLog.CompletedAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResponse<CatalogImportLogResponse>(
            items,
            pageRequest.NormalizedPageNumber,
            pageRequest.NormalizedPageSize,
            totalCount));
    }
}
