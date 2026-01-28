namespace Gateway.Core.Pagination;

/// <summary>
/// Represents a paged result set with metadata.
/// </summary>
public class PagedResult<T>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// The current page number (1-based).
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// The total count of items across all pages. Null if not requested.
    /// </summary>
    public int? TotalCount { get; }

    /// <summary>
    /// The total number of pages. Null if TotalCount is not available.
    /// </summary>
    public int? TotalPages => TotalCount.HasValue && PageSize > 0
        ? (int)Math.Ceiling((double)TotalCount.Value / PageSize)
        : null;

    /// <summary>
    /// Whether there is a previous page available.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page available.
    /// </summary>
    public bool HasNextPage
    {
        get
        {
            if (TotalPages.HasValue)
            {
                return PageNumber < TotalPages.Value;
            }
            // If total count is not available, check if we have more items
            return _hasMoreItems;
        }
    }

    private readonly bool _hasMoreItems;

    public PagedResult(
        IReadOnlyList<T> items,
        int pageNumber,
        int pageSize,
        int? totalCount = null,
        bool hasMoreItems = false)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        _hasMoreItems = hasMoreItems;
    }
}
