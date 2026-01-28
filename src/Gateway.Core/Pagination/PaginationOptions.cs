namespace Gateway.Core.Pagination;

/// <summary>
/// Configuration options for pagination.
/// </summary>
public class PaginationOptions
{
    /// <summary>
    /// Default page size when not specified. Default is 25.
    /// </summary>
    public int DefaultPageSize { get; set; } = 25;

    /// <summary>
    /// Maximum allowed page size. Default is 1000.
    /// </summary>
    public int MaxPageSize { get; set; } = 1000;

    /// <summary>
    /// Gets the effective page size, capped at MaxPageSize.
    /// </summary>
    public int GetEffectivePageSize(int? requestedPageSize)
    {
        var pageSize = requestedPageSize ?? DefaultPageSize;
        return Math.Min(pageSize, MaxPageSize);
    }
}
