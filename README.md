# Gateway

A lightweight, high-performance Object Mapper for .NET that works with Couchbase using SQL++ (N1QL). Inspired by [Dapper](https://github.com/DapperLib/Dapper), Gateway provides a simple, efficient way to map Couchbase query results to .NET objects with powerful filtering and pagination capabilities.

## Features

- **Extension Methods** - Non-invasive extensions on Couchbase SDK's `IScope` and `ICouchbaseCollection`
- **Object Mapping** - Automatic JSON to POCO mapping with System.Text.Json
- **FilterBuilder** - Fluent, parameterized SQL++ WHERE clause generation
- **Pagination** - PagedResult with metadata (HasPreviousPage, HasNextPage, TotalPages)
- **CRUD Operations** - GetAsync, InsertAsync, UpsertAsync, ReplaceAsync, RemoveAsync
- **Async-Only API** - All operations are async for optimal performance
- **DI Support** - Microsoft.Extensions.DependencyInjection integration

## Installation

```bash
# Add project reference (NuGet package coming soon)
dotnet add reference path/to/Gateway.Core.csproj
```

## Quick Start

### 1. Register Services

```csharp
// Program.cs
builder.Services.AddCouchbase(options =>
{
    options.ConnectionString = "couchbase://localhost";
    options.UserName = "Administrator";
    options.Password = "password";
});

builder.Services.AddCouchbaseSimpleMapper(opts =>
{
    opts.DefaultBucket = "myBucket";
    opts.DefaultScope = "_default";
});
```

### 2. Query with FilterBuilder

```csharp
// Build dynamic filters
var filter = new FilterBuilder<User>();
filter.Where("status", "active");
filter.WhereGreaterThan("age", 18);
filter.OrderBy("createdAt", descending: true);
filter.Skip(0).Take(10);

// Execute query
var query = $"SELECT * FROM `users` {filter.Build()}";
var options = new QueryOptions();
foreach (var p in filter.Parameters)
    options.Parameter(p.Key, p.Value);

var users = await scope.QueryToListAsync<User>(query, options);
```

### 3. CRUD Operations

```csharp
// Get document
var user = await collection.GetAsync<User>("user::123");

// Insert new document
await collection.InsertAsync("user::456", newUser);

// Update document
await collection.ReplaceAsync("user::456", updatedUser);

// Delete document
await collection.RemoveAsync("user::456");
```

### 4. Pagination

```csharp
var pagedResult = new PagedResult<User>(
    items: users,
    pageNumber: 1,
    pageSize: 10,
    totalCount: 100
);

// Access metadata
Console.WriteLine($"Page {pagedResult.PageNumber} of {pagedResult.TotalPages}");
Console.WriteLine($"Has Next: {pagedResult.HasNextPage}");
```

## FilterBuilder API

| Method | SQL++ Output |
|--------|-------------|
| `Where("name", "John")` | `name = $p0` |
| `WhereNotEqual("status", "deleted")` | `status != $p0` |
| `WhereGreaterThan("age", 21)` | `age > $p0` |
| `WhereLessThanOrEqual("price", 100)` | `price <= $p0` |
| `WhereIn("category", ["A", "B"])` | `category IN [$p0, $p1]` |
| `WhereNotIn("status", ["x", "y"])` | `status NOT IN [$p0, $p1]` |
| `WhereNull("deletedAt")` | `deletedAt IS NULL` |
| `WhereNotNull("email")` | `email IS NOT NULL` |
| `WhereBetween("age", 18, 65)` | `age BETWEEN $p0 AND $p1` |
| `WhereLike("name", "%john%")` | `name LIKE $p0` |
| `WhereContains("title", "search")` | `CONTAINS(title, $p0)` |
| `WhereRaw("ARRAY_LENGTH(items) > $min", new { min = 5 })` | `ARRAY_LENGTH(items) > $min` |
| `OrderBy("name", descending: false)` | `ORDER BY name ASC` |
| `Skip(20).Take(10)` | `OFFSET 20 LIMIT 10` |

## Project Structure

```
Gateway/
├── src/
│   └── Gateway.Core/           # Core library
│       ├── Extensions/         # IScope and ICollection extensions
│       ├── Filtering/          # FilterBuilder
│       ├── Pagination/         # PagedResult, PaginationOptions
│       ├── Mapping/            # ObjectMapper, attributes
│       └── Exceptions/         # MappingException, QueryException
├── tests/
│   └── Gateway.AcceptanceTests/ # 218 acceptance tests
├── playground/
│   └── ToDos/                  # Sample ToDo API demo
└── docs/
    ├── requirements.md         # Full requirements specification
    ├── ACCEPTANCE_TESTS_ROADMAP.md
    └── REQUIREMENTS_AUDIT.md   # Implementation audit
```

## Demo: ToDos API

A complete sample API demonstrating all Gateway features:

```bash
cd playground/ToDos

# Start Couchbase + API with Docker
docker-compose up -d

# Initialize database
.\scripts\setup-couchbase.ps1

# Access the API
# Swagger: http://localhost:5000/swagger
# Couchbase: http://localhost:8091
```

See [playground/ToDos/README.md](playground/ToDos/README.md) for full documentation.

## Documentation

| Document | Description |
|----------|-------------|
| [Requirements](docs/requirements.md) | Complete feature specifications |
| [Acceptance Tests Roadmap](docs/ACCEPTANCE_TESTS_ROADMAP.md) | Implementation plan |
| [Requirements Audit](docs/REQUIREMENTS_AUDIT.md) | Implementation status audit |
| [ToDos Demo](playground/ToDos/README.md) | Sample API documentation |

## Current Status

| Metric | Value |
|--------|-------|
| Acceptance Tests | 218 |
| Tests Passing | 218 (100%) |
| Requirements Implemented | 47 fully, 14 partially |
| Phase | MVP Complete |

### Test Coverage by Category

| Category | Tests |
|----------|-------|
| Connection Management | 11 |
| Object Mapping | 35 |
| Query Execution | 33 |
| CRUD Operations | 31 |
| Filter Builder | 24 |
| Filter Operations | 31 |
| Pagination | 28 |
| Performance | 10 |
| Error Handling | 15 |

## Requirements

- .NET 9.0+
- Couchbase Server 7.0+ (or Community Edition via Docker)
- CouchbaseNetClient 3.6+

## License

MIT License - See [LICENSE](LICENSE) for details.