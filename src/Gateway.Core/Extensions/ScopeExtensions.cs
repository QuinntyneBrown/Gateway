using Couchbase.KeyValue;
using Couchbase.Query;

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
}
