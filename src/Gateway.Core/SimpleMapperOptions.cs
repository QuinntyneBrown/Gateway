namespace Gateway.Core;

/// <summary>
/// Configuration options for SimpleMapper.
/// Note: This class intentionally has NO connection pool settings.
/// All connection management is delegated to the Couchbase SDK.
/// </summary>
public class SimpleMapperOptions
{
    /// <summary>
    /// The default bucket name to use when not explicitly specified.
    /// </summary>
    public string? DefaultBucket { get; set; }

    /// <summary>
    /// The default scope name to use when not explicitly specified.
    /// </summary>
    public string? DefaultScope { get; set; }
}
