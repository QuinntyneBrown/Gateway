# GetPage Design and Roadmap

## Overview

The GetPage functionality provides a convenient, efficient way to retrieve paginated data from Couchbase collections. The implementation ensures that only the data for the requested page is queried, rather than fetching all data and then filtering in memory.

## Design Principles

### 1. Query-Level Pagination
- **Principle**: Generate SQL++ queries with `LIMIT` and `OFFSET` clauses
- **Benefit**: Only requested page data is transferred from database
- **Implementation**: Uses FilterBuilder's `Skip()` and `Take()` methods

### 2. Minimal Data Transfer
- **Principle**: Avoid fetching unnecessary data
- **Benefit**: Reduces network bandwidth and memory usage
- **Implementation**: 
  - Data query includes `LIMIT pageSize`
  - Optional total count uses separate optimized query

### 3. Optional Total Count
- **Principle**: Total count query is opt-in via parameter
- **Benefit**: Faster queries when total count isn't needed
- **Implementation**:
  - `includeTotalCount: false` (default) - returns only page data
  - `includeTotalCount: true` - executes count query in parallel

### 4. Parallel Execution
- **Principle**: Execute data and count queries simultaneously when both needed
- **Benefit**: Reduces overall response time
- **Implementation**: Uses `Task.WhenAll()` for parallel execution

## API Design

### Extension Method Signature

```csharp
public static async Task<PagedResult<T>> GetPageAsync<T>(
    this IScope scope,
    string baseQuery,
    FilterBuilder<T> filter,
    int pageNumber,
    int pageSize,
    bool includeTotalCount = false,
    QueryOptions? options = null)
```

### Parameters

| Parameter | Type | Description | Validation |
|-----------|------|-------------|------------|
| `scope` | `IScope` | Couchbase scope for query execution | Required (extension target) |
| `baseQuery` | `string` | Base SQL++ query - **must start with "SELECT *"** | Required, validated when includeTotalCount=true |
| `filter` | `FilterBuilder<T>` | Filter with WHERE conditions and ORDER BY. **Note: Will be modified by adding Skip/Take** | Required |
| `pageNumber` | `int` | Page number (1-based) | Must be ≥ 1 |
| `pageSize` | `int` | Number of items per page | Must be ≥ 1 |
| `includeTotalCount` | `bool` | Whether to execute COUNT query | Default: false |
| `options` | `QueryOptions?` | Optional Couchbase query options | Optional |

### Important Notes

1. **Filter Mutation**: The `filter` parameter will be modified by this method (Skip/Take are added). If you need to reuse the filter object, create a new instance before calling GetPageAsync.

2. **Base Query Format**: When `includeTotalCount` is true, the `baseQuery` must start with "SELECT *" (case-insensitive). This is required for the count query to work correctly. Examples:
   - ✅ Valid: `"SELECT * FROM \`users\`"`
   - ✅ Valid: `"select * FROM \`products\`"`
   - ❌ Invalid: `"SELECT u.* FROM \`users\` u"`
   - ❌ Invalid: `"SELECT DISTINCT * FROM \`items\`"`

3. **Null Parameters**: Parameter values will be converted to empty string if null (Couchbase requirement).

### Return Type

Returns `PagedResult<T>` containing:
- `Items` - The page items as read-only list
- `PageNumber` - Current page number (1-based)
- `PageSize` - Items per page
- `TotalCount` - Total count (null if not requested)
- `TotalPages` - Calculated total pages (null if count not available)
- `HasPreviousPage` - Boolean flag for navigation
- `HasNextPage` - Boolean flag for navigation

## Implementation Details

### Query Generation

#### Data Query
```
{baseQuery}{filter.Build()}
```

Example:
```sql
SELECT * FROM `users` WHERE status = $p0 AND age > $p1 ORDER BY name LIMIT 20 OFFSET 40
```

#### Count Query (when requested)
```
{baseQuery with SELECT COUNT(*) as count} WHERE {filter.BuildWhereClause()}
```

Example:
```sql
SELECT COUNT(*) as count FROM `users` WHERE status = $p0 AND age > $p1
```

**Note**: Count query excludes `ORDER BY`, `LIMIT`, and `OFFSET` for optimization.

### Offset Calculation

```csharp
var offset = (pageNumber - 1) * pageSize;
```

| Page | Size | Offset | Items Retrieved |
|------|------|--------|-----------------|
| 1 | 10 | 0 | 1-10 |
| 2 | 10 | 10 | 11-20 |
| 3 | 10 | 20 | 21-30 |

### Parallel Execution Flow

```
if (includeTotalCount)
{
    dataTask = ExecuteDataQuery()
    countTask = ExecuteCountQuery()
    await Task.WhenAll(dataTask, countTask)
}
else
{
    await ExecuteDataQuery()
}
```

## Usage Examples

### Basic Pagination (without total count)

```csharp
var filter = new FilterBuilder<User>();
filter.Where("status", "active")
      .OrderBy("name");

var page = await scope.GetPageAsync(
    baseQuery: "SELECT * FROM `users`",
    filter: filter,
    pageNumber: 1,
    pageSize: 20
);

// page.TotalCount == null
// page.Items contains up to 20 users
```

### Pagination with Total Count

```csharp
var filter = new FilterBuilder<User>();
filter.Where("status", "active")
      .WhereGreaterThan("age", 18)
      .OrderBy("createdAt", descending: true);

var page = await scope.GetPageAsync(
    baseQuery: "SELECT * FROM `users`",
    filter: filter,
    pageNumber: 2,
    pageSize: 25,
    includeTotalCount: true
);

// page.TotalCount has value
// page.TotalPages calculated
// page.HasPreviousPage == true
// page.HasNextPage depends on remaining data
```

### With PaginationOptions

```csharp
var options = new PaginationOptions 
{
    DefaultPageSize = 25,
    MaxPageSize = 100
};

var requestedPageSize = 150; // User requests 150
var effectivePageSize = options.GetEffectivePageSize(requestedPageSize); // Capped at 100

var filter = new FilterBuilder<User>();
var page = await scope.GetPageAsync(
    baseQuery: "SELECT * FROM `users`",
    filter: filter,
    pageNumber: 1,
    pageSize: effectivePageSize
);
```

## Performance Characteristics

### Without Total Count
- **Queries Executed**: 1 (data query only)
- **Response Time**: Single query latency
- **Network Transfer**: Page data only
- **Use Case**: Infinite scroll, "load more" patterns

### With Total Count
- **Queries Executed**: 2 (data + count, in parallel)
- **Response Time**: max(data_query_time, count_query_time)
- **Network Transfer**: Page data + count result
- **Use Case**: Traditional pagination with page numbers

### Optimization Tips

1. **Avoid COUNT on large datasets**: Use `includeTotalCount: false` when possible
2. **Use appropriate page sizes**: Balance between request count and response size
3. **Add indexes**: Ensure indexed columns for WHERE and ORDER BY clauses
4. **Consider keyset pagination**: For very large datasets, use cursor-based approach

## Integration with FilterBuilder

The GetPageAsync method integrates seamlessly with FilterBuilder:

```csharp
// Build complex filters
var filter = new FilterBuilder<Product>();
filter.Where("category", "Electronics")
      .WhereIn("brand", new[] { "Apple", "Samsung", "Sony" })
      .WhereGreaterThan("price", 100)
      .WhereLessThanOrEqual("price", 1000)
      .WhereNotNull("discountPrice")
      .OrderBy("price", descending: true);

// Execute paginated query
var page = await scope.GetPageAsync(
    "SELECT * FROM `products`",
    filter,
    pageNumber: 1,
    pageSize: 30,
    includeTotalCount: true
);
```

## Comparison with Manual Pagination

### Manual Approach (Before)
```csharp
var filter = new FilterBuilder<User>();
filter.Where("status", "active");

// Calculate offset manually
var offset = (pageNumber - 1) * pageSize;
filter.Skip(offset).Take(pageSize);

// Build query manually
var query = $"SELECT * FROM `users`{filter.Build()}";
var options = new QueryOptions();
foreach (var p in filter.Parameters)
    options.Parameter(p.Key, p.Value);

// Execute data query
var users = await scope.QueryToListAsync<User>(query, options);

// Execute count query separately if needed
int? totalCount = null;
if (includeTotalCount)
{
    var countQuery = $"SELECT COUNT(*) as count FROM `users` WHERE {filter.BuildWhereClause()}";
    // Execute count query...
}

// Create PagedResult manually
var result = new PagedResult<User>(users, pageNumber, pageSize, totalCount);
```

### GetPageAsync Approach (After)
```csharp
var filter = new FilterBuilder<User>();
filter.Where("status", "active");

var page = await scope.GetPageAsync(
    "SELECT * FROM `users`",
    filter,
    pageNumber,
    pageSize,
    includeTotalCount: true
);
```

**Benefits**:
- ✅ Single method call
- ✅ Automatic offset calculation
- ✅ Automatic parameter binding
- ✅ Parallel count query execution
- ✅ Less boilerplate code
- ✅ Consistent error handling

## Future Enhancements

### Phase 2: Cursor-Based Pagination
- Add `After(cursor)` method to FilterBuilder
- Support composite cursors for multi-column sorts
- Generate keyset WHERE clauses automatically

### Phase 3: Smart Pagination
- Intelligent page size adjustment based on result size
- Prefetch next page in background
- Cache count results with TTL

### Phase 4: Analytics
- Track pagination patterns
- Identify frequently accessed pages
- Optimize queries based on usage

## Testing Strategy

### Unit Tests
- Parameter validation (page number, page size)
- Offset calculation
- Query building
- Parameter binding

### Integration Tests
- End-to-end pagination with real Couchbase
- Count query accuracy
- Parallel execution
- Filter integration

### Performance Tests
- Large dataset pagination
- Count query performance
- Memory usage with different page sizes
- Parallel execution timing

## Best Practices

### 1. Choose Appropriate Page Size
```csharp
// Too small - many requests
pageSize: 5  // ❌

// Too large - slow response, high memory
pageSize: 10000  // ❌

// Good balance
pageSize: 20-50  // ✅
```

### 2. Use Total Count Wisely
```csharp
// First page with count for pagination UI
var firstPage = await GetPageAsync(..., pageNumber: 1, includeTotalCount: true);

// Subsequent pages without count
var nextPage = await GetPageAsync(..., pageNumber: 2, includeTotalCount: false);
```

### 3. Order Results Consistently
```csharp
// Always include ORDER BY for predictable results
filter.OrderBy("id");  // ✅ Consistent pagination

// Without ORDER BY, results may be inconsistent
// No OrderBy  // ❌ Unpredictable across pages
```

### 4. Index Your Sort Columns
```sql
-- Create index on sort columns for performance
CREATE INDEX idx_users_status_name ON `users`(status, name);
```

### 5. Handle Edge Cases
```csharp
// Validate input
if (pageNumber < 1)
    throw new ArgumentOutOfRangeException(nameof(pageNumber));

// Handle empty results
if (page.Items.Count == 0)
{
    // Show "no results" message
}

// Handle last page
if (!page.HasNextPage)
{
    // Disable "next" button
}
```

## Conclusion

The GetPage functionality provides a robust, efficient, and user-friendly way to implement pagination in Couchbase applications. By generating SQL++ queries with proper LIMIT and OFFSET clauses, it ensures minimal data transfer while providing rich metadata for building pagination UIs. The optional total count feature with parallel execution optimizes performance for different use cases.
