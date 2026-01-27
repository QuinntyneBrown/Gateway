# Couchbase Object Mapper for .NET - MVP Requirements

## Overview

This document defines the requirements for a Minimum Viable Product (MVP) of a lightweight, high-performance Object Mapper for .NET that works with Couchbase using SQL++ (N1QL). The library is inspired by [Dapper](https://github.com/DapperLib/Dapper) and aims to provide a simple, efficient way to map Couchbase query results to .NET objects while offering powerful filtering and pagination capabilities.

**Project Name:** `Couchbase.SimpleMapper` (working title)

---

## 1. Goals and Non-Goals

### 1.1 Goals

- Provide a lightweight, performant object mapper for Couchbase SQL++
- Support strongly-typed query results with automatic mapping
- Enable fluent filter building that translates to SQL++ WHERE clauses
- Provide first-class support for paginated queries
- Minimize boilerplate code for common CRUD operations
- Support async/await patterns throughout
- Maintain compatibility with the official Couchbase .NET SDK

### 1.2 Non-Goals (Out of Scope for MVP)

- Full ORM features (change tracking, lazy loading, identity map)
- Automatic schema migrations
- Complex relationship mapping (joins across collections)
- Caching layer
- Query result caching
- Full-text search (FTS) integration
- Analytics service integration
- Eventing service integration

---

## 2. Technical Requirements

### 2.1 Platform and Dependencies

| Requirement | Specification |
|-------------|---------------|
| Target Framework | .NET 8.0+ |
| Couchbase SDK | CouchbaseNetClient 3.4+ |
| Couchbase Server | 7.0+ (SQL++ support) |
| Language Version | C# 12+ |

### 2.2 NuGet Package Structure

```
Couchbase.SimpleMapper           - Core library
Couchbase.SimpleMapper.Tests     - Unit and integration tests
```

---

## 3. Core Features

### 3.1 Connection Management

---

#### REQ-CONN-001: Couchbase SDK Integration

**Requirement Statement:**
The library SHALL integrate with the existing Couchbase .NET SDK cluster and bucket management without creating separate connection handling.

**Rationale:**
Leveraging the official SDK ensures stability, security updates, and consistent behavior with Couchbase best practices.

**Acceptance Criteria:**

```gherkin
Scenario: Library uses existing SDK cluster connection
  Given a Couchbase cluster is connected using the official SDK
  And a bucket and scope are opened
  When I use the SimpleMapper extension methods
  Then the library uses the existing cluster connection
  And no additional connections are created

Scenario: Library works with SDK connection pooling
  Given a Couchbase cluster with connection pooling configured
  When multiple concurrent queries are executed via SimpleMapper
  Then all queries use the SDK's connection pool
  And connection count does not exceed SDK pool limits

Scenario: Library respects SDK cluster disposal
  Given a SimpleMapper context is created from an SDK cluster
  When the SDK cluster is disposed
  Then SimpleMapper operations throw ObjectDisposedException
  And no orphaned connections remain
```

---

#### REQ-CONN-002: No Independent Connection Pool

**Requirement Statement:**
The library SHALL NOT manage its own connection pool and SHALL delegate all connection management to the Couchbase SDK.

**Rationale:**
Duplicate connection management creates resource conflicts and complicates troubleshooting.

**Acceptance Criteria:**

```gherkin
Scenario: Library has no connection pool configuration
  Given the SimpleMapper options class
  When inspecting available configuration properties
  Then no connection pool settings exist (min/max connections, idle timeout, etc.)
  And all connection behavior is inherited from SDK configuration

Scenario: Library creates no background connection threads
  Given SimpleMapper is initialized with a scope
  When monitoring active threads before and after initialization
  Then no additional background threads are created by SimpleMapper
  And thread count remains consistent with SDK-only baseline
```

---

#### REQ-CONN-003: Extension Methods on SDK Interfaces

**Requirement Statement:**
The library SHALL provide extension methods on `ICouchbaseCollection` and `IScope` interfaces for all data operations.

**Rationale:**
Extension methods provide a natural, discoverable API that integrates seamlessly with existing SDK usage patterns.

**Acceptance Criteria:**

```gherkin
Scenario: Extension methods available on IScope
  Given a reference to an IScope instance
  When using IntelliSense or reflection to list available methods
  Then QueryAsync<T>, QueryFirstAsync<T>, QueryFirstOrDefaultAsync<T> are available
  And QuerySingleAsync<T>, ExecuteAsync, and Query<T> builder are available

Scenario: Extension methods available on ICouchbaseCollection
  Given a reference to an ICouchbaseCollection instance
  When using IntelliSense or reflection to list available methods
  Then GetAsync<T>, InsertAsync<T>, UpsertAsync<T> are available
  And ReplaceAsync<T>, RemoveAsync methods are available

Scenario: Extension methods work without additional setup
  Given a Couchbase scope obtained from SDK
  And SimpleMapper NuGet package is referenced
  When calling scope.QueryAsync<User>("SELECT * FROM users")
  Then the query executes successfully
  And results are mapped to User objects
```

---

#### REQ-CONN-004: Dependency Injection Support

**Requirement Statement:**
The library SHALL support configuration via dependency injection using Microsoft.Extensions.DependencyInjection.

**Rationale:**
Modern .NET applications use DI containers; native support simplifies integration and testing.

**Acceptance Criteria:**

```gherkin
Scenario: Register SimpleMapper with DI container
  Given a ServiceCollection for dependency injection
  When calling services.AddCouchbaseSimpleMapper(options => { ... })
  Then SimpleMapper services are registered in the container
  And ISimpleMapperContext is resolvable from the provider

Scenario: Configure options via DI
  Given SimpleMapper registered with options
  And options.DefaultBucket = "testBucket"
  And options.DefaultScope = "testScope"
  When resolving ISimpleMapperContext
  Then the context is configured with the specified bucket and scope

Scenario: Options can be bound from configuration
  Given appsettings.json contains SimpleMapper configuration section
  When calling services.AddCouchbaseSimpleMapper(config.GetSection("SimpleMapper"))
  Then options are populated from the configuration file
  And all settings match the JSON values
```

---

### 3.2 Object Mapping

---

#### REQ-MAP-001: Automatic POCO Mapping

**Requirement Statement:**
The library SHALL automatically map SQL++ query results to Plain Old CLR Objects (POCOs) based on property name matching.

**Rationale:**
Automatic mapping eliminates boilerplate code and reduces errors from manual data transfer.

**Acceptance Criteria:**

```gherkin
Scenario: Map query result to POCO with matching property names
  Given a User class with properties: Id (string), Name (string), Age (int)
  And a SQL++ query returns JSON: {"id": "u1", "name": "John", "age": 30}
  When the query result is mapped to User
  Then user.Id equals "u1"
  And user.Name equals "John"
  And user.Age equals 30

Scenario: Map query result with missing properties
  Given a User class with properties: Id, Name, Age, Email
  And a SQL++ query returns JSON without "email" field
  When the query result is mapped to User
  Then user.Email is null (for reference types) or default (for value types)
  And no exception is thrown

Scenario: Map query result with extra fields
  Given a User class with properties: Id, Name
  And a SQL++ query returns JSON: {"id": "u1", "name": "John", "age": 30, "extra": "data"}
  When the query result is mapped to User
  Then mapping succeeds
  And extra fields are ignored
  And user.Id equals "u1" and user.Name equals "John"
```

---

#### REQ-MAP-002: Support for Records, Classes, and Structs

**Requirement Statement:**
The library SHALL support mapping to C# records, classes, and structs.

**Rationale:**
Different application architectures prefer different type kinds; supporting all ensures flexibility.

**Acceptance Criteria:**

```gherkin
Scenario: Map to a class with parameterless constructor
  Given a public class User { public string Id { get; set; } public string Name { get; set; } }
  And a SQL++ query returns {"id": "u1", "name": "John"}
  When the query result is mapped to User
  Then a User instance is created
  And properties are populated correctly

Scenario: Map to a record type
  Given a public record UserRecord(string Id, string Name);
  And a SQL++ query returns {"id": "u1", "name": "John"}
  When the query result is mapped to UserRecord
  Then a UserRecord instance is created via constructor
  And Id equals "u1" and Name equals "John"

Scenario: Map to a struct
  Given a public struct UserStruct { public string Id; public string Name; }
  And a SQL++ query returns {"id": "u1", "name": "John"}
  When the query result is mapped to UserStruct
  Then a UserStruct is created
  And fields are populated correctly

Scenario: Map to a record with init-only properties
  Given a public record User { public string Id { get; init; } public string Name { get; init; } }
  And a SQL++ query returns {"id": "u1", "name": "John"}
  When the query result is mapped to User
  Then the User instance has Id = "u1" and Name = "John"
```

---

#### REQ-MAP-003: Column Attribute for Property Mapping

**Requirement Statement:**
The library SHALL support a `[Column("name")]` attribute to map properties to differently-named JSON fields.

**Rationale:**
Database naming conventions often differ from C# conventions; explicit mapping handles this cleanly.

**Acceptance Criteria:**

```gherkin
Scenario: Map property with Column attribute
  Given a User class with [Column("full_name")] public string FullName { get; set; }
  And a SQL++ query returns {"full_name": "John Doe"}
  When the query result is mapped to User
  Then user.FullName equals "John Doe"

Scenario: Column attribute takes precedence over property name
  Given a User class with [Column("user_name")] public string Name { get; set; }
  And a SQL++ query returns {"name": "Wrong", "user_name": "Correct"}
  When the query result is mapped to User
  Then user.Name equals "Correct"

Scenario: Column attribute with empty string is invalid
  Given a User class with [Column("")] on a property
  When the mapper is initialized
  Then an InvalidOperationException is thrown
  And the message indicates empty column name is not allowed
```

---

#### REQ-MAP-004: Ignore Attribute for Exclusion

**Requirement Statement:**
The library SHALL support an `[Ignore]` attribute to exclude properties from mapping.

**Rationale:**
Computed properties, navigation properties, or internal state should not be mapped from/to the database.

**Acceptance Criteria:**

```gherkin
Scenario: Property with Ignore attribute is not mapped from query
  Given a User class with [Ignore] public string Computed { get; set; }
  And a SQL++ query returns {"id": "u1", "computed": "should_ignore"}
  When the query result is mapped to User
  Then user.Computed is null
  And no attempt is made to map the "computed" field

Scenario: Property with Ignore attribute is not included in insert
  Given a User class with [Ignore] public string TempValue { get; set; } = "temp"
  When inserting the user via InsertAsync
  Then the generated document does not contain "tempValue" or "TempValue" field

Scenario: Ignore attribute on getter-only property
  Given a User class with [Ignore] public string FullName => $"{First} {Last}"
  When mapping or serializing
  Then no error occurs
  And the property is completely excluded
```

---

#### REQ-MAP-005: Case-Insensitive Property Matching

**Requirement Statement:**
The library SHALL use case-insensitive property matching by default when mapping query results.

**Rationale:**
JSON fields may use different casing than C# properties; case-insensitive matching improves compatibility.

**Acceptance Criteria:**

```gherkin
Scenario: Match property with different casing
  Given a User class with property FullName
  And a SQL++ query returns {"fullname": "John Doe"}
  When the query result is mapped to User
  Then user.FullName equals "John Doe"

Scenario: Match property with snake_case to PascalCase
  Given a User class with property FirstName
  And a SQL++ query returns {"first_name": "John"}
  And naming convention is configured to handle snake_case
  When the query result is mapped to User
  Then user.FirstName equals "John"

Scenario: Exact match takes precedence over case-insensitive
  Given a User class with properties Name and name (if allowed) or single Name
  And a SQL++ query returns {"Name": "Exact", "name": "Insensitive"}
  When the query result is mapped to User
  Then the exact case match "Name" is used
```

---

#### REQ-MAP-006: Nested Object Mapping

**Requirement Statement:**
The library SHALL support nested object mapping for embedded documents in query results.

**Rationale:**
Couchbase documents often contain nested objects; mapping should handle arbitrary nesting depth.

**Acceptance Criteria:**

```gherkin
Scenario: Map nested object
  Given a User class with property Address of type Address
  And Address has properties: Street, City, Country
  And a SQL++ query returns {"name": "John", "address": {"street": "123 Main", "city": "NYC", "country": "USA"}}
  When the query result is mapped to User
  Then user.Address is not null
  And user.Address.Street equals "123 Main"
  And user.Address.City equals "NYC"

Scenario: Map deeply nested objects
  Given a User class with Address.Coordinates.Latitude (3 levels deep)
  And a SQL++ query returns nested JSON structure
  When the query result is mapped to User
  Then all nested levels are correctly populated

Scenario: Nested object is null in JSON
  Given a User class with property Address of type Address
  And a SQL++ query returns {"name": "John", "address": null}
  When the query result is mapped to User
  Then user.Address is null
  And no exception is thrown
```

---

#### REQ-MAP-007: Collection Property Mapping

**Requirement Statement:**
The library SHALL support mapping JSON arrays to collection properties (arrays, List<T>, IEnumerable<T>).

**Rationale:**
Documents frequently contain arrays; supporting collections is essential for real-world use.

**Acceptance Criteria:**

```gherkin
Scenario: Map JSON array to List<T>
  Given a User class with property Tags of type List<string>
  And a SQL++ query returns {"name": "John", "tags": ["vip", "active", "premium"]}
  When the query result is mapped to User
  Then user.Tags contains 3 elements
  And user.Tags contains "vip", "active", "premium"

Scenario: Map JSON array to array
  Given a User class with property Scores of type int[]
  And a SQL++ query returns {"scores": [85, 92, 78]}
  When the query result is mapped to User
  Then user.Scores is an array with 3 elements
  And values are 85, 92, 78

Scenario: Map JSON array of objects to List<T>
  Given a User class with Orders of type List<Order>
  And a SQL++ query returns {"orders": [{"id": "o1", "total": 99.99}, {"id": "o2", "total": 149.99}]}
  When the query result is mapped to User
  Then user.Orders contains 2 Order objects
  And orders are correctly mapped with Id and Total properties

Scenario: Empty JSON array maps to empty collection
  Given a User class with Tags of type List<string>
  And a SQL++ query returns {"tags": []}
  When the query result is mapped to User
  Then user.Tags is an empty list (not null)
  And user.Tags.Count equals 0
```

---

#### REQ-MAP-008: Nullable Type Support

**Requirement Statement:**
The library SHALL support nullable value types and nullable reference types in mapping.

**Rationale:**
Nullable types are common in databases; proper handling prevents NullReferenceExceptions.

**Acceptance Criteria:**

```gherkin
Scenario: Map null JSON value to nullable value type
  Given a User class with property Age of type int?
  And a SQL++ query returns {"name": "John", "age": null}
  When the query result is mapped to User
  Then user.Age is null (not 0)

Scenario: Map JSON value to nullable value type
  Given a User class with property Age of type int?
  And a SQL++ query returns {"name": "John", "age": 25}
  When the query result is mapped to User
  Then user.Age equals 25

Scenario: Map missing field to nullable reference type
  Given a User class with nullable reference type string? MiddleName
  And a SQL++ query returns {"firstName": "John", "lastName": "Doe"}
  When the query result is mapped to User
  Then user.MiddleName is null

Scenario: Map null to non-nullable value type throws or uses default
  Given a User class with property Age of type int (non-nullable)
  And a SQL++ query returns {"age": null}
  When the query result is mapped to User
  Then either default(int) = 0 is used (configurable)
  Or a MappingException is thrown (strict mode)
```

---

#### REQ-MAP-009: Custom Type Converters

**Requirement Statement:**
The library SHALL support custom type converters via an `ITypeConverter` interface for non-standard type mappings.

**Rationale:**
Custom types (e.g., value objects, domain types) require user-defined conversion logic.

**Acceptance Criteria:**

```gherkin
Scenario: Register and use custom type converter
  Given a Money class that stores value as long (cents)
  And a custom MoneyConverter implementing ITypeConverter
  And the converter is registered with SimpleMapper
  When a SQL++ query returns {"price": 9999} (meaning $99.99)
  Then the Money property is correctly converted using MoneyConverter

Scenario: Custom converter for date format
  Given a custom DateConverter that parses "dd/MM/yyyy" format
  And the converter is registered for DateTime type
  When a SQL++ query returns {"birthDate": "25/12/1990"}
  Then the DateTime property is correctly parsed as December 25, 1990

Scenario: Converter exception provides context
  Given a custom converter that throws on invalid data
  When a SQL++ query returns invalid data for that type
  Then a MappingException is thrown
  And the exception includes property name, value, and converter type
```

---

#### REQ-MAP-010: Constructor-Based Initialization

**Requirement Statement:**
The library SHALL support constructor-based initialization for immutable types.

**Rationale:**
Immutable types and records use constructor parameters; the mapper must support this pattern.

**Acceptance Criteria:**

```gherkin
Scenario: Map to type with parameterized constructor
  Given a record User(string Id, string Name, int Age)
  And a SQL++ query returns {"id": "u1", "name": "John", "age": 30}
  When the query result is mapped to User
  Then the constructor is called with ("u1", "John", 30)
  And the User instance is correctly created

Scenario: Constructor parameter matching by name
  Given a class with constructor(string name, int age)
  And properties Name and Age with private setters
  And a SQL++ query returns {"name": "John", "age": 30}
  When the query result is mapped
  Then constructor parameters are matched by name (case-insensitive)
  And the object is correctly initialized

Scenario: Mixed constructor and property initialization
  Given a class with constructor(string id) and public string Name { get; set; }
  And a SQL++ query returns {"id": "u1", "name": "John"}
  When the query result is mapped
  Then id is passed to constructor
  And Name is set via property setter

Scenario: No suitable constructor found
  Given a class with only a private parameterless constructor
  When attempting to map query results
  Then a MappingException is thrown
  And the message indicates no suitable constructor was found
```

---

### 3.3 Query Execution

---

#### REQ-QUERY-001: Raw SQL++ Query Support

**Requirement Statement:**
The library SHALL support execution of raw SQL++ queries with parameter binding and result mapping.

**Rationale:**
Raw query support provides flexibility for complex queries that cannot be expressed via builders.

**Acceptance Criteria:**

```gherkin
Scenario: Execute raw SQL++ query with parameters
  Given a connected scope
  And a SQL++ query "SELECT * FROM users WHERE age > $minAge"
  And parameter minAge = 21
  When calling QueryAsync<User>(query, new { minAge = 21 })
  Then the query is executed against Couchbase
  And results are mapped to User objects
  And only users with age > 21 are returned

Scenario: Execute query without parameters
  Given a connected scope
  And a SQL++ query "SELECT * FROM users"
  When calling QueryAsync<User>(query)
  Then the query executes successfully
  And all users are returned

Scenario: Query with multiple parameters
  Given a SQL++ query with parameters $minAge, $maxAge, $status
  When calling QueryAsync with new { minAge = 18, maxAge = 65, status = "active" }
  Then all parameters are bound correctly
  And the query executes with the correct filters
```

---

#### REQ-QUERY-002: Anonymous Object Parameters

**Requirement Statement:**
The library SHALL support anonymous objects for query parameter binding.

**Rationale:**
Anonymous objects provide concise, type-safe parameter specification.

**Acceptance Criteria:**

```gherkin
Scenario: Bind parameters from anonymous object
  Given a query "SELECT * FROM users WHERE name = $name AND age = $age"
  When calling QueryAsync with new { name = "John", age = 30 }
  Then parameter $name is bound to "John"
  And parameter $age is bound to 30
  And the query executes correctly

Scenario: Anonymous object with nested property (flattened)
  Given a query with parameter $city
  When calling QueryAsync with new { city = "NYC" }
  Then $city is bound to "NYC"

Scenario: Parameter name mismatch handling
  Given a query with parameter $userName
  When calling QueryAsync with new { name = "John" }
  Then a QueryException is thrown
  And the message indicates missing parameter $userName
```

---

#### REQ-QUERY-003: Dictionary Parameters

**Requirement Statement:**
The library SHALL support `Dictionary<string, object>` for query parameter binding.

**Rationale:**
Dictionary parameters support dynamic parameter construction at runtime.

**Acceptance Criteria:**

```gherkin
Scenario: Bind parameters from Dictionary
  Given a query "SELECT * FROM users WHERE status = $status"
  And a Dictionary<string, object> { ["status"] = "active" }
  When calling QueryAsync with the dictionary
  Then parameter $status is bound to "active"
  And the query executes correctly

Scenario: Dictionary with various value types
  Given a Dictionary with int, string, bool, DateTime values
  When binding parameters from the dictionary
  Then all values are correctly converted to Couchbase types
  And the query executes with correct parameter types

Scenario: Null dictionary value
  Given a Dictionary with { ["name"] = null }
  When binding parameters
  Then $name is bound as JSON null
  And the query handles NULL comparison correctly
```

---

#### REQ-QUERY-004: QueryAsync for Multiple Results

**Requirement Statement:**
The library SHALL provide `QueryAsync<T>` method that returns multiple mapped results.

**Rationale:**
Most queries return multiple rows; this is the primary query method.

**Acceptance Criteria:**

```gherkin
Scenario: QueryAsync returns all matching results
  Given 100 users in the database
  And a query "SELECT * FROM users WHERE age > 18"
  And 75 users match the condition
  When calling QueryAsync<User>(query)
  Then an IEnumerable<User> with 75 items is returned
  And all items are correctly mapped User objects

Scenario: QueryAsync with no results returns empty enumerable
  Given a query that matches no documents
  When calling QueryAsync<User>(query)
  Then an empty IEnumerable<User> is returned (not null)
  And enumerable.Count() equals 0

Scenario: QueryAsync streams results
  Given a query that returns 10,000 rows
  When calling QueryAsync<User>(query) and iterating
  Then results are streamed (not all loaded into memory at once)
  And memory usage remains bounded
```

---

#### REQ-QUERY-005: QueryFirstAsync for Single Result (Required)

**Requirement Statement:**
The library SHALL provide `QueryFirstAsync<T>` that returns the first result and throws if no results exist.

**Rationale:**
When a result is expected, throwing on empty helps catch logic errors early.

**Acceptance Criteria:**

```gherkin
Scenario: QueryFirstAsync returns first matching result
  Given 10 users matching the query condition
  When calling QueryFirstAsync<User>(query)
  Then a single User object is returned
  And it is the first result from the query

Scenario: QueryFirstAsync throws when no results
  Given a query that matches no documents
  When calling QueryFirstAsync<User>(query)
  Then an InvalidOperationException is thrown
  And the message indicates "Sequence contains no elements"

Scenario: QueryFirstAsync with multiple results
  Given a query that returns 5 results
  When calling QueryFirstAsync<User>(query)
  Then only the first result is returned
  And no exception is thrown
```

---

#### REQ-QUERY-006: QueryFirstOrDefaultAsync for Optional Single Result

**Requirement Statement:**
The library SHALL provide `QueryFirstOrDefaultAsync<T>` that returns the first result or default(T) if none.

**Rationale:**
Optional lookups should not throw; returning null/default is more appropriate.

**Acceptance Criteria:**

```gherkin
Scenario: QueryFirstOrDefaultAsync returns first result
  Given users matching the query condition
  When calling QueryFirstOrDefaultAsync<User>(query)
  Then the first matching User is returned

Scenario: QueryFirstOrDefaultAsync returns null when no results
  Given a query that matches no documents
  When calling QueryFirstOrDefaultAsync<User>(query)
  Then null is returned (for reference types)
  And no exception is thrown

Scenario: QueryFirstOrDefaultAsync with value type
  Given a query returning int values
  And the query matches no documents
  When calling QueryFirstOrDefaultAsync<int>(query)
  Then default(int) = 0 is returned
```

---

#### REQ-QUERY-007: QuerySingleAsync for Exactly One Result

**Requirement Statement:**
The library SHALL provide `QuerySingleAsync<T>` that throws if not exactly one result exists.

**Rationale:**
When exactly one result is expected, violations indicate data integrity issues.

**Acceptance Criteria:**

```gherkin
Scenario: QuerySingleAsync returns single result
  Given exactly one document matches the query
  When calling QuerySingleAsync<User>(query)
  Then the single User is returned

Scenario: QuerySingleAsync throws on no results
  Given no documents match the query
  When calling QuerySingleAsync<User>(query)
  Then an InvalidOperationException is thrown
  And the message indicates "Sequence contains no elements"

Scenario: QuerySingleAsync throws on multiple results
  Given 2 or more documents match the query
  When calling QuerySingleAsync<User>(query)
  Then an InvalidOperationException is thrown
  And the message indicates "Sequence contains more than one element"
```

---

#### REQ-QUERY-008: ExecuteAsync for Non-Query Operations

**Requirement Statement:**
The library SHALL provide `ExecuteAsync` for non-query operations (UPDATE, DELETE) returning affected count.

**Rationale:**
Modification operations need a method that returns metrics rather than mapped objects.

**Acceptance Criteria:**

```gherkin
Scenario: ExecuteAsync returns affected row count for UPDATE
  Given 10 users with status = "active"
  And an UPDATE query setting status = "inactive" for age > 60
  And 3 users have age > 60
  When calling ExecuteAsync(updateQuery, parameters)
  Then the return value is 3 (or mutationCount from Couchbase)

Scenario: ExecuteAsync returns affected row count for DELETE
  Given 5 documents matching delete criteria
  When calling ExecuteAsync(deleteQuery, parameters)
  Then the return value reflects deleted document count

Scenario: ExecuteAsync with no affected rows
  Given an UPDATE query that matches no documents
  When calling ExecuteAsync(query, parameters)
  Then 0 is returned
  And no exception is thrown
```

---

#### REQ-QUERY-009: Async-Only API

**Requirement Statement:**
All query methods SHALL be async-only with no synchronous overloads.

**Rationale:**
Couchbase SDK is async; forcing async prevents blocking thread pool threads and improves scalability.

**Acceptance Criteria:**

```gherkin
Scenario: All public query methods return Task or Task<T>
  Given the SimpleMapper public API
  When inspecting all query method signatures
  Then all methods return Task<T> or Task
  And method names end with "Async" suffix

Scenario: No GetAwaiter().GetResult() calls internally
  Given the SimpleMapper source code
  When searching for .Result, .Wait(), or GetAwaiter().GetResult()
  Then no synchronous blocking calls are found
  And all async operations properly use await

Scenario: Attempting sync call causes compile error
  Given user code trying to call QueryAsync without await
  When compiling the code
  Then the code compiles but returns Task<T> (not T)
  And using the result without await produces a warning or error
```

---

#### REQ-QUERY-010: Cancellation Token Support

**Requirement Statement:**
All query methods SHALL support query cancellation via `CancellationToken` parameter.

**Rationale:**
Long-running queries should be cancellable to support request timeouts and graceful shutdown.

**Acceptance Criteria:**

```gherkin
Scenario: Query respects cancellation token
  Given a long-running query
  And a CancellationTokenSource
  When calling QueryAsync with the token
  And cancellation is requested during execution
  Then OperationCanceledException or TaskCanceledException is thrown
  And the query is cancelled on the server (best effort)

Scenario: Cancelled token before execution
  Given a pre-cancelled CancellationToken
  When calling QueryAsync with the cancelled token
  Then OperationCanceledException is thrown immediately
  And no query is sent to the server

Scenario: Default cancellation token when not provided
  Given QueryAsync called without CancellationToken
  When the query executes
  Then CancellationToken.None is used internally
  And the query completes normally
```

---

#### REQ-QUERY-011: Query Options Support

**Requirement Statement:**
All query methods SHALL support query options including timeout, scan consistency, and other Couchbase query settings.

**Rationale:**
Different queries have different consistency and performance requirements.

**Acceptance Criteria:**

```gherkin
Scenario: Override query timeout
  Given a QueryOptions with Timeout = 5 seconds
  When calling QueryAsync with options
  Then the query uses a 5-second timeout
  And TimeoutException is thrown if exceeded

Scenario: Set scan consistency to RequestPlus
  Given a QueryOptions with ScanConsistency = RequestPlus
  When calling QueryAsync with options
  Then the query waits for index consistency
  And results include all prior mutations

Scenario: Use default options when none provided
  Given SimpleMapper configured with default timeout of 30 seconds
  When calling QueryAsync without explicit options
  Then the 30-second default timeout is used
  And default scan consistency is applied
```

---

### 3.4 CRUD Operations

---

#### REQ-CRUD-001: Get Document by Key

**Requirement Statement:**
The library SHALL provide `GetAsync<T>(key)` to retrieve a single document by its key and map it to type T.

**Rationale:**
Key-based lookup is the most efficient Couchbase operation; direct support is essential.

**Acceptance Criteria:**

```gherkin
Scenario: Get existing document by key
  Given a document with key "user::123" exists in the collection
  When calling collection.GetAsync<User>("user::123")
  Then the document is retrieved
  And mapped to a User object with correct property values

Scenario: Get non-existent document by key
  Given no document exists with key "user::999"
  When calling collection.GetAsync<User>("user::999")
  Then null is returned (or DocumentNotFoundException based on config)

Scenario: Get document with wrong type mapping
  Given a document with key "order::123" (Order type)
  When calling collection.GetAsync<User>("order::123")
  Then mapping attempts to populate User properties
  And missing/mismatched properties result in nulls/defaults
```

---

#### REQ-CRUD-002: Get Multiple Documents by Keys

**Requirement Statement:**
The library SHALL provide `GetAsync<T>(keys)` to retrieve multiple documents by their keys in a single operation.

**Rationale:**
Batch retrieval is more efficient than individual lookups for multiple documents.

**Acceptance Criteria:**

```gherkin
Scenario: Get multiple existing documents
  Given documents with keys "user::1", "user::2", "user::3" exist
  When calling collection.GetAsync<User>(["user::1", "user::2", "user::3"])
  Then 3 User objects are returned
  And all are correctly mapped

Scenario: Get multiple documents with some missing
  Given documents "user::1", "user::2" exist but "user::3" does not
  When calling collection.GetAsync<User>(["user::1", "user::2", "user::3"])
  Then 2 User objects are returned for existing documents
  And missing documents are excluded or returned as null (configurable)

Scenario: Get with empty key collection
  Given an empty list of keys
  When calling collection.GetAsync<User>(emptyList)
  Then an empty IEnumerable<User> is returned
  And no database operation is performed
```

---

#### REQ-CRUD-003: Insert Document

**Requirement Statement:**
The library SHALL provide `InsertAsync<T>(entity)` to insert a new document, failing if the key already exists.

**Rationale:**
Insert semantics prevent accidental overwrites of existing documents.

**Acceptance Criteria:**

```gherkin
Scenario: Insert new document successfully
  Given a new User object with Id = "user::new"
  And no document with that key exists
  When calling collection.InsertAsync(user)
  Then the document is created in Couchbase
  And document content matches the serialized User object

Scenario: Insert fails for existing key
  Given a document with key "user::123" already exists
  And a User object with Id = "user::123"
  When calling collection.InsertAsync(user)
  Then a DocumentExistsException is thrown
  And the existing document is not modified

Scenario: Insert with auto-generated key
  Given a User object with Id = null
  And key generation strategy is Guid
  When calling collection.InsertAsync(user)
  Then a new GUID-based key is generated
  And the document is created with that key
  And user.Id is updated with the generated key
```

---

#### REQ-CRUD-004: Upsert Document

**Requirement Statement:**
The library SHALL provide `UpsertAsync<T>(entity)` to insert or update a document.

**Rationale:**
Upsert simplifies code when the caller doesn't know if the document exists.

**Acceptance Criteria:**

```gherkin
Scenario: Upsert creates new document
  Given no document exists with key "user::new"
  And a User object with Id = "user::new"
  When calling collection.UpsertAsync(user)
  Then a new document is created
  And content matches the serialized User

Scenario: Upsert updates existing document
  Given a document exists with key "user::123" and name = "Old Name"
  And a User object with Id = "user::123" and name = "New Name"
  When calling collection.UpsertAsync(user)
  Then the document is updated
  And the name field becomes "New Name"

Scenario: Upsert replaces entire document
  Given an existing document with fields A, B, C
  And a User object with only fields A, B
  When calling collection.UpsertAsync(user)
  Then the document contains only fields A, B (C is removed)
  And this is full replacement, not partial update
```

---

#### REQ-CRUD-005: Replace Document

**Requirement Statement:**
The library SHALL provide `ReplaceAsync<T>(entity)` to update an existing document, failing if it doesn't exist.

**Rationale:**
Replace semantics ensure the document exists, catching logic errors when update is expected.

**Acceptance Criteria:**

```gherkin
Scenario: Replace existing document
  Given a document exists with key "user::123"
  And a User object with Id = "user::123" and updated properties
  When calling collection.ReplaceAsync(user)
  Then the document is updated with new content
  And the operation succeeds

Scenario: Replace non-existent document fails
  Given no document exists with key "user::999"
  And a User object with Id = "user::999"
  When calling collection.ReplaceAsync(user)
  Then a DocumentNotFoundException is thrown
  And no document is created

Scenario: Replace with CAS for optimistic concurrency
  Given a document with key "user::123" and CAS value
  And the entity has the original CAS stored
  When calling collection.ReplaceAsync(user) with CAS
  Then replacement succeeds if CAS matches
  And fails with CasMismatchException if document was modified
```

---

#### REQ-CRUD-006: Remove Document by Key

**Requirement Statement:**
The library SHALL provide `RemoveAsync<T>(key)` to delete a document by its key.

**Rationale:**
Direct key-based deletion is a fundamental operation.

**Acceptance Criteria:**

```gherkin
Scenario: Remove existing document by key
  Given a document exists with key "user::123"
  When calling collection.RemoveAsync<User>("user::123")
  Then the document is deleted from Couchbase
  And subsequent GetAsync returns null

Scenario: Remove non-existent document
  Given no document exists with key "user::999"
  When calling collection.RemoveAsync<User>("user::999")
  Then a DocumentNotFoundException is thrown (or silent based on config)

Scenario: Remove with empty key
  Given an empty string as key
  When calling collection.RemoveAsync<User>("")
  Then an ArgumentException is thrown
  And the message indicates key cannot be empty
```

---

#### REQ-CRUD-007: Remove Document by Entity

**Requirement Statement:**
The library SHALL provide `RemoveAsync<T>(entity)` to delete a document using the entity's key property.

**Rationale:**
Removing by entity is convenient when the entity is already in memory.

**Acceptance Criteria:**

```gherkin
Scenario: Remove document using entity
  Given a User object with Id = "user::123"
  And the document exists in Couchbase
  When calling collection.RemoveAsync(user)
  Then the document with key "user::123" is deleted

Scenario: Remove entity without key property
  Given an entity type without [Key] attribute or Id property
  When calling collection.RemoveAsync(entity)
  Then an InvalidOperationException is thrown
  And the message indicates no key property found

Scenario: Remove entity with null key
  Given a User object with Id = null
  When calling collection.RemoveAsync(user)
  Then an ArgumentException is thrown
  And the message indicates key cannot be null
```

---

#### REQ-CRUD-008: Optimistic Concurrency via CAS

**Requirement Statement:**
The library SHALL support optimistic concurrency via Couchbase's Compare-And-Swap (CAS) mechanism.

**Rationale:**
CAS prevents lost updates when multiple clients modify the same document.

**Acceptance Criteria:**

```gherkin
Scenario: CAS prevents concurrent update
  Given a document with key "user::123" and CAS = 12345
  And two clients read the document simultaneously
  When client A updates with original CAS (succeeds, new CAS = 12346)
  And client B updates with original CAS (12345)
  Then client B receives ConcurrencyException
  And client B's update is rejected

Scenario: CAS value tracking via interface
  Given an entity implementing ICasEntity with CasValue property
  When the entity is retrieved via GetAsync
  Then CasValue is populated from Couchbase
  And subsequent Replace/Upsert uses this CAS value

Scenario: Disable CAS checking
  Given an entity without CAS tracking
  When calling ReplaceAsync with ignoreCas option
  Then the update proceeds regardless of concurrent modifications
  And last-write-wins semantics apply
```

---

#### REQ-CRUD-009: Document Expiration (TTL)

**Requirement Statement:**
The library SHALL support document expiration (Time-To-Live) when inserting or updating documents.

**Rationale:**
TTL is useful for session data, caches, and temporary documents.

**Acceptance Criteria:**

```gherkin
Scenario: Insert document with TTL
  Given a User object
  And insert options with Expiry = 1 hour
  When calling collection.InsertAsync(user, options)
  Then the document is created with 1-hour expiration
  And the document is automatically deleted after 1 hour

Scenario: Update document preserves existing TTL
  Given a document with 30-minute remaining TTL
  When calling ReplaceAsync without expiry option
  Then the document is updated
  And the original TTL is preserved

Scenario: Update document with new TTL
  Given a document with existing TTL
  And upsert options with Expiry = 2 hours
  When calling UpsertAsync(user, options)
  Then the document TTL is updated to 2 hours
```

---

#### REQ-CRUD-010: Auto-Generate Document Keys

**Requirement Statement:**
The library SHALL auto-generate document keys if not provided, using a configurable strategy.

**Rationale:**
Auto-generation simplifies insertion of new documents without manual key management.

**Acceptance Criteria:**

```gherkin
Scenario: Generate GUID key
  Given key generation strategy is Guid
  And a User object with Id = null
  When calling InsertAsync(user)
  Then a new GUID is generated as the key
  And user.Id is set to the generated GUID
  And the document is created with that key

Scenario: Generate composite key from properties
  Given key generation strategy is Composite with pattern "{type}::{email}"
  And a User object with Email = "john@example.com"
  When calling InsertAsync(user)
  Then key is generated as "user::john@example.com"
  And user.Id is set to the generated key

Scenario: Key generation with prefix
  Given key prefix configured as "prod:"
  And key generation strategy is Guid
  When calling InsertAsync(user)
  Then the generated key starts with "prod:"
  And format is "prod:{guid}"

Scenario: No key generation when key provided
  Given a User object with Id = "my-custom-key"
  When calling InsertAsync(user)
  Then the provided key "my-custom-key" is used
  And no auto-generation occurs
```

---

## 4. Filter Builder (Critical Feature)

---

#### REQ-FILTER-001: Fluent WHERE Clause Builder

**Requirement Statement:**
The library SHALL provide a fluent API for building SQL++ WHERE clauses programmatically.

**Rationale:**
Fluent builders enable dynamic query construction while maintaining type safety.

**Acceptance Criteria:**

```gherkin
Scenario: Build simple filter
  Given Filter<User>.Create()
  When calling .Where(u => u.Age > 21).Build()
  Then the result contains WHERE clause "age > $p1"
  And parameters contain { p1: 21 }

Scenario: Chain multiple conditions
  Given Filter<User>.Create()
  When calling .Where(u => u.Age > 21).And(u => u.Status == "active").Build()
  Then the WHERE clause is "age > $p1 AND status = $p2"
  And parameters contain { p1: 21, p2: "active" }

Scenario: Empty filter produces no WHERE clause
  Given Filter<User>.Create()
  When calling .Build() without any conditions
  Then the WHERE clause is empty string
  And parameters dictionary is empty
```

---

#### REQ-FILTER-002: Type-Safe Lambda Expressions

**Requirement Statement:**
The library SHALL support type-safe filter expressions using C# lambda expressions.

**Rationale:**
Lambda expressions provide compile-time checking and IDE support (IntelliSense, refactoring).

**Acceptance Criteria:**

```gherkin
Scenario: Property access via lambda
  Given Filter<User>.Create()
  When calling .Where(u => u.Email == "test@example.com")
  Then the expression is parsed correctly
  And generates "email = $p1" (or mapped column name)

Scenario: Compile-time error for invalid property
  Given Filter<User>.Create()
  When writing code .Where(u => u.InvalidProperty == "x")
  Then a compile-time error occurs
  And IDE shows property does not exist on User

Scenario: Complex expression support
  Given Filter<User>.Create()
  When calling .Where(u => u.Age + 5 > 25)
  Then the expression "age + 5 > $p1" is generated
  And parameter p1 = 25
```

---

#### REQ-FILTER-003: Parameterized SQL++ Generation

**Requirement Statement:**
The library SHALL translate filter expressions to parameterized SQL++ to prevent SQL injection.

**Rationale:**
Parameterized queries are essential for security and performance (query plan caching).

**Acceptance Criteria:**

```gherkin
Scenario: Values become parameters
  Given a filter .Where(u => u.Name == "John'; DROP TABLE users;--")
  When building the filter
  Then the WHERE clause is "name = $p1"
  And $p1 value is the literal string (not executed as SQL)
  And SQL injection is prevented

Scenario: Consistent parameter naming
  Given multiple filter conditions
  When building the filter
  Then parameters are named $p1, $p2, $p3, etc.
  And each parameter maps to the correct value

Scenario: Parameter reuse for same value
  Given .Where(u => u.Status == "active").And(u => u.Type == "active")
  When building the filter
  Then either two parameters exist with same value
  Or implementation may optimize to reuse $p1 for both
```

---

#### REQ-FILTER-004: SQL Injection Prevention

**Requirement Statement:**
The library SHALL prevent SQL injection by always using parameters for values and validating identifiers.

**Rationale:**
Security is non-negotiable; the library must be safe by default.

**Acceptance Criteria:**

```gherkin
Scenario: User input in filter value is parameterized
  Given user-provided input "malicious'; DELETE FROM users;--"
  When used in .Where(u => u.Name == userInput)
  Then the value is passed as a parameter
  And no SQL code is executed

Scenario: Property names are validated
  Given the filter builder parsing lambda expressions
  When property names are extracted
  Then they are validated against the entity type
  And arbitrary strings cannot be injected as column names

Scenario: WhereRaw validates parameter usage
  Given .WhereRaw("status = $status", new { status = "active" })
  When the filter is built
  Then the raw SQL is included with parameters bound
  And direct string interpolation is not used
```

---

#### REQ-FILTER-005: AND/OR Operators

**Requirement Statement:**
The library SHALL support combining filters with AND and OR operators.

**Rationale:**
Complex query logic requires Boolean operators to combine conditions.

**Acceptance Criteria:**

```gherkin
Scenario: Combine with AND
  Given Filter<User>.Create()
  When calling .Where(u => u.Age > 18).And(u => u.Age < 65)
  Then WHERE clause is "age > $p1 AND age < $p2"
  And parameters are { p1: 18, p2: 65 }

Scenario: Combine with OR
  Given Filter<User>.Create()
  When calling .Where(u => u.Role == "admin").Or(u => u.Role == "superuser")
  Then WHERE clause is "role = $p1 OR role = $p2"

Scenario: Mixed AND/OR precedence
  Given Filter<User>.Create()
  When calling .Where(A).Or(B).And(C)
  Then WHERE clause respects operator precedence
  And produces "A OR B AND C" (AND binds tighter)
  Or requires explicit grouping for clarity
```

---

#### REQ-FILTER-006: Filter Grouping

**Requirement Statement:**
The library SHALL support grouping of filter conditions for complex Boolean logic.

**Rationale:**
Parenthetical grouping is necessary to express complex AND/OR combinations.

**Acceptance Criteria:**

```gherkin
Scenario: Group with AndGroup
  Given Filter<User>.Create()
  When calling .Where(u => u.Country == "USA").AndGroup(g => g.Where(u => u.Age >= 21).Or(u => u.HasConsent))
  Then WHERE clause is "country = $p1 AND (age >= $p2 OR hasConsent = $p3)"

Scenario: Nested groups
  Given complex nesting with OrGroup inside AndGroup
  When building the filter
  Then parentheses are correctly nested
  And the Boolean logic is preserved

Scenario: Empty group handling
  Given .AndGroup(g => { /* no conditions */ })
  When building the filter
  Then the empty group is omitted
  And no empty parentheses appear in WHERE clause
```

---

#### REQ-FILTER-007: Negation Support

**Requirement Statement:**
The library SHALL support negation of conditions using NOT operator.

**Rationale:**
Negation is a fundamental Boolean operation for expressing exclusion criteria.

**Acceptance Criteria:**

```gherkin
Scenario: Negate simple condition
  Given Filter<User>.Create()
  When calling .WhereNot(u => u.Status == "deleted")
  Then WHERE clause is "NOT (status = $p1)"

Scenario: Negate using != operator
  Given .Where(u => u.Status != "deleted")
  When building the filter
  Then WHERE clause is "status != $p1" or "NOT status = $p1"

Scenario: Negate a group
  Given .WhereNotGroup(g => g.Where(A).Or(B))
  When building the filter
  Then WHERE clause is "NOT (A OR B)"
```

---

#### REQ-FILTER-008: Composable and Reusable Filters

**Requirement Statement:**
Filters SHALL be composable and reusable across multiple queries.

**Rationale:**
Common filter patterns (e.g., "active users", "recent orders") should be defined once and reused.

**Acceptance Criteria:**

```gherkin
Scenario: Combine two filters
  Given activeFilter = Filter<User>.Create().Where(u => u.IsActive)
  And adultFilter = Filter<User>.Create().Where(u => u.Age >= 18)
  When combining with activeFilter.And(adultFilter)
  Then the combined filter has both conditions
  And WHERE clause is "isActive = $p1 AND age >= $p2"

Scenario: Reuse filter in multiple queries
  Given a predefined filter recentOrdersFilter
  When used in Query1 and Query2
  Then both queries use the same filter logic
  And filter can be built multiple times with fresh parameters

Scenario: Filter immutability
  Given a filter instance filterA
  When calling filterA.And(condition)
  Then a new filter instance is returned
  And filterA is not modified (immutable)
```

---

### 4.2 Filter Operations

---

#### REQ-FILTER-OP-001: Equality and Inequality

**Requirement Statement:**
The library SHALL support equals (==) and not equals (!=) comparison operators.

**Acceptance Criteria:**

```gherkin
Scenario: Equals operator
  Given .Where(u => u.Status == "active")
  When building the filter
  Then WHERE clause is "status = $p1"
  And $p1 = "active"

Scenario: Not equals operator
  Given .Where(u => u.Status != "deleted")
  When building the filter
  Then WHERE clause is "status != $p1"
  And $p1 = "deleted"

Scenario: Equals with null
  Given .Where(u => u.DeletedAt == null)
  When building the filter
  Then WHERE clause is "deletedAt IS NULL"
  And no parameter is created for null
```

---

#### REQ-FILTER-OP-002: Comparison Operators

**Requirement Statement:**
The library SHALL support greater than, less than, greater than or equal, and less than or equal operators.

**Acceptance Criteria:**

```gherkin
Scenario: Greater than
  Given .Where(u => u.Age > 21)
  When building the filter
  Then WHERE clause is "age > $p1"
  And $p1 = 21

Scenario: Less than or equal
  Given .Where(u => u.Balance <= 1000)
  When building the filter
  Then WHERE clause is "balance <= $p1"

Scenario: Chained comparisons for range
  Given .Where(u => u.Age >= 18).And(u => u.Age <= 65)
  When building the filter
  Then WHERE clause is "age >= $p1 AND age <= $p2"
```

---

#### REQ-FILTER-OP-003: String Contains/StartsWith/EndsWith

**Requirement Statement:**
The library SHALL support string pattern matching with Contains, StartsWith, and EndsWith.

**Acceptance Criteria:**

```gherkin
Scenario: String Contains
  Given .Where(u => u.Name.Contains("john"))
  When building the filter
  Then WHERE clause is "name LIKE $p1"
  And $p1 = "%john%"

Scenario: String StartsWith
  Given .Where(u => u.Email.StartsWith("admin"))
  When building the filter
  Then WHERE clause is "email LIKE $p1"
  And $p1 = "admin%"

Scenario: String EndsWith
  Given .Where(u => u.Domain.EndsWith(".com"))
  When building the filter
  Then WHERE clause is "domain LIKE $p1"
  And $p1 = "%.com"

Scenario: Contains with special characters escaped
  Given .Where(u => u.Name.Contains("50% off"))
  When building the filter
  Then the % in the value is escaped properly
  And matches literal "50% off" not a wildcard
```

---

#### REQ-FILTER-OP-004: IN and NOT IN

**Requirement Statement:**
The library SHALL support IN and NOT IN operators for checking membership in a collection.

**Acceptance Criteria:**

```gherkin
Scenario: WhereIn with list of values
  Given .WhereIn(u => u.Status, ["active", "pending", "review"])
  When building the filter
  Then WHERE clause is "status IN [$p1, $p2, $p3]"
  And parameters contain the three values

Scenario: WhereNotIn
  Given .WhereNotIn(u => u.Category, ["spam", "deleted"])
  When building the filter
  Then WHERE clause is "status NOT IN [$p1, $p2]"

Scenario: WhereIn with empty collection
  Given .WhereIn(u => u.Status, [])
  When building the filter
  Then WHERE clause is "FALSE" or equivalent
  And query returns no results (empty IN is always false)
```

---

#### REQ-FILTER-OP-005: IS NULL and IS NOT NULL

**Requirement Statement:**
The library SHALL support null checking with IS NULL and IS NOT NULL operators.

**Acceptance Criteria:**

```gherkin
Scenario: IS NULL check
  Given .Where(u => u.DeletedAt == null)
  When building the filter
  Then WHERE clause is "deletedAt IS NULL"

Scenario: IS NOT NULL check
  Given .Where(u => u.Email != null)
  When building the filter
  Then WHERE clause is "email IS NOT NULL"

Scenario: Nullable property has value
  Given .Where(u => u.OptionalField.HasValue) for nullable type
  When building the filter
  Then WHERE clause is "optionalField IS NOT NULL"
```

---

#### REQ-FILTER-OP-006: BETWEEN

**Requirement Statement:**
The library SHALL support BETWEEN operator for range queries.

**Acceptance Criteria:**

```gherkin
Scenario: WhereBetween for numeric range
  Given .WhereBetween(u => u.Age, 18, 65)
  When building the filter
  Then WHERE clause is "age BETWEEN $p1 AND $p2"
  And $p1 = 18, $p2 = 65

Scenario: WhereBetween for date range
  Given .WhereBetween(u => u.CreatedAt, startDate, endDate)
  When building the filter
  Then WHERE clause uses BETWEEN with date parameters
  And dates are correctly formatted for Couchbase

Scenario: WhereBetween is inclusive
  Given .WhereBetween(u => u.Score, 0, 100)
  When querying documents with score = 0 or score = 100
  Then both boundary values are included in results
```

---

#### REQ-FILTER-OP-007: Array Contains

**Requirement Statement:**
The library SHALL support checking if an array property contains a specific value.

**Acceptance Criteria:**

```gherkin
Scenario: WhereArrayContains for string array
  Given a User with Tags = ["vip", "premium", "active"]
  And .WhereArrayContains(u => u.Tags, "vip")
  When building and executing the filter
  Then WHERE clause uses "ANY t IN tags SATISFIES t = $p1 END"
  And the user with "vip" tag is returned

Scenario: WhereArrayContains with no match
  Given .WhereArrayContains(u => u.Tags, "nonexistent")
  When executing the filter
  Then no results are returned for users without that tag

Scenario: WhereArrayContains on nested array
  Given .WhereArrayContains(u => u.Address.ZipCodes, "10001")
  When building the filter
  Then the nested path is correctly traversed
  And ANY/SATISFIES syntax is used
```

---

#### REQ-FILTER-OP-008: Array Any with Predicate

**Requirement Statement:**
The library SHALL support checking if any element in an array satisfies a predicate.

**Acceptance Criteria:**

```gherkin
Scenario: WhereAny with predicate
  Given a User with Orders = [{ Total: 50 }, { Total: 150 }, { Total: 75 }]
  And .WhereAny(u => u.Orders, o => o.Total > 100)
  When building and executing the filter
  Then WHERE clause uses "ANY o IN orders SATISFIES o.total > $p1 END"
  And users with at least one order > 100 are returned

Scenario: WhereAny with complex predicate
  Given .WhereAny(u => u.Orders, o => o.Status == "completed" && o.Total > 50)
  When building the filter
  Then the compound predicate is correctly translated
  And wrapped in ANY/SATISFIES/END

Scenario: WhereAll with predicate
  Given .WhereAll(u => u.Scores, s => s >= 60)
  When building the filter
  Then WHERE clause uses "EVERY s IN scores SATISFIES s >= $p1 END"
```

---

#### REQ-FILTER-OP-009: Nested Property Access

**Requirement Statement:**
The library SHALL support filtering on nested object properties using dot notation.

**Acceptance Criteria:**

```gherkin
Scenario: Filter on nested property
  Given .Where(u => u.Address.City == "New York")
  When building the filter
  Then WHERE clause is "address.city = $p1"
  And dot notation is preserved for Couchbase

Scenario: Deep nesting
  Given .Where(u => u.Company.Address.Country.Code == "US")
  When building the filter
  Then WHERE clause is "company.address.country.code = $p1"

Scenario: Nested property with Column attribute
  Given Address.PostalCode has [Column("zip_code")]
  And .Where(u => u.Address.PostalCode == "10001")
  When building the filter
  Then WHERE clause is "address.zip_code = $p1"
```

---

#### REQ-FILTER-OP-010: Raw SQL++ Escape Hatch

**Requirement Statement:**
The library SHALL provide WhereRaw for custom SQL++ fragments that cannot be expressed via the builder.

**Rationale:**
Complex or database-specific expressions may require raw SQL++ as an escape hatch.

**Acceptance Criteria:**

```gherkin
Scenario: WhereRaw with parameters
  Given .WhereRaw("ARRAY_LENGTH(orders) > $minOrders", new { minOrders = 5 })
  When building the filter
  Then the raw SQL is included in WHERE clause
  And $minOrders parameter is bound to 5

Scenario: WhereRaw combined with fluent filters
  Given .Where(u => u.Status == "active").WhereRaw("LOWER(name) = $lowerName", new { lowerName = "john" })
  When building the filter
  Then both conditions are combined with AND
  And WHERE clause is "status = $p1 AND LOWER(name) = $lowerName"

Scenario: WhereRaw without parameters
  Given .WhereRaw("META().id LIKE 'user::%'")
  When building the filter
  Then the raw SQL is included as-is
  And no parameters are added
```

---

## 5. Pagination (Critical Feature)

---

#### REQ-PAGE-001: Offset-Based Pagination

**Requirement Statement:**
The library SHALL provide offset-based pagination using LIMIT and OFFSET SQL++ clauses.

**Rationale:**
Offset pagination is familiar to developers and suitable for most use cases with moderate data sizes.

**Acceptance Criteria:**

```gherkin
Scenario: Basic pagination with page number
  Given 100 documents in the collection
  And .Page(pageNumber: 2, pageSize: 10)
  When executing the paginated query
  Then OFFSET = 10 (page 2 starts at item 11)
  And LIMIT = 10
  And results contain items 11-20

Scenario: First page
  Given .Page(pageNumber: 1, pageSize: 25)
  When executing the paginated query
  Then OFFSET = 0
  And LIMIT = 25
  And first 25 items are returned

Scenario: Skip and Take
  Given .Skip(50).Take(20)
  When executing the paginated query
  Then OFFSET = 50
  And LIMIT = 20
```

---

#### REQ-PAGE-002: Keyset (Cursor-Based) Pagination

**Requirement Statement:**
The library SHALL provide keyset pagination for better performance on large datasets.

**Rationale:**
Keyset pagination avoids the performance degradation of high OFFSET values.

**Acceptance Criteria:**

```gherkin
Scenario: Initial keyset page
  Given .OrderBy(u => u.Id).Take(20)
  When executing without After clause
  Then first 20 items are returned sorted by Id
  And continuation info is available for next page

Scenario: Subsequent keyset page using After
  Given first page ends with Id = "user::050"
  And .OrderBy(u => u.Id).After("user::050").Take(20)
  When executing the paginated query
  Then WHERE clause includes "id > $lastId"
  And results start after "user::050"

Scenario: Keyset with descending order
  Given .OrderByDescending(u => u.CreatedAt).After(lastDate).Take(20)
  When executing the paginated query
  Then WHERE clause includes "createdAt < $lastDate"
  And results are in descending order after the cursor

Scenario: Keyset with composite key
  Given .OrderBy(u => u.Status).ThenBy(u => u.Id).After(("active", "user::100"))
  When executing the paginated query
  Then WHERE handles both sort columns correctly
  And results resume after the composite cursor position
```

---

#### REQ-PAGE-003: Pagination Metadata

**Requirement Statement:**
The library SHALL return pagination metadata including total count, page info, and navigation flags.

**Rationale:**
Metadata enables UI to display pagination controls and progress information.

**Acceptance Criteria:**

```gherkin
Scenario: PagedResult contains all metadata
  Given a paginated query with IncludeTotalCount()
  When executing the query
  Then result.Items contains the page data
  And result.PageNumber equals the requested page
  And result.PageSize equals the requested size
  And result.TotalCount equals total matching documents
  And result.TotalPages is calculated correctly
  And result.HasPreviousPage and result.HasNextPage are set

Scenario: Calculate HasNextPage correctly
  Given TotalCount = 95 and PageSize = 10 and PageNumber = 9
  When calculating pagination metadata
  Then HasNextPage = true (page 10 has 5 items)

Scenario: Calculate HasNextPage for last page
  Given TotalCount = 100 and PageSize = 10 and PageNumber = 10
  When calculating pagination metadata
  Then HasNextPage = false
  And HasPreviousPage = true
```

---

#### REQ-PAGE-004: Configurable Page Size

**Requirement Statement:**
The library SHALL support configurable page size with sensible defaults and maximum limits.

**Rationale:**
Reasonable defaults and limits prevent accidental large queries that could impact performance.

**Acceptance Criteria:**

```gherkin
Scenario: Use default page size
  Given default page size configured as 25
  And .Page(pageNumber: 1) without specifying pageSize
  When executing the paginated query
  Then pageSize defaults to 25
  And LIMIT = 25

Scenario: Enforce maximum page size
  Given maximum page size configured as 1000
  And .Page(pageNumber: 1, pageSize: 5000)
  When executing the paginated query
  Then pageSize is capped at 1000
  And LIMIT = 1000 (not 5000)

Scenario: Custom page size within limits
  Given .Page(pageNumber: 1, pageSize: 50)
  When executing the paginated query
  Then pageSize = 50 is used
  And LIMIT = 50
```

---

#### REQ-PAGE-005: Single and Multiple Column Sorting

**Requirement Statement:**
The library SHALL support sorting by single or multiple columns.

**Rationale:**
Complex data often requires multi-column sorting for predictable ordering.

**Acceptance Criteria:**

```gherkin
Scenario: Single column sort
  Given .OrderBy(u => u.LastName)
  When executing the paginated query
  Then ORDER BY lastName ASC is in the query

Scenario: Multiple column sort
  Given .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
  When executing the paginated query
  Then ORDER BY lastName ASC, firstName ASC is in the query

Scenario: Mixed sort directions
  Given .OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id)
  When executing the paginated query
  Then ORDER BY createdAt DESC, id ASC is in the query
```

---

#### REQ-PAGE-006: Sort Direction

**Requirement Statement:**
The library SHALL support ascending and descending sort directions.

**Rationale:**
Both directions are commonly needed; DESC for recent-first, ASC for alphabetical, etc.

**Acceptance Criteria:**

```gherkin
Scenario: Ascending sort (default)
  Given .OrderBy(u => u.Name)
  When executing the paginated query
  Then ORDER BY name ASC is applied

Scenario: Descending sort
  Given .OrderByDescending(u => u.CreatedAt)
  When executing the paginated query
  Then ORDER BY createdAt DESC is applied

Scenario: ThenByDescending
  Given .OrderBy(u => u.Status).ThenByDescending(u => u.Priority)
  When executing the paginated query
  Then ORDER BY status ASC, priority DESC is applied
```

---

#### REQ-PAGE-007: Filter Integration with Pagination

**Requirement Statement:**
The library SHALL integrate the filter builder seamlessly with pagination.

**Rationale:**
Filtering and pagination are almost always used together in real applications.

**Acceptance Criteria:**

```gherkin
Scenario: Paginate filtered results
  Given .WithFilter(f => f.Where(u => u.Status == "active"))
      .OrderBy(u => u.Name)
      .Page(1, 20)
  When executing the paginated query
  Then WHERE clause includes status filter
  And ORDER BY and LIMIT/OFFSET are applied
  And only active users are returned, paginated

Scenario: Filter affects total count
  Given 100 users total, 40 are active
  And .WithFilter(f => f.Where(u => u.Status == "active")).IncludeTotalCount()
  When executing the paginated query
  Then TotalCount = 40 (not 100)

Scenario: Configure filter inline
  Given .WithFilter(f => f
          .Where(u => u.Age >= 18)
          .And(u => u.Country == "USA"))
      .Page(1, 10)
  When executing the paginated query
  Then both filter conditions are in WHERE clause
```

---

#### REQ-PAGE-008: Optimized COUNT Queries

**Requirement Statement:**
The library SHALL optimize COUNT queries when total count is requested.

**Rationale:**
COUNT queries should not fetch document content and should use covering indexes when possible.

**Acceptance Criteria:**

```gherkin
Scenario: COUNT query is separate from data query
  Given .WithFilter(f => ...).Page(1, 20).IncludeTotalCount()
  When executing the paginated query
  Then two queries are executed
  And data query: SELECT ... LIMIT 20 OFFSET 0
  And count query: SELECT COUNT(*) AS count ...
  And both use the same WHERE clause

Scenario: COUNT query has no ORDER BY
  Given a paginated query with sorting
  When the COUNT query is generated
  Then ORDER BY is omitted from COUNT query
  And only SELECT COUNT(*) with WHERE is executed

Scenario: COUNT query runs in parallel
  Given a paginated query with IncludeTotalCount()
  When executing
  Then data and count queries run in parallel
  And total response time is max(data_time, count_time)
```

---

#### REQ-PAGE-009: Optional Total Count

**Requirement Statement:**
The library SHALL support skipping total count calculation for performance.

**Rationale:**
COUNT queries on large datasets can be expensive; skipping when not needed improves performance.

**Acceptance Criteria:**

```gherkin
Scenario: Skip total count by default
  Given .Page(1, 20) without IncludeTotalCount()
  When executing the paginated query
  Then only data query is executed
  And result.TotalCount is null
  And result.TotalPages is null

Scenario: Explicitly request total count
  Given .Page(1, 20).IncludeTotalCount()
  When executing the paginated query
  Then both data and count queries execute
  And result.TotalCount has a value

Scenario: HasNextPage without total count
  Given .Page(1, 20) without total count
  When executing (fetches 21 items internally, returns 20)
  Then HasNextPage can be determined by fetching pageSize + 1
  And returning only pageSize items
```

---

## 6. Query Builder (Fluent API)

---

#### REQ-QB-001: Fluent Query Builder

**Requirement Statement:**
The library SHALL provide a fluent query builder as an alternative to raw SQL++ strings.

**Rationale:**
Fluent builders improve code readability and maintainability while preventing syntax errors.

**Acceptance Criteria:**

```gherkin
Scenario: Build query fluently
  Given scope.Query<User>()
      .Where(u => u.Status == "active")
      .OrderBy(u => u.Name)
      .Take(10)
  When executing the query
  Then valid SQL++ is generated
  And results are returned mapped to User

Scenario: Query builder produces parameterized query
  Given a fluent query with filter conditions
  When inspecting the generated query
  Then all values are parameterized
  And no inline values in SQL++ string
```

---

#### REQ-QB-002: SELECT with Projection

**Requirement Statement:**
The query builder SHALL support SELECT with column projection to retrieve partial documents.

**Rationale:**
Projections reduce data transfer and improve performance when only certain fields are needed.

**Acceptance Criteria:**

```gherkin
Scenario: Project specific columns
  Given .Select(u => new { u.Id, u.Name, u.Email })
  When executing the query
  Then SELECT clause includes only id, name, email
  And results map to anonymous type or DTO

Scenario: Project with computed expression
  Given .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
  When executing the query
  Then FullName is computed in SQL++ or after mapping

Scenario: Select all (default)
  Given .Query<User>() without Select
  When executing the query
  Then SELECT u.* or SELECT * FROM ... is used
  And full document is retrieved
```

---

#### REQ-QB-003: WHERE with Filter Builder

**Requirement Statement:**
The query builder SHALL support WHERE clause via the filter builder.

**Rationale:**
Filter builder integration provides consistent filtering across pagination and query builder APIs.

**Acceptance Criteria:**

```gherkin
Scenario: WHERE using lambda
  Given .Where(u => u.Age > 21)
  When building the query
  Then WHERE age > $p1 is in the query

Scenario: WHERE using filter builder
  Given .WithFilter(f => f.Where(...).And(...))
  When building the query
  Then filter conditions are in WHERE clause

Scenario: Multiple WHERE calls combine with AND
  Given .Where(A).Where(B)
  When building the query
  Then WHERE A AND B is generated
```

---

#### REQ-QB-004: ORDER BY Support

**Requirement Statement:**
The query builder SHALL support ORDER BY clause for sorting results.

**Rationale:**
Sorting is fundamental for presenting data in a meaningful order.

**Acceptance Criteria:**

```gherkin
Scenario: OrderBy single column
  Given .OrderBy(u => u.CreatedAt)
  When building the query
  Then ORDER BY createdAt ASC is in the query

Scenario: OrderBy with direction
  Given .OrderByDescending(u => u.Score)
  When building the query
  Then ORDER BY score DESC is in the query

Scenario: Multiple OrderBy
  Given .OrderBy(u => u.Status).ThenByDescending(u => u.CreatedAt)
  When building the query
  Then ORDER BY status ASC, createdAt DESC is generated
```

---

#### REQ-QB-005: LIMIT and OFFSET

**Requirement Statement:**
The query builder SHALL support LIMIT and OFFSET for result set restriction.

**Rationale:**
LIMIT and OFFSET are the underlying mechanisms for pagination.

**Acceptance Criteria:**

```gherkin
Scenario: Take limits results
  Given .Take(50)
  When building the query
  Then LIMIT 50 is in the query

Scenario: Skip offsets results
  Given .Skip(100)
  When building the query
  Then OFFSET 100 is in the query

Scenario: Skip and Take together
  Given .Skip(20).Take(10)
  When building the query
  Then OFFSET 20 LIMIT 10 is in the query
```

---

#### REQ-QB-006: GROUP BY and HAVING

**Requirement Statement:**
The query builder SHALL support GROUP BY and HAVING clauses for aggregation queries.

**Rationale:**
Grouping and filtering aggregates are common analytical query patterns.

**Acceptance Criteria:**

```gherkin
Scenario: GroupBy single column
  Given .GroupBy(o => o.CustomerId)
  When building the query
  Then GROUP BY customerId is in the query

Scenario: GroupBy multiple columns
  Given .GroupBy(o => new { o.CustomerId, o.Status })
  When building the query
  Then GROUP BY customerId, status is generated

Scenario: Having clause
  Given .GroupBy(o => o.CustomerId)
      .Having(g => g.Count() > 5)
  When building the query
  Then GROUP BY customerId HAVING COUNT(*) > $p1 is generated
```

---

#### REQ-QB-007: Aggregate Functions

**Requirement Statement:**
The query builder SHALL support aggregate functions: COUNT, SUM, AVG, MIN, MAX.

**Rationale:**
Aggregates are essential for reporting and analytics queries.

**Acceptance Criteria:**

```gherkin
Scenario: COUNT aggregate
  Given .GroupBy(o => o.Category)
      .Select(g => new { Category = g.Key, Count = g.Count() })
  When building the query
  Then SELECT category, COUNT(*) AS count GROUP BY category is generated

Scenario: SUM aggregate
  Given .GroupBy(o => o.CustomerId)
      .Select(g => new { CustomerId = g.Key, Total = g.Sum(o => o.Amount) })
  When building the query
  Then SELECT customerId, SUM(amount) AS total is generated

Scenario: Multiple aggregates
  Given .GroupBy(o => o.Status)
      .Select(g => new {
          Status = g.Key,
          Count = g.Count(),
          Total = g.Sum(o => o.Amount),
          Average = g.Avg(o => o.Amount)
      })
  When building the query
  Then all aggregate functions are in SELECT clause
```

---

#### REQ-QB-008: Parameterized Query Generation

**Requirement Statement:**
The query builder SHALL generate fully parameterized queries.

**Rationale:**
Parameterized queries prevent SQL injection and enable query plan caching.

**Acceptance Criteria:**

```gherkin
Scenario: All literals become parameters
  Given .Where(u => u.Age > 21).Where(u => u.Name == "John")
  When inspecting the generated query
  Then WHERE age > $p1 AND name = $p2
  And parameters contain { p1: 21, p2: "John" }

Scenario: No string interpolation in query
  Given any query built via the fluent API
  When inspecting the generated SQL++
  Then no user values appear as literals
  And all values are bound via parameters
```

---

## 7. Error Handling

---

#### REQ-ERR-001: Custom Exception Types

**Requirement Statement:**
The library SHALL define custom exception types for different error scenarios.

**Rationale:**
Typed exceptions enable precise error handling and better error messages.

**Acceptance Criteria:**

```gherkin
Scenario: Mapping error throws MappingException
  Given a query result that cannot be mapped to target type
  When mapping fails
  Then MappingException is thrown
  And includes TargetType, PropertyName, and problematic Value

Scenario: Document not found throws DocumentNotFoundException
  Given GetAsync for non-existent key
  When document is not found (and configured to throw)
  Then DocumentNotFoundException is thrown
  And includes the Key that was not found

Scenario: CAS mismatch throws ConcurrencyException
  Given a Replace with stale CAS value
  When CAS mismatch occurs
  Then ConcurrencyException is thrown
  And includes Key and ExpectedCas
```

---

#### REQ-ERR-002: SDK Exception Wrapping

**Requirement Statement:**
The library SHALL wrap Couchbase SDK exceptions with additional context.

**Rationale:**
Raw SDK exceptions may lack query context; wrapping adds debugging information.

**Acceptance Criteria:**

```gherkin
Scenario: Query error includes query text
  Given a SQL++ syntax error in a query
  When QueryException is thrown
  Then exception.Query contains the SQL++ text
  And exception.InnerException is the original SDK exception

Scenario: Connection error is wrapped
  Given cluster connection fails
  When attempting a query
  Then appropriate exception is thrown
  And original connection error is in InnerException
```

---

#### REQ-ERR-003: Mapping Failure Messages

**Requirement Statement:**
The library SHALL provide meaningful error messages for mapping failures.

**Rationale:**
Clear error messages accelerate debugging of type mismatch issues.

**Acceptance Criteria:**

```gherkin
Scenario: Type mismatch error message
  Given a JSON field with value "not-a-number" for int property
  When mapping fails
  Then MappingException message includes:
    - Target type name
    - Property name
    - Actual value that failed
    - Expected type

Scenario: Missing required property error
  Given strict mode enabled
  And a required property has no corresponding JSON field
  When mapping
  Then MappingException indicates which property is missing
```

---

#### REQ-ERR-004: Query Text in Exceptions

**Requirement Statement:**
The library SHALL include SQL++ query text in exceptions for debugging, configurable for security.

**Rationale:**
Query text aids debugging but may expose sensitive information in production.

**Acceptance Criteria:**

```gherkin
Scenario: Query included when configured
  Given options.IncludeQueryInExceptions = true
  When a query error occurs
  Then exception.Query contains the full SQL++ text

Scenario: Query excluded when configured
  Given options.IncludeQueryInExceptions = false
  When a query error occurs
  Then exception.Query is null or "[Query hidden]"
  And sensitive information is not exposed

Scenario: Parameters optionally included
  Given options.IncludeParametersInExceptions = true
  When a query error occurs
  Then exception.Parameters contains parameter values
  And sensitive values should be redacted based on configuration
```

---

#### REQ-ERR-005: Problem Details Pattern

**Requirement Statement:**
The library SHALL support the problem details pattern (RFC 7807) for HTTP API scenarios.

**Rationale:**
Problem details provide standardized error responses for REST APIs.

**Acceptance Criteria:**

```gherkin
Scenario: Convert exception to ProblemDetails
  Given a MappingException
  When calling exception.ToProblemDetails()
  Then a ProblemDetails object is returned
  And Type, Title, Status, Detail are populated appropriately

Scenario: Include extension members
  Given a QueryException with query context
  When converting to ProblemDetails
  Then additional properties (query, parameters) are in Extensions dictionary
```

---

## 8. Logging and Diagnostics

---

#### REQ-LOG-001: Microsoft.Extensions.Logging Integration

**Requirement Statement:**
The library SHALL integrate with Microsoft.Extensions.Logging for all logging operations.

**Rationale:**
Standard logging integration works with any configured logging provider.

**Acceptance Criteria:**

```gherkin
Scenario: Logger is injected via DI
  Given SimpleMapper registered with DI
  And ILogger<T> is available in the container
  When SimpleMapper logs messages
  Then messages go through the configured ILogger

Scenario: Logger category is correct
  Given SimpleMapper logging
  When inspecting log entries
  Then category is "Couchbase.SimpleMapper.X" (appropriate namespace)
```

---

#### REQ-LOG-002: Query Logging at Debug Level

**Requirement Statement:**
The library SHALL log generated SQL++ queries at Debug log level.

**Rationale:**
Debug-level query logging aids development without impacting production performance.

**Acceptance Criteria:**

```gherkin
Scenario: Query logged at Debug level
  Given logging configured for Debug level
  When executing a query
  Then log entry at Debug level contains the SQL++ query text

Scenario: Query not logged at Info level
  Given logging configured for Info level only
  When executing a query
  Then no query text appears in logs
```

---

#### REQ-LOG-003: Parameter Logging at Trace Level

**Requirement Statement:**
The library SHALL log query parameters at Trace level with optional sanitization.

**Rationale:**
Parameter logging aids debugging but must be optional due to potential sensitive data.

**Acceptance Criteria:**

```gherkin
Scenario: Parameters logged at Trace level
  Given logging configured for Trace level
  When executing a query with parameters
  Then log entry contains parameter names and values

Scenario: Sensitive parameters sanitized
  Given a parameter marked as [Sensitive]
  And Trace logging enabled
  When logging parameters
  Then sensitive values are replaced with "[REDACTED]"
```

---

#### REQ-LOG-004: Query Timing Logging

**Requirement Statement:**
The library SHALL log query execution time at Debug level.

**Rationale:**
Timing information helps identify performance issues during development.

**Acceptance Criteria:**

```gherkin
Scenario: Execution time logged
  Given Debug logging enabled
  When a query completes
  Then log entry includes execution duration in milliseconds

Scenario: Slow query warning
  Given a query takes longer than slowQueryThreshold (e.g., 1000ms)
  When the query completes
  Then a Warning level log entry is created
  And includes the slow query details
```

---

#### REQ-LOG-005: OpenTelemetry Support

**Requirement Statement:**
The library SHALL support OpenTelemetry for distributed tracing.

**Rationale:**
OpenTelemetry is the industry standard for observability in distributed systems.

**Acceptance Criteria:**

```gherkin
Scenario: Activity created for query execution
  Given OpenTelemetry configured with ActivitySource
  When executing a query
  Then an Activity (span) is created
  And includes db.system = "couchbase"
  And includes db.statement (the query)
  And includes timing information

Scenario: Activity links to parent span
  Given an existing trace context
  When executing a SimpleMapper query
  Then the query span is a child of the current span
  And trace context is propagated
```

---

#### REQ-LOG-006: Metrics Exposure

**Requirement Statement:**
The library SHALL expose metrics including query count and latency histogram.

**Rationale:**
Metrics enable monitoring, alerting, and performance analysis in production.

**Acceptance Criteria:**

```gherkin
Scenario: Query count metric
  Given metrics collection enabled
  When 10 queries are executed
  Then query_count metric equals 10
  And metric is tagged with operation type (query/get/insert/etc.)

Scenario: Latency histogram
  Given metrics collection enabled
  When queries with varying durations execute
  Then latency histogram captures distribution
  And percentiles (p50, p95, p99) can be calculated
```

---

## 9. Configuration

---

#### REQ-CFG-001: IOptions Pattern Support

**Requirement Statement:**
The library SHALL support configuration via the IOptions<T> pattern.

**Rationale:**
IOptions is the standard .NET pattern for typed configuration.

**Acceptance Criteria:**

```gherkin
Scenario: Configure via IOptions
  Given services.Configure<SimpleMapperOptions>(options => { ... })
  When SimpleMapper is resolved
  Then configuration from IOptions is applied

Scenario: Options validation
  Given invalid configuration (e.g., negative page size)
  When SimpleMapper initializes
  Then OptionsValidationException is thrown
  And message indicates the invalid setting
```

---

#### REQ-CFG-002: JSON Configuration Support

**Requirement Statement:**
The library SHALL support configuration via JSON files (appsettings.json).

**Rationale:**
JSON configuration enables environment-specific settings without code changes.

**Acceptance Criteria:**

```gherkin
Scenario: Load configuration from appsettings.json
  Given appsettings.json contains:
    {
      "SimpleMapper": {
        "DefaultBucket": "myBucket",
        "DefaultPageSize": 50
      }
    }
  When binding configuration section to SimpleMapperOptions
  Then options.DefaultBucket = "myBucket"
  And options.DefaultPageSize = 50

Scenario: Environment-specific overrides
  Given appsettings.Development.json overrides DefaultPageSize = 10
  When running in Development environment
  Then DefaultPageSize = 10 (overridden value)
```

---

#### REQ-CFG-003: Sensible Defaults

**Requirement Statement:**
The library SHALL provide sensible defaults for all configuration options.

**Rationale:**
Sensible defaults enable zero-configuration for common scenarios.

**Acceptance Criteria:**

```gherkin
Scenario: Default values applied
  Given no explicit configuration
  When SimpleMapper initializes
  Then DefaultScope = "_default"
  And DefaultCollection = "_default"
  And DefaultPageSize = 25
  And DefaultQueryTimeout = 30 seconds
  And NamingConvention = CamelCase

Scenario: Defaults can be overridden
  Given configuration with DefaultPageSize = 100
  When SimpleMapper initializes
  Then DefaultPageSize = 100 (not default 25)
```

---

#### REQ-CFG-004: Per-Query Option Overrides

**Requirement Statement:**
The library SHALL support per-query option overrides.

**Rationale:**
Different queries may need different timeouts, consistency levels, etc.

**Acceptance Criteria:**

```gherkin
Scenario: Override timeout per query
  Given global timeout = 30 seconds
  And query options with Timeout = 5 seconds
  When executing the query with options
  Then 5-second timeout is used for this query only

Scenario: Override scan consistency
  Given global consistency = NotBounded
  And query options with ScanConsistency = RequestPlus
  When executing the query
  Then RequestPlus consistency is used for this query

Scenario: Partial override
  Given query options with only Timeout specified
  When executing the query
  Then timeout is overridden
  And all other settings use global defaults
```

---

## 10. Testing Support

---

#### REQ-TEST-001: Interface-Based Design

**Requirement Statement:**
The library SHALL provide interfaces for all major components to enable testing.

**Rationale:**
Interfaces enable mocking and dependency injection for unit testing.

**Acceptance Criteria:**

```gherkin
Scenario: All public services have interfaces
  Given the SimpleMapper public API
  When examining dependency injection registrations
  Then all services are registered as interface implementations
  And ISimpleMapperContext, IFilterBuilder, etc. exist

Scenario: Interfaces are mockable
  Given a unit test using Moq or NSubstitute
  When mocking ISimpleMapperContext
  Then all methods can be mocked
  And test can verify method calls and return fake data
```

---

#### REQ-TEST-002: In-Memory Implementation

**Requirement Statement:**
The library SHALL provide mock/fake implementations for unit testing.

**Rationale:**
In-memory fakes enable fast unit tests without database dependencies.

**Acceptance Criteria:**

```gherkin
Scenario: InMemorySimpleMapperContext for unit tests
  Given InMemorySimpleMapperContext configured with test data
  When unit test calls QueryAsync
  Then fake data from in-memory store is returned
  And no Couchbase connection is required

Scenario: In-memory supports CRUD operations
  Given InMemorySimpleMapperContext
  When calling InsertAsync, GetAsync, UpdateAsync, RemoveAsync
  Then operations work against in-memory dictionary
  And state is verifiable in tests

Scenario: In-memory filter evaluation
  Given InMemorySimpleMapperContext with test users
  And a filter .Where(u => u.Age > 21)
  When executing the query
  Then filter is evaluated in-memory using LINQ
  And matching records are returned
```

---

#### REQ-TEST-003: Test Container Support

**Requirement Statement:**
The library SHALL support integration testing with Couchbase test containers.

**Rationale:**
Test containers enable realistic integration tests in CI/CD pipelines.

**Acceptance Criteria:**

```gherkin
Scenario: Integration test with Testcontainers
  Given Testcontainers.Couchbase NuGet package
  And a test class with Couchbase container fixture
  When running integration tests
  Then real Couchbase container is started
  And SimpleMapper connects to container
  And tests run against real database

Scenario: Container cleanup after tests
  Given test container started for test class
  When all tests complete
  Then container is stopped and removed
  And no resources are leaked
```

---

#### REQ-TEST-004: Testing Documentation

**Requirement Statement:**
The library SHALL document testing patterns and provide examples.

**Rationale:**
Documentation helps users write effective tests for their applications.

**Acceptance Criteria:**

```gherkin
Scenario: Unit testing example in documentation
  Given the library documentation
  When reading testing section
  Then examples show how to mock ISimpleMapperContext
  And examples show how to use InMemorySimpleMapperContext

Scenario: Integration testing example
  Given the library documentation
  When reading integration testing section
  Then examples show Testcontainers setup
  And examples show test fixture patterns
```

---

## 11. Performance Requirements

---

#### REQ-PERF-001: Compiled Expression Trees

**Requirement Statement:**
The library SHALL use compiled expression trees for object mapping, not runtime reflection.

**Rationale:**
Compiled delegates are orders of magnitude faster than reflection.

**Acceptance Criteria:**

```gherkin
Scenario: Mapper uses compiled delegates
  Given the object mapper implementation
  When inspecting mapping code
  Then Expression.Compile() or similar is used
  And no reflection MethodInfo.Invoke for property access

Scenario: Performance benchmark
  Given 10,000 objects to map
  When benchmarking mapping performance
  Then compiled mapping is at least 10x faster than reflection
  And mapping throughput exceeds 1 million objects/second
```

---

#### REQ-PERF-002: Mapper Caching

**Requirement Statement:**
The library SHALL cache compiled mappers per type.

**Rationale:**
Recompiling mappers for each query is wasteful; caching amortizes compilation cost.

**Acceptance Criteria:**

```gherkin
Scenario: Mapper compiled once per type
  Given 1000 queries returning User type
  When inspecting mapper compilation
  Then User mapper is compiled once
  And cached mapper is reused for subsequent queries

Scenario: Cache is thread-safe
  Given concurrent queries for same type from multiple threads
  When mappers are accessed
  Then no race conditions or duplicate compilations occur
  And cache uses ConcurrentDictionary or similar
```

---

#### REQ-PERF-003: Minimal Allocations

**Requirement Statement:**
The library SHALL minimize allocations during query execution.

**Rationale:**
Reduced allocations improve throughput and reduce GC pressure.

**Acceptance Criteria:**

```gherkin
Scenario: Benchmark allocation rate
  Given a simple query executed 10,000 times
  When profiling allocations
  Then allocation per query is below target (e.g., < 1KB excluding results)
  And no unnecessary boxing of value types

Scenario: Reuse buffers where possible
  Given query parameter building
  When parameters are serialized
  Then buffer pooling (ArrayPool) is used where appropriate
```

---

#### REQ-PERF-004: Streaming Results

**Requirement Statement:**
The library SHALL support streaming results for large datasets.

**Rationale:**
Loading millions of rows into memory is impractical; streaming enables bounded memory usage.

**Acceptance Criteria:**

```gherkin
Scenario: Results are streamed
  Given a query returning 100,000 rows
  When iterating with foreach or IAsyncEnumerable
  Then rows are fetched incrementally
  And memory usage remains bounded
  And not all rows loaded before iteration starts

Scenario: AsAsyncEnumerable support
  Given .Query<User>().Where(...).AsAsyncEnumerable()
  When consuming with await foreach
  Then results stream from Couchbase
  And can be processed one at a time
```

---

#### REQ-PERF-005: Parallel Count Query

**Requirement Statement:**
The library SHALL execute count and data queries in parallel for pagination.

**Rationale:**
Parallel execution reduces total latency for paginated requests.

**Acceptance Criteria:**

```gherkin
Scenario: Count and data queries run in parallel
  Given .Page(1, 20).IncludeTotalCount()
  When executing the paginated query
  Then both queries start concurrently (Task.WhenAll or similar)
  And total time ≈ max(data_query_time, count_query_time)
  And not sum of both times

Scenario: Error in one query surfaces appropriately
  Given count query succeeds but data query fails
  When exception is thrown
  Then data query exception is primary
  And count result is not returned without data
```

---

## 12. Security Requirements

---

#### REQ-SEC-001: Parameterized Queries Only

**Requirement Statement:**
The library SHALL always use parameterized queries to prevent SQL injection.

**Rationale:**
SQL injection is a critical security vulnerability; prevention is mandatory.

**Acceptance Criteria:**

```gherkin
Scenario: All filter values parameterized
  Given any filter built via FilterBuilder
  When inspecting generated SQL++
  Then no filter values appear as literals
  And all values are bound as $pN parameters

Scenario: WhereRaw validates parameter usage
  Given .WhereRaw("name = $name", new { name = value })
  When building the filter
  Then $name is bound from parameters
  And raw SQL cannot contain unparameterized values

Scenario: SQL injection attempt is neutralized
  Given malicious input "'; DROP TABLE users; --"
  When used in a filter
  Then input becomes a parameter value
  And is treated as literal string data
  And no SQL commands execute
```

---

#### REQ-SEC-002: Secure Default Logging

**Requirement Statement:**
The library SHALL NOT log sensitive parameter values by default.

**Rationale:**
Logs may be accessible to unauthorized parties; sensitive data must not leak.

**Acceptance Criteria:**

```gherkin
Scenario: Parameters not logged by default
  Given default logging configuration
  When a query with password parameter executes
  Then parameter values are not in logs
  And only parameter names may be logged

Scenario: Explicit opt-in for parameter logging
  Given options.LogParameterValues = true
  When a query executes
  Then parameter values appear in Trace logs
  And this requires explicit configuration
```

---

#### REQ-SEC-003: Sensitive Property Marking

**Requirement Statement:**
The library SHALL support marking properties as sensitive to exclude them from logs.

**Rationale:**
Some properties (passwords, SSN, etc.) should never appear in logs or exceptions.

**Acceptance Criteria:**

```gherkin
Scenario: Sensitive property excluded from logs
  Given a User class with [Sensitive] public string Password { get; set; }
  When Password is used in a query parameter
  And parameter logging is enabled
  Then Password value is logged as "[SENSITIVE]"

Scenario: Sensitive property in exception
  Given a mapping error for a sensitive property
  When MappingException is created
  Then exception.Value is "[SENSITIVE]" not actual value
```

---

#### REQ-SEC-004: Input Length Validation

**Requirement Statement:**
The library SHALL validate input lengths to prevent denial-of-service attacks.

**Rationale:**
Extremely long inputs can cause performance issues or crashes.

**Acceptance Criteria:**

```gherkin
Scenario: Maximum key length enforced
  Given a document key longer than maximum (e.g., > 250 bytes)
  When calling InsertAsync with that key
  Then ArgumentException is thrown
  And message indicates key is too long

Scenario: Maximum query length
  Given a dynamically built query exceeding maximum length
  When attempting to execute
  Then appropriate exception indicates query too long
  And prevents potential memory issues

Scenario: Maximum filter conditions
  Given a filter with > 1000 conditions (potential DoS)
  When building the filter
  Then FilterBuilderException is thrown
  And message indicates condition limit exceeded
```

---

## 13. API Surface Summary

### Extension Methods on IScope

```csharp
public static class ScopeExtensions
{
    // Raw queries
    Task<IEnumerable<T>> QueryAsync<T>(this IScope scope, string query, object? parameters = null, QueryOptions? options = null, CancellationToken ct = default);
    Task<T> QueryFirstAsync<T>(this IScope scope, string query, object? parameters = null, QueryOptions? options = null, CancellationToken ct = default);
    Task<T?> QueryFirstOrDefaultAsync<T>(this IScope scope, string query, object? parameters = null, QueryOptions? options = null, CancellationToken ct = default);
    Task<T> QuerySingleAsync<T>(this IScope scope, string query, object? parameters = null, QueryOptions? options = null, CancellationToken ct = default);
    Task<int> ExecuteAsync(this IScope scope, string query, object? parameters = null, QueryOptions? options = null, CancellationToken ct = default);

    // Fluent query builder
    IPaginationBuilder<T> Query<T>(this IScope scope);
}
```

### Extension Methods on ICouchbaseCollection

```csharp
public static class CollectionExtensions
{
    // CRUD
    Task<T?> GetAsync<T>(this ICouchbaseCollection collection, string key, GetOptions? options = null, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAsync<T>(this ICouchbaseCollection collection, IEnumerable<string> keys, GetOptions? options = null, CancellationToken ct = default);
    Task InsertAsync<T>(this ICouchbaseCollection collection, T entity, InsertOptions? options = null, CancellationToken ct = default);
    Task UpsertAsync<T>(this ICouchbaseCollection collection, T entity, UpsertOptions? options = null, CancellationToken ct = default);
    Task ReplaceAsync<T>(this ICouchbaseCollection collection, T entity, ReplaceOptions? options = null, CancellationToken ct = default);
    Task RemoveAsync(this ICouchbaseCollection collection, string key, RemoveOptions? options = null, CancellationToken ct = default);
    Task RemoveAsync<T>(this ICouchbaseCollection collection, T entity, RemoveOptions? options = null, CancellationToken ct = default);
}
```

---

## 14. Milestones and Prioritization

### Phase 1: Core Foundation (MVP)
1. Basic object mapping (REQ-MAP-001 through REQ-MAP-008)
2. Raw query execution (REQ-QUERY-001 through REQ-QUERY-011)
3. Basic CRUD operations (REQ-CRUD-001 through REQ-CRUD-007)
4. Simple filter builder (basic operations only)
5. Offset-based pagination

### Phase 2: Enhanced Filtering and Pagination
1. Complete filter builder with all operations
2. Keyset pagination
3. Query builder fluent API
4. Advanced type mapping (converters, constructors)

### Phase 3: Production Readiness
1. Logging and diagnostics
2. OpenTelemetry integration
3. Performance optimization
4. Comprehensive testing utilities
5. Documentation and samples

---

## 15. Open Questions

1. **Naming:** Final package name - `Couchbase.SimpleMapper`, `Couchbase.Dapper`, `CouchQL`?
2. **LINQ Provider:** Should we consider a LINQ provider in future versions?
3. **Source Generators:** Should we use source generators instead of runtime compilation for AOT support?
4. **Transactions:** Should MVP include transaction support?

---

## 16. References

- [Dapper GitHub Repository](https://github.com/DapperLib/Dapper)
- [Couchbase .NET SDK Documentation](https://docs.couchbase.com/dotnet-sdk/current/hello-world/overview.html)
- [SQL++ (N1QL) Reference](https://docs.couchbase.com/server/current/n1ql/n1ql-language-reference/index.html)
- [Couchbase Query Service](https://docs.couchbase.com/server/current/learn/services-and-indexes/services/query-service.html)
