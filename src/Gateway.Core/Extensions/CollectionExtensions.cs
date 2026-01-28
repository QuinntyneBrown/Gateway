using System.Text.Json;
using Couchbase.KeyValue;

namespace Gateway.Core.Extensions;

/// <summary>
/// Extension methods for ICouchbaseCollection to provide SimpleMapper CRUD functionality.
/// These methods use the existing SDK connection without creating additional connections.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Gets a document by key and maps it to the specified type.
    /// </summary>
    public static async Task<T?> GetAsync<T>(
        this ICouchbaseCollection collection,
        string key,
        GetOptions? options = null) where T : class
    {
        try
        {
            var result = await collection.GetAsync(key, options ?? new GetOptions());
            return result.ContentAs<T>();
        }
        catch (Couchbase.Core.Exceptions.KeyValue.DocumentNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Inserts a new document with the specified key.
    /// </summary>
    public static async Task InsertAsync<T>(
        this ICouchbaseCollection collection,
        string key,
        T document,
        InsertOptions? options = null) where T : class
    {
        await collection.InsertAsync(key, document, options ?? new InsertOptions());
    }

    /// <summary>
    /// Upserts a document (inserts or updates) with the specified key.
    /// </summary>
    public static async Task UpsertAsync<T>(
        this ICouchbaseCollection collection,
        string key,
        T document,
        UpsertOptions? options = null) where T : class
    {
        await collection.UpsertAsync(key, document, options ?? new UpsertOptions());
    }

    /// <summary>
    /// Replaces an existing document with the specified key.
    /// </summary>
    public static async Task ReplaceAsync<T>(
        this ICouchbaseCollection collection,
        string key,
        T document,
        ReplaceOptions? options = null) where T : class
    {
        await collection.ReplaceAsync(key, document, options ?? new ReplaceOptions());
    }

    /// <summary>
    /// Removes a document by key.
    /// </summary>
    public static async Task RemoveAsync(
        this ICouchbaseCollection collection,
        string key,
        RemoveOptions? options = null)
    {
        await collection.RemoveAsync(key, options ?? new RemoveOptions());
    }
}
