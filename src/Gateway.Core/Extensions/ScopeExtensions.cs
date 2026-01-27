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
}
