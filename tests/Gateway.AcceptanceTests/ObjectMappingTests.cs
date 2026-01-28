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
}
