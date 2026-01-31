// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Couchbase;
using Couchbase.KeyValue;
using Couchbase.Query;
using Gateway.Core.Extensions;
using Gateway.Core.Filtering;
using Gateway.Core.Pagination;
using SimpleStorage.Core.Interfaces;
using SimpleStorage.Core.Models;

namespace SimpleStorage.Infrastructure.Repositories;

public class MetadataRepository : IMetadataRepository
{
    private readonly IBucket _bucket;
    private readonly string _scopeName;
    private readonly string _collectionName;

    public MetadataRepository(IBucket bucket, string scopeName = "artifacts", string collectionName = "metadata")
    {
        _bucket = bucket;
        _scopeName = scopeName;
        _collectionName = collectionName;
    }

    public async Task<IReadOnlyList<Metadata>> GetAllAsync()
    {
        var scope = await _bucket.ScopeAsync(_scopeName);
        
        var query = $"SELECT META().id, m.* FROM `{_bucket.Name}`.`{_scopeName}`.`{_collectionName}` m ORDER BY m.uploadedAt DESC";
        
        var results = await scope.QueryToListAsync<Metadata>(query);
        return results;
    }

    public async Task<PagedResult<Metadata>> GetPageAsync(int pageNumber, int pageSize, int? fileType = null)
    {
        var scope = await _bucket.ScopeAsync(_scopeName);
        
        var filter = new FilterBuilder<Metadata>();
        
        if (fileType.HasValue)
        {
            filter.Where("fileType", fileType.Value);
        }
        
        filter.OrderBy("uploadedAt", descending: true);
        
        var paginationOptions = new PaginationOptions();
        var effectivePageSize = paginationOptions.GetEffectivePageSize(pageSize);
        var offset = (pageNumber - 1) * effectivePageSize;
        
        filter.Skip(offset).Take(effectivePageSize + 1);
        
        var whereClause = filter.Build();
        var query = $"SELECT META().id, m.* FROM `{_bucket.Name}`.`{_scopeName}`.`{_collectionName}` m {whereClause}";
        
        var queryOptions = new QueryOptions();
        foreach (var param in filter.Parameters)
        {
            queryOptions.Parameter(param.Key, param.Value ?? DBNull.Value);
        }
        
        var results = await scope.QueryToListAsync<Metadata>(query, queryOptions);
        
        var hasNextPage = results.Count > effectivePageSize;
        var items = hasNextPage ? results.Take(effectivePageSize).ToList() : results;
        
        return new PagedResult<Metadata>(
            items: items,
            pageNumber: pageNumber,
            pageSize: effectivePageSize,
            hasMoreItems: hasNextPage
        );
    }

    public async Task<Metadata?> GetByIdAsync(string id)
    {
        var scope = await _bucket.ScopeAsync(_scopeName);
        var collection = scope.Collection(_collectionName);
        return await collection.GetAsync<Metadata>(id);
    }

    public async Task<Metadata> CreateAsync(Metadata metadata)
    {
        var scope = await _bucket.ScopeAsync(_scopeName);
        var collection = scope.Collection(_collectionName);
        await collection.InsertAsync(metadata.Id, metadata);
        return metadata;
    }

    public async Task<Metadata> UpdateAsync(string id, Metadata metadata)
    {
        var scope = await _bucket.ScopeAsync(_scopeName);
        var collection = scope.Collection(_collectionName);
        await collection.ReplaceAsync(id, metadata);
        return metadata;
    }

    public async Task DeleteAsync(string id)
    {
        var scope = await _bucket.ScopeAsync(_scopeName);
        var collection = scope.Collection(_collectionName);
        await collection.RemoveAsync(id);
    }
}
