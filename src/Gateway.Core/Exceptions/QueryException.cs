namespace Gateway.Core.Exceptions;

/// <summary>
/// Exception thrown when a query execution fails.
/// </summary>
public class QueryException : Exception
{
    /// <summary>
    /// The SQL++ query that failed.
    /// </summary>
    public string? Query { get; }

    /// <summary>
    /// The error code from Couchbase, if available.
    /// </summary>
    public int? ErrorCode { get; }

    public QueryException(string message)
        : base(message)
    {
    }

    public QueryException(string message, string? query)
        : base(message)
    {
        Query = query;
    }

    public QueryException(string message, string? query, int? errorCode)
        : base(message)
    {
        Query = query;
        ErrorCode = errorCode;
    }

    public QueryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public QueryException(string message, string? query, Exception innerException)
        : base(message, innerException)
    {
        Query = query;
    }
}
