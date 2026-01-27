# Gateway Acceptance Tests

This project contains comprehensive acceptance tests for the Couchbase SimpleMapper library based on the requirements specified in `/docs/requirements.md`.

## Test-Driven Development Approach

These tests follow **Acceptance Test-Driven Development (ATDD)** principles:
- All tests are written **before** implementation (Red phase)
- Each test maps directly to a Gherkin scenario from the requirements document
- All tests currently **FAIL** by throwing `NotImplementedException`
- Tests will guide the implementation and turn **GREEN** as features are completed

## Test Coverage

### Total Tests: 214 acceptance tests

| Test File | Test Count | Requirements Covered |
|-----------|-----------|---------------------|
| ConnectionManagementTests.cs | 11 | REQ-CONN-001 to REQ-CONN-004 |
| ObjectMappingTests.cs | 34 | REQ-MAP-001 to REQ-MAP-010 |
| QueryExecutionTests.cs | 33 | REQ-QUERY-001 to REQ-QUERY-011 |
| CrudOperationsTests.cs | 31 | REQ-CRUD-001 to REQ-CRUD-010 |
| FilterBuilderTests.cs | 24 | REQ-FILTER-001 to REQ-FILTER-008 |
| FilterOperationsTests.cs | 31 | REQ-FILTER-OP-001 to REQ-FILTER-OP-010 |
| PaginationTests.cs | 28 | REQ-PAGE-001 to REQ-PAGE-009 |
| PerformanceTests.cs | 10 | REQ-PERF-001 to REQ-PERF-005 |
| ErrorHandlingTests.cs | 12 | REQ-ERR-001 to REQ-ERR-005 |

## Test Structure

Each test follows this pattern:

```csharp
[Fact]
public async Task TestName()
{
    // REQ-XXX-NNN: Scenario: Scenario Name
    // Given: preconditions
    // When: action performed
    // Then: expected outcome
    // And: additional expectations

    throw new NotImplementedException("Test not yet implemented - ATDD Red phase");
}
```

## Key Features

- **xUnit Framework**: Modern .NET testing framework
- **FluentAssertions**: For expressive, readable assertions
- **Async/Await**: Full async support throughout
- **Descriptive Names**: Test names match scenario descriptions
- **Requirements Traceability**: Each test references its requirement ID

## Running the Tests

### Build the project
```bash
dotnet build tests/Gateway.AcceptanceTests/Gateway.AcceptanceTests.csproj
```

### Run all tests
```bash
dotnet test tests/Gateway.AcceptanceTests/Gateway.AcceptanceTests.csproj
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~ConnectionManagementTests"
```

### Run tests matching a pattern
```bash
dotnet test --filter "DisplayName~Filter"
```

## Current Status

✅ **All 214 tests compile successfully**  
❌ **All 214 tests fail (as expected in Red phase)**  
🔄 **Ready for implementation to begin**

## Important Notes

### Special Test Cases
Some tests in this suite verify requirements that aren't traditional runtime behavior tests:

- **Compile-time safety tests** (e.g., FilterBuilderTests.CompileTimeErrorForInvalidProperty): These verify type safety through IDE/compiler support. They should be documented rather than runtime-tested, or verified through Roslyn analyzers.
  
- **API surface tests** (e.g., ConnectionManagementTests checking for extension methods): These verify API availability and should be complemented with actual usage tests.

- **Implementation detail tests** (e.g., PerformanceTests checking for compiled delegates): These verify internal implementation choices from requirements and may need alternative verification strategies like code reviews or benchmarks.

When implementing these tests, consider:
1. For compile-time checks: Document the expected behavior and/or use Roslyn analyzers
2. For API surface checks: Supplement with actual usage scenarios  
3. For performance/implementation checks: Use profiling tools and benchmarks instead of or in addition to unit tests

These special cases are included because they represent actual requirements from the specification document, even though they may not be ideal as runtime acceptance tests.

## Next Steps

1. Implement SimpleMapper library features
2. Tests will guide implementation (TDD)
3. Run tests frequently to see progress
4. Tests turn green as features are completed
5. Add integration tests with actual Couchbase server

## Test Categories

### Connection Management (11 tests)
- SDK integration without duplicate connection handling
- Extension methods on IScope and ICouchbaseCollection
- Dependency injection support

### Object Mapping (34 tests)
- Automatic POCO mapping
- Support for records, classes, and structs
- Column and Ignore attributes
- Nested objects and collections
- Custom type converters
- Constructor-based initialization

### Query Execution (33 tests)
- Raw SQL++ query support
- Parameter binding (anonymous objects, dictionaries)
- Query methods: QueryAsync, QueryFirstAsync, QuerySingleAsync
- Async-only API
- Cancellation token support

### CRUD Operations (31 tests)
- Get/Insert/Upsert/Replace/Remove operations
- Batch operations
- Optimistic concurrency (CAS)
- Document expiration (TTL)
- Auto-generated keys

### Filter Builder (24 tests)
- Fluent WHERE clause API
- Type-safe lambda expressions
- Parameterized SQL++ generation
- SQL injection prevention
- AND/OR operators and grouping

### Filter Operations (31 tests)
- Comparison operators
- String matching (Contains, StartsWith, EndsWith)
- IN and NOT IN
- NULL checks
- BETWEEN
- Array operations
- Nested property access

### Pagination (28 tests)
- Offset-based pagination
- Keyset (cursor-based) pagination
- Metadata and navigation
- Sorting support
- Filter integration
- Optimized COUNT queries

### Performance (10 tests)
- Compiled expression trees
- Mapper caching
- Minimal allocations
- Result streaming
- Parallel query execution

### Error Handling (12 tests)
- Custom exception types
- SDK exception wrapping
- Meaningful error messages
- Query context in exceptions
- Problem Details pattern (RFC 7807)

## Contributing

When implementing features:
1. Find the relevant test file and tests
2. Remove the `throw new NotImplementedException()` line
3. Implement the actual test logic with assertions
4. Run the test to verify it passes
5. Move on to the next test

## References

- Requirements: `/docs/requirements.md`
- xUnit Documentation: https://xunit.net/
- FluentAssertions: https://fluentassertions.com/
- Couchbase .NET SDK: https://docs.couchbase.com/dotnet-sdk/current/hello-world/start-using-sdk.html
