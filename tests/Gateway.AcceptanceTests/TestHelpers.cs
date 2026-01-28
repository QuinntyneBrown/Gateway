namespace Gateway.AcceptanceTests;

/// <summary>
/// Test helper extensions for async enumerable operations.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Converts an IEnumerable to an IAsyncEnumerable for test mocking purposes.
    /// </summary>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }
}
