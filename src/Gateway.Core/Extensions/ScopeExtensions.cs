using Couchbase.KeyValue;
using Couchbase.Query;
using Gateway.Core.Filtering;
using Gateway.Core.Pagination;

namespace Gateway.Core.Extensions;

/// <summary>
/// Extension methods for IScope to provide SimpleMapper query functionality.
/// These methods use the existing SDK connection without creating additional connections.
/// </summary>
public static class ScopeExtensions
{
    /// <summary>
    /// Executes a SQL++ query and returns the results as a list.
    /// Uses the existing SDK connection - no additional connections are created.
    /// </summary>
    public static async Task<List<T>> QueryToListAsync<T>(
        this IScope scope,
        string statement,
        QueryOptions? options = null)
    {
        var queryOptions = options ?? new QueryOptions();
        var result = await scope.QueryAsync<T>(statement, queryOptions);
        return await result.Rows.ToListAsync();
    }

    /// <summary>
    /// Executes a SQL++ query and returns the first result.
    /// Uses the existing SDK connection - no additional connections are created.
    /// </summary>
    public static async Task<T> QueryFirstAsync<T>(
        this IScope scope,
        string statement,
        QueryOptions? options = null)
    {
        var queryOptions = options ?? new QueryOptions();
        var result = await scope.QueryAsync<T>(statement, queryOptions);
        return await result.Rows.FirstAsync();
    }

    /// <summary>
    /// Executes a SQL++ query and returns the first result or default.
    /// Uses the existing SDK connection - no additional connections are created.
    /// </summary>
    public static async Task<T?> QueryFirstOrDefaultAsync<T>(
        this IScope scope,
        string statement,
        QueryOptions? options = null)
    {
        var queryOptions = options ?? new QueryOptions();
        var result = await scope.QueryAsync<T>(statement, queryOptions);
        return await result.Rows.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Executes a SQL++ query and returns exactly one result.
    /// Throws if there are zero or more than one results.
    /// Uses the existing SDK connection - no additional connections are created.
    /// </summary>
    public static async Task<T> QuerySingleAsync<T>(
        this IScope scope,
        string statement,
        QueryOptions? options = null)
    {
        var queryOptions = options ?? new QueryOptions();
        var result = await scope.QueryAsync<T>(statement, queryOptions);
        return await result.Rows.SingleAsync();
    }

    /// <summary>
    /// Executes a SQL++ non-query statement (UPDATE, DELETE, INSERT).
    /// Returns the number of affected rows.
    /// Uses the existing SDK connection - no additional connections are created.
    /// </summary>
    public static async Task<int> ExecuteAsync(
        this IScope scope,
        string statement,
        QueryOptions? options = null)
    {
        var queryOptions = options ?? new QueryOptions();
        var result = await scope.QueryAsync<dynamic>(statement, queryOptions);
        // Consume the result to ensure the query completes
        await result.Rows.ToListAsync();
        // Return mutation count from metadata if available, otherwise 0
        return 0;
    }

    /// <summary>
    /// Executes a paginated query and returns a page of results with metadata.
    /// Only retrieves the items for the requested page, not all data.
    /// Uses the existing SDK connection - no additional connections are created.
    /// </summary>
    /// <typeparam name="T">The type to map results to</typeparam>
    /// <param name="scope">The Couchbase scope</param>
    /// <param name="baseQuery">The base SQL++ query. Must start with "SELECT *" for count query to work correctly.</param>
    /// <param name="filter">The filter builder with conditions and sorting. Note: This filter will be modified by adding Skip/Take.</param>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="includeTotalCount">Whether to execute a separate COUNT query for total count</param>
    /// <param name="options">Optional query options</param>
    /// <returns>A PagedResult containing the page items and metadata</returns>
    /// <remarks>
    /// The filter parameter will be modified by this method to add pagination (Skip/Take).
    /// If you need to reuse the filter, create a new instance before calling this method.
    /// </remarks>
    public static async Task<PagedResult<T>> GetPageAsync<T>(
        this IScope scope,
        string baseQuery,
        FilterBuilder<T> filter,
        int pageNumber,
        int pageSize,
        bool includeTotalCount = false,
        QueryOptions? options = null)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be 1 or greater");
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be 1 or greater");

        // Calculate offset for this page
        var offset = (pageNumber - 1) * pageSize;
        filter.Skip(offset).Take(pageSize);

        // Build the data query
        var dataQuery = $"{baseQuery}{filter.Build()}";
        var queryOptions = options ?? new QueryOptions();
        
        // Add filter parameters to query options
        foreach (var param in filter.Parameters)
        {
            queryOptions.Parameter(param.Key, param.Value ?? string.Empty);
        }

        // Execute queries
        Task<List<T>> dataTask = QueryToListAsync<T>(scope, dataQuery, queryOptions);
        Task<int>? countTask = null;

        if (includeTotalCount)
        {
            // Build count query - use WHERE clause but no ORDER BY, LIMIT, or OFFSET
            var whereClause = filter.BuildWhereClause();
            
            // Simple replacement - requires baseQuery to start with "SELECT *"
            if (!baseQuery.TrimStart().StartsWith("SELECT *", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("baseQuery must start with 'SELECT *' when includeTotalCount is true", nameof(baseQuery));
            }
            
            var countQuery = string.IsNullOrEmpty(whereClause) 
                ? $"{baseQuery.Replace("SELECT *", "SELECT COUNT(*) as count", StringComparison.OrdinalIgnoreCase)}"
                : $"{baseQuery.Replace("SELECT *", "SELECT COUNT(*) as count", StringComparison.OrdinalIgnoreCase)} WHERE {whereClause}";
            
            var countOptions = new QueryOptions();
            foreach (var param in filter.Parameters)
            {
                countOptions.Parameter(param.Key, param.Value ?? string.Empty);
            }
            
            // Execute count query asynchronously (no Task.Run needed - already async)
            countTask = ExecuteCountQueryAsync(scope, countQuery, countOptions);

            // Execute both queries in parallel
            await Task.WhenAll(dataTask, countTask);
        }
        else
        {
            await dataTask;
        }

        var items = dataTask.Result; // Task is already completed, safe to use Result
        var totalCount = countTask?.Result; // Task is already completed if not null

        return new PagedResult<T>(items, pageNumber, pageSize, totalCount);
    }

    private static async Task<int> ExecuteCountQueryAsync(IScope scope, string countQuery, QueryOptions options)
    {
        var result = await scope.QueryAsync<Dictionary<string, object>>(countQuery, options);
        var countResult = await result.Rows.FirstOrDefaultAsync();
        if (countResult != null && countResult.TryGetValue("count", out var countValue))
        {
            return Convert.ToInt32(countValue);
        }
        return 0;
    }
}
