namespace DropUz.Common.Application.Pagination;

public interface IPagedQuery
{
    int PageNumber { get; }

    int PageSize { get; }
}
