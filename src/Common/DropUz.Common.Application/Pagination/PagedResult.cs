namespace DropUz.Common.Application.Pagination;

public sealed record PagedResult<TItem>(
    IReadOnlyList<TItem> Items,
    int TotalCount);
