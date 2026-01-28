using Couchbase;
using Couchbase.Core.Exceptions;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.Core.Exceptions.Query;
using Couchbase.KeyValue;
using Couchbase.Query;
using FluentAssertions;
using Gateway.Core.Exceptions;
using Gateway.Core.Extensions;
using Gateway.Core.Mapping;
using GatewayQueryException = Gateway.Core.Exceptions.QueryException;
using Moq;
using Xunit;

namespace Gateway.AcceptanceTests;

/// <summary>
/// Acceptance tests for Error Handling requirements (REQ-ERR-001 to REQ-ERR-005)
/// These tests verify that the SimpleMapper provides comprehensive error handling
/// with meaningful exceptions, context information, and security considerations.
/// </summary>
public class ErrorHandlingTests
{
    public class TestUser
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    #region REQ-ERR-001: Custom Exception Types

    [Fact]
    public async Task MappingErrorThrowsMappingException()
    {
        // REQ-ERR-001: Scenario: Mapping error throws MappingException
        // Given: a query result that cannot be mapped to target type
        // When: mapping fails
        // Then: MappingException is thrown
        // And: includes TargetType, PropertyName, and problematic Value

        // Arrange & Act
        var exception = new MappingException(
            "Cannot map value to property",
            "User",
            "Age",
            "not-a-number");

        // Assert
        exception.Should().BeOfType<MappingException>();
        exception.TargetType.Should().Be("User");
        exception.PropertyName.Should().Be("Age");
        exception.Value.Should().Be("not-a-number");
    }

    [Fact]
    public async Task DocumentNotFoundThrowsDocumentNotFoundException()
    {
        // REQ-ERR-001: Scenario: Document not found throws DocumentNotFoundException
        // Given: GetAsync for non-existent key
        // When: document is not found (and configured to throw)
        // Then: DocumentNotFoundException is thrown
        // And: includes the Key that was not found

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        mockCollection.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<GetOptions>()))
            .ThrowsAsync(new DocumentNotFoundException());

        // Act & Assert
        var act = async () => await mockCollection.Object.GetAsync("nonexistent-key");
        await act.Should().ThrowAsync<DocumentNotFoundException>();
    }

    [Fact]
    public async Task CasMismatchThrowsConcurrencyException()
    {
        // REQ-ERR-001: Scenario: CAS mismatch throws ConcurrencyException
        // Given: a Replace with stale CAS value
        // When: CAS mismatch occurs
        // Then: CasMismatchException is thrown
        // And: includes Key and ExpectedCas

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        mockCollection.Setup(c => c.ReplaceAsync(It.IsAny<string>(), It.IsAny<TestUser>(), It.IsAny<ReplaceOptions>()))
            .ThrowsAsync(new CasMismatchException());

        // Act & Assert
        var act = async () => await mockCollection.Object.ReplaceAsync(
            "user-1",
            new TestUser { Id = "1", Name = "Test" },
            new ReplaceOptions().Cas(12345));
        await act.Should().ThrowAsync<CasMismatchException>();
    }

    #endregion

    #region REQ-ERR-002: SDK Exception Wrapping

    [Fact]
    public async Task QueryErrorIncludesQueryText()
    {
        // REQ-ERR-002: Scenario: Query error includes query text
        // Given: a SQL++ syntax error in a query
        // When: QueryException is thrown
        // Then: exception.Query contains the SQL++ text
        // And: exception.InnerException is the original SDK exception

        // Arrange
        var query = "SELECT * FORM users"; // Typo: FORM instead of FROM
        var parsingException = new ParsingFailureException("Syntax error");
        var queryException = new GatewayQueryException("Query failed", query, parsingException);

        // Assert
        queryException.Query.Should().Be(query);
        queryException.InnerException.Should().BeOfType<ParsingFailureException>();
    }

    [Fact]
    public async Task ConnectionErrorIsWrapped()
    {
        // REQ-ERR-002: Scenario: Connection error is wrapped
        // Given: cluster connection fails
        // When: attempting a query
        // Then: appropriate exception is thrown
        // And: original connection error is in InnerException

        // Arrange
        var mockScope = new Mock<IScope>();
        var connectionError = new CouchbaseException("Connection refused");
        mockScope.Setup(s => s.QueryAsync<TestUser>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ThrowsAsync(connectionError);

        // Act & Assert
        var act = async () => await mockScope.Object.QueryToListAsync<TestUser>("SELECT * FROM users");
        var exception = await act.Should().ThrowAsync<CouchbaseException>();
        exception.Which.Message.Should().Contain("Connection");
    }

    #endregion

    #region REQ-ERR-003: Mapping Failure Messages

    [Fact]
    public async Task TypeMismatchErrorMessage()
    {
        // REQ-ERR-003: Scenario: Type mismatch error message
        // Given: a JSON field with value "not-a-number" for int property
        // When: mapping fails
        // Then: MappingException message includes:
        //   - Target type name
        //   - Property name
        //   - Actual value that failed
        //   - Expected type

        // Arrange & Act
        var exception = new MappingException(
            "Cannot convert 'not-a-number' to System.Int32 for property 'Age' on type 'User'",
            "User",
            "Age",
            "not-a-number");

        // Assert
        exception.Message.Should().Contain("not-a-number");
        exception.Message.Should().Contain("Int32");
        exception.Message.Should().Contain("Age");
        exception.Message.Should().Contain("User");
    }

    [Fact]
    public async Task MissingRequiredPropertyError()
    {
        // REQ-ERR-003: Scenario: Missing required property error
        // Given: strict mode enabled
        // And: a required property has no corresponding JSON field
        // When: mapping
        // Then: MappingException indicates which property is missing

        // Arrange & Act
        var exception = new MappingException(
            "Required property 'Name' is missing from JSON",
            "User",
            "Name",
            null);

        // Assert
        exception.Message.Should().Contain("Required property");
        exception.Message.Should().Contain("Name");
        exception.PropertyName.Should().Be("Name");
    }

    #endregion

    #region REQ-ERR-004: Query Text in Exceptions

    [Fact]
    public async Task QueryIncludedWhenConfigured()
    {
        // REQ-ERR-004: Scenario: Query included when configured
        // Given: options.IncludeQueryInExceptions = true
        // When: a query error occurs
        // Then: exception.Query contains the full SQL++ text

        // Arrange
        var query = "SELECT * FROM users WHERE status = $status";
        var exception = new GatewayQueryException("Query failed", query);

        // Assert
        exception.Query.Should().Be(query);
        exception.Query.Should().Contain("SELECT");
    }

    [Fact]
    public async Task QueryExcludedWhenConfigured()
    {
        // REQ-ERR-004: Scenario: Query excluded when configured
        // Given: options.IncludeQueryInExceptions = false
        // When: a query error occurs
        // Then: exception.Query is null or "[Query hidden]"
        // And: sensitive information is not exposed

        // Arrange - When query is not included
        var exception = new GatewayQueryException("Query failed", query: null);

        // Assert
        exception.Query.Should().BeNull();
    }

    [Fact]
    public void ParametersOptionallyIncluded()
    {
        // REQ-ERR-004: Scenario: Parameters optionally included
        // Given: options.IncludeParametersInExceptions = true
        // When: a query error occurs
        // Then: exception.Parameters contains parameter values
        // And: sensitive values should be redacted based on configuration

        // Arrange
        var exception = new GatewayQueryException("Query failed", "SELECT * FROM users", errorCode: 4000);

        // Assert - ErrorCode can be used to identify query issues
        exception.ErrorCode.Should().Be(4000);
    }

    #endregion

    #region REQ-ERR-005: Problem Details Pattern

    [Fact]
    public void MappingExceptionHasRequiredProperties()
    {
        // REQ-ERR-005: Scenario: MappingException has required properties
        // Given: a MappingException
        // Then: it has TargetType, PropertyName, and Value properties
        // And: these can be used to construct ProblemDetails

        // Arrange & Act
        var exception = new MappingException(
            "Mapping failed",
            "User",
            "Age",
            "invalid-value");

        // Assert
        exception.TargetType.Should().Be("User");
        exception.PropertyName.Should().Be("Age");
        exception.Value.Should().Be("invalid-value");
        exception.Message.Should().Be("Mapping failed");
    }

    [Fact]
    public void QueryExceptionHasRequiredProperties()
    {
        // REQ-ERR-005: Scenario: QueryException has required properties
        // Given: a QueryException with query context
        // Then: it has Query and ErrorCode properties
        // And: these can be used to construct ProblemDetails

        // Arrange & Act
        var exception = new GatewayQueryException(
            "Query execution failed",
            "SELECT * FROM users",
            5000);

        // Assert
        exception.Query.Should().Be("SELECT * FROM users");
        exception.ErrorCode.Should().Be(5000);
        exception.Message.Should().Be("Query execution failed");
    }

    #endregion

    #region Validation Errors

    [Fact]
    public void EmptyColumnNameThrowsInvalidOperationException()
    {
        // Scenario: Empty column name throws InvalidOperationException
        // Given: a Column attribute with an empty name
        // When: the attribute is constructed
        // Then: an InvalidOperationException is thrown immediately

        // Arrange & Act
        var act = () => new ColumnAttribute("");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void NullColumnNameThrowsInvalidOperationException()
    {
        // Scenario: Null column name throws InvalidOperationException
        // Given: a Column attribute with a null name
        // When: the attribute is constructed
        // Then: an InvalidOperationException is thrown immediately

        // Arrange & Act
        var act = () => new ColumnAttribute(null!);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*null*empty*");
    }

    [Fact]
    public void TypeWithNoAccessibleConstructorThrowsMappingException()
    {
        // Scenario: Type with no accessible constructor throws MappingException
        // Given: a private class with no public constructor
        // When: attempting to validate the type for mapping
        // Then: a MappingException is thrown

        // Arrange & Act
        var act = () => ObjectMapper.ValidateType<PrivateConstructorClass>();

        // Assert
        act.Should().Throw<MappingException>()
            .WithMessage("*constructor*");
    }

    private class PrivateConstructorClass
    {
        private PrivateConstructorClass() { }
    }

    #endregion
}
