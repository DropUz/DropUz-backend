namespace DropUz.Common.Application.Pagination;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount);
