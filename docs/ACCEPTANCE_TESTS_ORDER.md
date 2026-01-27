# Acceptance Tests - Implementation Order

Tests to be completed first appear first on this list.

## Phase 0: Foundation (44 tests)

1. **Connection Management** - 11 tests
   - SDK integration, extension methods, dependency injection

2. **Basic Object Mapping** - 15 tests
   - POCO mapping, records, classes, structs, Column attribute

3. **Basic Query Execution** - 10 tests
   - Raw SQL++ queries, parameter binding, QueryAsync methods

4. **Error Handling Framework** - 8 tests
   - Custom exceptions, SDK exception wrapping, error context

## Phase 1: Core Features (105 tests)

5. **Advanced Object Mapping** - 19 tests
   - Ignore attribute, nested objects, custom converters, enums

6. **CRUD Operations** - 31 tests
   - Get/Insert/Upsert/Replace/Remove, batch operations, CAS, TTL

7. **Filter Builder** - 24 tests
   - Fluent API, lambda expressions, parameterized SQL++ generation

8. **Filter Operations** - 31 tests
   - Comparison, string, array, NULL, BETWEEN, nested property access

## Phase 2: Advanced Features (51 tests)

9. **Advanced Query Execution** - 23 tests
   - Cancellation tokens, query options, streaming, dynamic mapping

10. **Pagination** - 28 tests
    - Offset-based, keyset/cursor pagination, sorting, metadata

## Phase 3: Optimization (14 tests)

11. **Performance Features** - 10 tests
    - Expression caching, object pooling, streaming, parallel execution

12. **Final Error Handling** - 4 tests
    - Query context in exceptions, RFC 7807 Problem Details format

---

**Total: 214 acceptance tests**
