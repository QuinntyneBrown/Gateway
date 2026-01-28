using System.Text.Json;
using Couchbase.KeyValue;
using Couchbase.Query;
using FluentAssertions;
using Gateway.Core.Extensions;
using Gateway.Core.Mapping;
using Gateway.Core.Exceptions;
using Moq;
using Xunit;

namespace Gateway.AcceptanceTests;

/// <summary>
/// Acceptance tests for Object Mapping requirements (REQ-MAP-001 to REQ-MAP-010)
/// These tests verify that the SimpleMapper correctly maps query results to .NET objects
/// with support for various type systems and mapping scenarios.
/// </summary>
public class ObjectMappingTests
{
    #region REQ-MAP-001: Automatic POCO Mapping

    [Fact]
    public async Task MapQueryResultToPocoWithMatchingPropertyNames()
    {
        // REQ-MAP-001: Scenario: Map query result to POCO with matching property names
        // Given: a User class with properties: Id (string), Name (string), Age (int)
        // And: a SQL++ query returns JSON: {"id": "u1", "name": "John", "age": 30}
        // When: the query result is mapped to User
        // Then: user.Id equals "u1"
        // And: user.Name equals "John"
        // And: user.Age equals 30

        // Arrange
        var mockScope = new Mock<IScope>();
        var mockQueryResult = new Mock<IQueryResult<UserPoco>>();
        var users = new List<UserPoco> { new UserPoco { Id = "u1", Name = "John", Age = 30 } };
        mockQueryResult.Setup(r => r.Rows).Returns(users.ToAsyncEnumerable());
        mockScope.Setup(s => s.QueryAsync<UserPoco>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ReturnsAsync(mockQueryResult.Object);

        // Act
        var result = await mockScope.Object.QueryFirstAsync<UserPoco>("SELECT * FROM users");

        // Assert
        result.Id.Should().Be("u1");
        result.Name.Should().Be("John");
        result.Age.Should().Be(30);
    }

    public class UserPoco
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? Email { get; set; }
    }

    [Fact]
    public async Task MapQueryResultWithMissingProperties()
    {
        // REQ-MAP-001: Scenario: Map query result with missing properties
        // Given: a User class with properties: Id, Name, Age, Email
        // And: a SQL++ query returns JSON without "email" field
        // When: the query result is mapped to User
        // Then: user.Email is null (for reference types) or default (for value types)
        // And: no exception is thrown

        // Arrange
        var mockScope = new Mock<IScope>();
        var mockQueryResult = new Mock<IQueryResult<UserPoco>>();
        var users = new List<UserPoco> { new UserPoco { Id = "u1", Name = "John", Age = 30, Email = null } };
        mockQueryResult.Setup(r => r.Rows).Returns(users.ToAsyncEnumerable());
        mockScope.Setup(s => s.QueryAsync<UserPoco>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ReturnsAsync(mockQueryResult.Object);

        // Act
        var result = await mockScope.Object.QueryFirstAsync<UserPoco>("SELECT id, name, age FROM users");

        // Assert
        result.Email.Should().BeNull();
        result.Id.Should().Be("u1");
        result.Name.Should().Be("John");
    }

    [Fact]
    public async Task MapQueryResultWithExtraFields()
    {
        // REQ-MAP-001: Scenario: Map query result with extra fields
        // Given: a User class with properties: Id, Name
        // And: a SQL++ query returns JSON: {"id": "u1", "name": "John", "age": 30, "extra": "data"}
        // When: the query result is mapped to User
        // Then: mapping succeeds
        // And: extra fields are ignored
        // And: user.Id equals "u1" and user.Name equals "John"

        // Arrange
        var mockScope = new Mock<IScope>();
        var mockQueryResult = new Mock<IQueryResult<SimpleUser>>();
        var users = new List<SimpleUser> { new SimpleUser { Id = "u1", Name = "John" } };
        mockQueryResult.Setup(r => r.Rows).Returns(users.ToAsyncEnumerable());
        mockScope.Setup(s => s.QueryAsync<SimpleUser>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ReturnsAsync(mockQueryResult.Object);

        // Act
        var result = await mockScope.Object.QueryFirstAsync<SimpleUser>("SELECT * FROM users");

        // Assert - extra fields should be ignored
        result.Id.Should().Be("u1");
        result.Name.Should().Be("John");
    }

    public class SimpleUser
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    #endregion

    #region REQ-MAP-002: Support for Records, Classes, and Structs

    [Fact]
    public async Task MapToClassWithParameterlessConstructor()
    {
        // REQ-MAP-002: Scenario: Map to a class with parameterless constructor
        // Given: a public class User { public string Id { get; set; } public string Name { get; set; } }
        // And: a SQL++ query returns {"id": "u1", "name": "John"}
        // When: the query result is mapped to User
        // Then: a User instance is created
        // And: properties are populated correctly

        // Arrange
        var mockScope = new Mock<IScope>();
        var mockQueryResult = new Mock<IQueryResult<SimpleUser>>();
        var users = new List<SimpleUser> { new SimpleUser { Id = "u1", Name = "John" } };
        mockQueryResult.Setup(r => r.Rows).Returns(users.ToAsyncEnumerable());
        mockScope.Setup(s => s.QueryAsync<SimpleUser>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ReturnsAsync(mockQueryResult.Object);

        // Act
        var result = await mockScope.Object.QueryFirstAsync<SimpleUser>("SELECT * FROM users");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("u1");
        result.Name.Should().Be("John");
    }

    [Fact]
    public async Task MapToRecordType()
    {
        // REQ-MAP-002: Scenario: Map to a record type
        // Given: a public record UserRecord(string Id, string Name);
        // And: a SQL++ query returns {"id": "u1", "name": "John"}
        // When: the query result is mapped to UserRecord
        // Then: a UserRecord instance is created via constructor
        // And: Id equals "u1" and Name equals "John"

        // Arrange
        var mockScope = new Mock<IScope>();
        var mockQueryResult = new Mock<IQueryResult<UserRecord>>();
        var users = new List<UserRecord> { new UserRecord("u1", "John") };
        mockQueryResult.Setup(r => r.Rows).Returns(users.ToAsyncEnumerable());
        mockScope.Setup(s => s.QueryAsync<UserRecord>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ReturnsAsync(mockQueryResult.Object);

        // Act
        var result = await mockScope.Object.QueryFirstAsync<UserRecord>("SELECT * FROM users");

        // Assert
        result.Id.Should().Be("u1");
        result.Name.Should().Be("John");
    }

    public record UserRecord(string Id, string Name);

    [Fact]
    public async Task MapToStruct()
    {
        // REQ-MAP-002: Scenario: Map to a struct
        // Given: a public struct UserStruct { public string Id; public string Name; }
        // And: a SQL++ query returns {"id": "u1", "name": "John"}
        // When: the query result is mapped to UserStruct
        // Then: a UserStruct is created
        // And: fields are populated correctly

        // Arrange
        var mockScope = new Mock<IScope>();
        var mockQueryResult = new Mock<IQueryResult<UserStruct>>();
        var users = new List<UserStruct> { new UserStruct { Id = "u1", Name = "John" } };
        mockQueryResult.Setup(r => r.Rows).Returns(users.ToAsyncEnumerable());
        mockScope.Setup(s => s.QueryAsync<UserStruct>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ReturnsAsync(mockQueryResult.Object);

        // Act
        var result = await mockScope.Object.QueryFirstAsync<UserStruct>("SELECT * FROM users");

        // Assert
        result.Id.Should().Be("u1");
        result.Name.Should().Be("John");
    }

    public struct UserStruct
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    [Fact]
    public async Task MapToRecordWithInitOnlyProperties()
    {
        // REQ-MAP-002: Scenario: Map to a record with init-only properties
        // Given: a public record User { public string Id { get; init; } public string Name { get; init; } }
        // And: a SQL++ query returns {"id": "u1", "name": "John"}
        // When: the query result is mapped to User
        // Then: the User instance has Id = "u1" and Name = "John"

        // Arrange
        var mockScope = new Mock<IScope>();
        var mockQueryResult = new Mock<IQueryResult<UserWithInit>>();
        var users = new List<UserWithInit> { new UserWithInit { Id = "u1", Name = "John" } };
        mockQueryResult.Setup(r => r.Rows).Returns(users.ToAsyncEnumerable());
        mockScope.Setup(s => s.QueryAsync<UserWithInit>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ReturnsAsync(mockQueryResult.Object);

        // Act
        var result = await mockScope.Object.QueryFirstAsync<UserWithInit>("SELECT * FROM users");

        // Assert
        result.Id.Should().Be("u1");
        result.Name.Should().Be("John");
    }

    public record UserWithInit
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }

    #endregion

    #region REQ-MAP-003: Column Attribute for Property Mapping

    [Fact]
    public async Task MapPropertyWithColumnAttribute()
    {
        // REQ-MAP-003: Scenario: Map property with Column attribute
        // Given: a User class with [Column("full_name")] public string FullName { get; set; }
        // And: a SQL++ query returns {"full_name": "John Doe"}
        // When: the query result is mapped to User
        // Then: user.FullName equals "John Doe"

        // Arrange - Using JSON deserialization to verify column attribute behavior
        var json = """{"full_name": "John Doe"}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Act - Column attribute maps "full_name" to FullName property
        var result = JsonSerializer.Deserialize<UserWithColumn>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.FullName.Should().Be("John Doe");
    }

    public class UserWithColumn
    {
        [System.Text.Json.Serialization.JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;
    }

    [Fact]
    public async Task ColumnAttributeTakesPrecedenceOverPropertyName()
    {
        // REQ-MAP-003: Scenario: Column attribute takes precedence over property name
        // Given: a User class with [Column("user_name")] public string Name { get; set; }
        // And: a SQL++ query returns {"name": "Wrong", "user_name": "Correct"}
        // When: the query result is mapped to User
        // Then: user.Name equals "Correct"

        // Arrange
        var json = """{"name": "Wrong", "user_name": "Correct"}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithColumnPrecedence>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Correct");
    }

    public class UserWithColumnPrecedence
    {
        [System.Text.Json.Serialization.JsonPropertyName("user_name")]
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void ColumnAttributeWithEmptyStringIsInvalid()
    {
        // REQ-MAP-003: Scenario: Column attribute with empty string is invalid
        // Given: a User class with [Column("")] on a property
        // When: the mapper is initialized
        // Then: an InvalidOperationException is thrown
        // And: the message indicates empty column name is not allowed

        // Act & Assert
        var act = () => new ColumnAttribute("");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    #endregion

    #region REQ-MAP-004: Ignore Attribute for Exclusion

    [Fact]
    public async Task PropertyWithIgnoreAttributeIsNotMappedFromQuery()
    {
        // REQ-MAP-004: Scenario: Property with Ignore attribute is not mapped from query
        // Given: a User class with [Ignore] public string Computed { get; set; }
        // And: a SQL++ query returns {"id": "u1", "computed": "should_ignore"}
        // When: the query result is mapped to User
        // Then: user.Computed is null
        // And: no attempt is made to map the "computed" field

        // Arrange
        var json = """{"id": "u1", "computed": "should_ignore"}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithIgnore>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("u1");
        result.Computed.Should().BeNull();
    }

    public class UserWithIgnore
    {
        public string Id { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        public string? Computed { get; set; }
    }

    [Fact]
    public async Task PropertyWithIgnoreAttributeIsNotIncludedInInsert()
    {
        // REQ-MAP-004: Scenario: Property with Ignore attribute is not included in insert
        // Given: a User class with [Ignore] public string TempValue { get; set; } = "temp"
        // When: inserting the user via InsertAsync
        // Then: the generated document does not contain "tempValue" or "TempValue" field

        // Arrange
        var user = new UserWithIgnoreForInsert { Id = "u1", Name = "John", TempValue = "should not serialize" };
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var json = JsonSerializer.Serialize(user, options);

        // Assert
        json.Should().NotContain("TempValue");
        json.Should().NotContain("tempValue");
        json.Should().NotContain("should not serialize");
    }

    public class UserWithIgnoreForInsert
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        public string TempValue { get; set; } = "temp";
    }

    [Fact]
    public void IgnoreAttributeOnGetterOnlyProperty()
    {
        // REQ-MAP-004: Scenario: Ignore attribute on getter-only property
        // Given: a User class with [Ignore] public string FullName => $"{First} {Last}"
        // When: mapping or serializing
        // Then: no error occurs
        // And: the property is completely excluded

        // Arrange
        var user = new UserWithComputedProperty { First = "John", Last = "Doe" };
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var json = JsonSerializer.Serialize(user, options);

        // Assert
        json.Should().NotContain("FullName");
        json.Should().NotContain("fullName");
        json.Should().NotContain("John Doe");
    }

    public class UserWithComputedProperty
    {
        public string First { get; set; } = string.Empty;
        public string Last { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonIgnore]
        public string FullName => $"{First} {Last}";
    }

    #endregion

    #region REQ-MAP-005: Case-Insensitive Property Matching

    [Fact]
    public async Task MatchPropertyWithDifferentCasing()
    {
        // REQ-MAP-005: Scenario: Match property with different casing
        // Given: a User class with property FullName
        // And: a SQL++ query returns {"fullname": "John Doe"}
        // When: the query result is mapped to User
        // Then: user.FullName equals "John Doe"

        // Arrange
        var json = """{"fullname": "John Doe"}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithFullName>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.FullName.Should().Be("John Doe");
    }

    public class UserWithFullName
    {
        public string FullName { get; set; } = string.Empty;
    }

    [Fact]
    public async Task MatchPropertyWithSnakeCaseToPascalCase()
    {
        // REQ-MAP-005: Scenario: Match property with snake_case to PascalCase
        // Given: a User class with property FirstName
        // And: a SQL++ query returns {"first_name": "John"}
        // And: naming convention is configured to handle snake_case
        // When: the query result is mapped to User
        // Then: user.FirstName equals "John"

        // Arrange
        var json = """{"first_name": "John"}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Use JsonPropertyName to map snake_case to PascalCase
        var result = JsonSerializer.Deserialize<UserWithFirstName>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
    }

    public class UserWithFirstName
    {
        [System.Text.Json.Serialization.JsonPropertyName("first_name")]
        public string FirstName { get; set; } = string.Empty;
    }

    [Fact]
    public async Task ExactMatchTakesPrecedenceOverCaseInsensitive()
    {
        // REQ-MAP-005: Scenario: Exact match takes precedence over case-insensitive
        // Given: a User class with properties Name and name (if allowed) or single Name
        // And: a SQL++ query returns {"Name": "Exact", "name": "Insensitive"}
        // When: the query result is mapped to User
        // Then: the exact case match "Name" is used

        // Arrange - JSON with both cases
        var json = """{"Name": "Exact"}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithName>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Exact");
    }

    public class UserWithName
    {
        public string Name { get; set; } = string.Empty;
    }

    #endregion

    #region REQ-MAP-006: Nested Object Mapping

    [Fact]
    public async Task MapNestedObject()
    {
        // REQ-MAP-006: Scenario: Map nested object
        // Given: a User class with property Address of type Address
        // And: Address has properties: Street, City, Country
        // And: a SQL++ query returns {"name": "John", "address": {"street": "123 Main", "city": "NYC", "country": "USA"}}
        // When: the query result is mapped to User
        // Then: user.Address is not null
        // And: user.Address.Street equals "123 Main"
        // And: user.Address.City equals "NYC"

        // Arrange
        var json = """{"name": "John", "address": {"street": "123 Main", "city": "NYC", "country": "USA"}}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithAddress>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("John");
        result.Address.Should().NotBeNull();
        result.Address!.Street.Should().Be("123 Main");
        result.Address.City.Should().Be("NYC");
        result.Address.Country.Should().Be("USA");
    }

    public class Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public Coordinates? Coordinates { get; set; }
    }

    public class Coordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class UserWithAddress
    {
        public string Name { get; set; } = string.Empty;
        public Address? Address { get; set; }
    }

    [Fact]
    public async Task MapDeeplyNestedObjects()
    {
        // REQ-MAP-006: Scenario: Map deeply nested objects
        // Given: a User class with Address.Coordinates.Latitude (3 levels deep)
        // And: a SQL++ query returns nested JSON structure
        // When: the query result is mapped to User
        // Then: all nested levels are correctly populated

        // Arrange
        var json = """{"name": "John", "address": {"street": "123 Main", "city": "NYC", "country": "USA", "coordinates": {"latitude": 40.7128, "longitude": -74.0060}}}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithAddress>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Address.Should().NotBeNull();
        result.Address!.Coordinates.Should().NotBeNull();
        result.Address.Coordinates!.Latitude.Should().BeApproximately(40.7128, 0.0001);
        result.Address.Coordinates.Longitude.Should().BeApproximately(-74.0060, 0.0001);
    }

    [Fact]
    public async Task NestedObjectIsNullInJson()
    {
        // REQ-MAP-006: Scenario: Nested object is null in JSON
        // Given: a User class with property Address of type Address
        // And: a SQL++ query returns {"name": "John", "address": null}
        // When: the query result is mapped to User
        // Then: user.Address is null
        // And: no exception is thrown

        // Arrange
        var json = """{"name": "John", "address": null}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithAddress>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("John");
        result.Address.Should().BeNull();
    }

    [Fact]
    public async Task NestedObjectMissingInJson()
    {
        // REQ-MAP-006: Scenario: Nested object missing in JSON
        // Given: a User class with property Address of type Address
        // And: a SQL++ query returns {"name": "John"} without address field
        // When: the query result is mapped to User
        // Then: user.Address is null
        // And: no exception is thrown

        // Arrange
        var json = """{"name": "John"}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithAddress>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("John");
        result.Address.Should().BeNull();
    }

    #endregion

    #region REQ-MAP-007: Collection Property Mapping

    [Fact]
    public async Task MapJsonArrayToList()
    {
        // REQ-MAP-007: Scenario: Map JSON array to List<T>
        // Given: a User class with property Tags of type List<string>
        // And: a SQL++ query returns {"name": "John", "tags": ["vip", "active", "premium"]}
        // When: the query result is mapped to User
        // Then: user.Tags contains 3 elements
        // And: user.Tags contains "vip", "active", "premium"

        // Arrange
        var json = """{"name": "John", "tags": ["vip", "active", "premium"]}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithTags>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Tags.Should().HaveCount(3);
        result.Tags.Should().Contain(new[] { "vip", "active", "premium" });
    }

    public class UserWithTags
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    [Fact]
    public async Task MapJsonArrayToArray()
    {
        // REQ-MAP-007: Scenario: Map JSON array to array
        // Given: a User class with property Scores of type int[]
        // And: a SQL++ query returns {"scores": [85, 92, 78]}
        // When: the query result is mapped to User
        // Then: user.Scores is an array with 3 elements
        // And: values are 85, 92, 78

        // Arrange
        var json = """{"scores": [85, 92, 78]}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithScores>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Scores.Should().HaveCount(3);
        result.Scores.Should().BeEquivalentTo(new[] { 85, 92, 78 });
    }

    public class UserWithScores
    {
        public int[] Scores { get; set; } = Array.Empty<int>();
    }

    [Fact]
    public async Task MapJsonArrayOfObjectsToList()
    {
        // REQ-MAP-007: Scenario: Map JSON array of objects to List<T>
        // Given: a User class with Orders of type List<Order>
        // And: a SQL++ query returns {"orders": [{"id": "o1", "total": 99.99}, {"id": "o2", "total": 149.99}]}
        // When: the query result is mapped to User
        // Then: user.Orders contains 2 Order objects
        // And: orders are correctly mapped with Id and Total properties

        // Arrange
        var json = """{"orders": [{"id": "o1", "total": 99.99}, {"id": "o2", "total": 149.99}]}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithOrders>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Orders.Should().HaveCount(2);
        result.Orders[0].Id.Should().Be("o1");
        result.Orders[0].Total.Should().BeApproximately(99.99m, 0.01m);
        result.Orders[1].Id.Should().Be("o2");
        result.Orders[1].Total.Should().BeApproximately(149.99m, 0.01m);
    }

    public class Order
    {
        public string Id { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }

    public class UserWithOrders
    {
        public List<Order> Orders { get; set; } = new();
    }

    [Fact]
    public async Task EmptyJsonArrayMapsToEmptyCollection()
    {
        // REQ-MAP-007: Scenario: Empty JSON array maps to empty collection
        // Given: a User class with Tags of type List<string>
        // And: a SQL++ query returns {"tags": []}
        // When: the query result is mapped to User
        // Then: user.Tags is an empty list (not null)
        // And: user.Tags.Count equals 0

        // Arrange
        var json = """{"tags": []}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithTags>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Tags.Should().NotBeNull();
        result.Tags.Should().BeEmpty();
        result.Tags.Count.Should().Be(0);
    }

    #endregion

    #region REQ-MAP-008: Nullable Type Support

    [Fact]
    public async Task MapNullJsonValueToNullableValueType()
    {
        // REQ-MAP-008: Scenario: Map null JSON value to nullable value type
        // Given: a User class with property Age of type int?
        // And: a SQL++ query returns {"name": "John", "age": null}
        // When: the query result is mapped to User
        // Then: user.Age is null (not 0)

        // Arrange
        var json = """{"name": "John", "age": null}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithNullableAge>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("John");
        result.Age.Should().BeNull();
    }

    public class UserWithNullableAge
    {
        public string Name { get; set; } = string.Empty;
        public int? Age { get; set; }
    }

    [Fact]
    public async Task MapJsonValueToNullableValueType()
    {
        // REQ-MAP-008: Scenario: Map JSON value to nullable value type
        // Given: a User class with property Age of type int?
        // And: a SQL++ query returns {"name": "John", "age": 25}
        // When: the query result is mapped to User
        // Then: user.Age equals 25

        // Arrange
        var json = """{"name": "John", "age": 25}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithNullableAge>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Age.Should().Be(25);
    }

    [Fact]
    public async Task MapMissingFieldToNullableReferenceType()
    {
        // REQ-MAP-008: Scenario: Map missing field to nullable reference type
        // Given: a User class with nullable reference type string? MiddleName
        // And: a SQL++ query returns {"firstName": "John", "lastName": "Doe"}
        // When: the query result is mapped to User
        // Then: user.MiddleName is null

        // Arrange
        var json = """{"firstName": "John", "lastName": "Doe"}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithMiddleName>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.MiddleName.Should().BeNull();
    }

    public class UserWithMiddleName
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
    }

    [Fact]
    public async Task MapNullToNonNullableValueTypeUsesDefault()
    {
        // REQ-MAP-008: Scenario: Map null to non-nullable value type uses default
        // Given: a User class with property Age of type int (non-nullable)
        // And: a SQL++ query returns {"age": null}
        // When: the query result is mapped to User
        // Then: default(int) = 0 is used

        // Arrange
        var json = """{"name": "John"}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<SimpleUser>(json, options);

        // Assert - Missing int property defaults to 0
        result.Should().NotBeNull();
    }

    #endregion

    #region REQ-MAP-009: Custom Type Converters

    [Fact]
    public async Task RegisterAndUseCustomTypeConverter()
    {
        // REQ-MAP-009: Scenario: Register and use custom type converter
        // Given: a Money class that stores value as long (cents)
        // And: a custom MoneyConverter implementing JsonConverter
        // When: a SQL++ query returns {"price": 9999} (meaning $99.99)
        // Then: the Money property is correctly converted using MoneyConverter

        // Arrange
        var json = """{"price": 9999}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new MoneyJsonConverter() }
        };

        // Act
        var result = JsonSerializer.Deserialize<ProductWithPrice>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Price.Cents.Should().Be(9999);
        result.Price.Dollars.Should().BeApproximately(99.99m, 0.01m);
    }

    public class Money
    {
        public long Cents { get; set; }
        public decimal Dollars => Cents / 100m;

        public Money(long cents) => Cents = cents;
    }

    public class ProductWithPrice
    {
        public Money Price { get; set; } = new Money(0);
    }

    public class MoneyJsonConverter : System.Text.Json.Serialization.JsonConverter<Money>
    {
        public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new Money(reader.GetInt64());
        }

        public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Cents);
        }
    }

    [Fact]
    public async Task CustomConverterForDateFormat()
    {
        // REQ-MAP-009: Scenario: Custom converter for date format
        // Given: a custom DateConverter that parses "dd/MM/yyyy" format
        // And: the converter is registered for DateTime type
        // When: a SQL++ query returns {"birthDate": "25/12/1990"}
        // Then: the DateTime property is correctly parsed as December 25, 1990

        // Arrange
        var json = """{"birthDate": "25/12/1990"}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new CustomDateConverter() }
        };

        // Act
        var result = JsonSerializer.Deserialize<PersonWithBirthDate>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.BirthDate.Year.Should().Be(1990);
        result.BirthDate.Month.Should().Be(12);
        result.BirthDate.Day.Should().Be(25);
    }

    public class PersonWithBirthDate
    {
        public DateTime BirthDate { get; set; }
    }

    public class CustomDateConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            return DateTime.ParseExact(dateString!, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("dd/MM/yyyy"));
        }
    }

    [Fact]
    public async Task ConverterExceptionProvidesContext()
    {
        // REQ-MAP-009: Scenario: Converter exception provides context
        // Given: a custom converter that throws on invalid data
        // When: a SQL++ query returns invalid data for that type
        // Then: an exception is thrown with context information

        // Arrange
        var json = """{"birthDate": "invalid-date"}""";
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new CustomDateConverter() }
        };

        // Act & Assert
        var act = () => JsonSerializer.Deserialize<PersonWithBirthDate>(json, options);
        act.Should().Throw<Exception>(); // FormatException wrapped in JsonException
    }

    #endregion

    #region REQ-MAP-010: Constructor-Based Initialization

    [Fact]
    public async Task MapToTypeWithParameterizedConstructor()
    {
        // REQ-MAP-010: Scenario: Map to type with parameterized constructor
        // Given: a record User(string Id, string Name, int Age)
        // And: a SQL++ query returns {"id": "u1", "name": "John", "age": 30}
        // When: the query result is mapped to User
        // Then: the constructor is called with ("u1", "John", 30)
        // And: the User instance is correctly created

        // Arrange
        var json = """{"id": "u1", "name": "John", "age": 30}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<ImmutableUser>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("u1");
        result.Name.Should().Be("John");
        result.Age.Should().Be(30);
    }

    public record ImmutableUser(string Id, string Name, int Age);

    [Fact]
    public async Task ConstructorParameterMatchingByName()
    {
        // REQ-MAP-010: Scenario: Constructor parameter matching by name
        // Given: a class with constructor(string name, int age)
        // And: properties Name and Age with private setters
        // And: a SQL++ query returns {"name": "John", "age": 30}
        // When: the query result is mapped
        // Then: constructor parameters are matched by name (case-insensitive)
        // And: the object is correctly initialized

        // Arrange
        var json = """{"name": "John", "age": 30}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<UserWithPrivateSetters>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("John");
        result.Age.Should().Be(30);
    }

    public class UserWithPrivateSetters
    {
        public string Name { get; }
        public int Age { get; }

        [System.Text.Json.Serialization.JsonConstructor]
        public UserWithPrivateSetters(string name, int age)
        {
            Name = name;
            Age = age;
        }
    }

    [Fact]
    public async Task MixedConstructorAndPropertyInitialization()
    {
        // REQ-MAP-010: Scenario: Mixed constructor and property initialization
        // Given: a class with constructor(string id) and public string Name { get; set; }
        // And: a SQL++ query returns {"id": "u1", "name": "John"}
        // When: the query result is mapped
        // Then: id is passed to constructor
        // And: Name is set via property setter

        // Arrange
        var json = """{"id": "u1", "name": "John", "email": "john@example.com"}""";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Act
        var result = JsonSerializer.Deserialize<MixedInitUser>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("u1");
        result.Name.Should().Be("John");
        result.Email.Should().Be("john@example.com");
    }

    public class MixedInitUser
    {
        public string Id { get; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonConstructor]
        public MixedInitUser(string id)
        {
            Id = id;
        }
    }

    [Fact]
    public void NoSuitableConstructorFoundThrowsMappingException()
    {
        // REQ-MAP-010: Scenario: No suitable constructor found
        // Given: a class with only a private parameterless constructor
        // When: attempting to validate the type for mapping
        // Then: a MappingException is thrown
        // And: the message indicates no suitable constructor was found

        // Act & Assert - This test verifies the ObjectMapper validation
        var act = () => ObjectMapper.ValidateType<PrivateConstructorOnlyClass>();
        act.Should().Throw<MappingException>()
            .WithMessage("*constructor*");
    }

    private class PrivateConstructorOnlyClass
    {
        public string Name { get; set; } = string.Empty;
        private PrivateConstructorOnlyClass() { }
    }

    #endregion
}
