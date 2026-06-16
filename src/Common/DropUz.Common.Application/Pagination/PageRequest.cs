namespace DropUz.Common.Application.Pagination;

public sealed record PageRequest(int PageNumber = 1, int PageSize = 20)
{
    public int Skip => (NormalizedPageNumber - 1) * NormalizedPageSize;

    public int NormalizedPageNumber => PageNumber < 1 ? 1 : PageNumber;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => 20,
        > 200 => 200,
        _ => PageSize
    };
}
