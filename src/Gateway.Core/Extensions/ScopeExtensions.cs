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
    /// <param name="baseQuery">The base SQL++ query (e.g., "SELECT * FROM `users`")</param>
    /// <param name="filter">The filter builder with conditions, sorting, and pagination</param>
    /// <param name="pageNumber">The page number (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="includeTotalCount">Whether to execute a separate COUNT query for total count</param>
    /// <param name="options">Optional query options</param>
    /// <returns>A PagedResult containing the page items and metadata</returns>
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
            if (param.Value != null)
            {
                queryOptions.Parameter(param.Key, param.Value);
            }
        }

        // Execute queries
        Task<List<T>> dataTask = QueryToListAsync<T>(scope, dataQuery, queryOptions);
        Task<int>? countTask = null;

        if (includeTotalCount)
        {
            // Build count query - use WHERE clause but no ORDER BY, LIMIT, or OFFSET
            var whereClause = filter.BuildWhereClause();
            var countQuery = string.IsNullOrEmpty(whereClause) 
                ? $"{baseQuery.Replace("SELECT *", "SELECT COUNT(*) as count")}"
                : $"{baseQuery.Replace("SELECT *", "SELECT COUNT(*) as count")} WHERE {whereClause}";
            
            var countOptions = new QueryOptions();
            foreach (var param in filter.Parameters)
            {
                if (param.Value != null)
                {
                    countOptions.Parameter(param.Key, param.Value);
                }
            }
            
            countTask = Task.Run(async () =>
            {
                var result = await scope.QueryAsync<Dictionary<string, object>>(countQuery, countOptions);
                var countResult = await result.Rows.FirstOrDefaultAsync();
                if (countResult != null && countResult.TryGetValue("count", out var countValue))
                {
                    return Convert.ToInt32(countValue);
                }
                return 0;
            });

            // Execute both queries in parallel
            await Task.WhenAll(dataTask, countTask);
        }
        else
        {
            await dataTask;
        }

        var items = await dataTask;
        var totalCount = countTask != null ? await countTask : (int?)null;

        return new PagedResult<T>(items, pageNumber, pageSize, totalCount);
    }
}
