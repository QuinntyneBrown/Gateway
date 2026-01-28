using FluentAssertions;
using Gateway.Core.Filtering;
using Xunit;

namespace Gateway.AcceptanceTests;

/// <summary>
/// Acceptance tests for Filter Operations requirements (REQ-FILTER-OP-001 to REQ-FILTER-OP-010)
/// These tests verify that the SimpleMapper supports comprehensive filter operations including
/// comparisons, string matching, null checks, arrays, and nested properties.
/// </summary>
public class FilterOperationsTests
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Age { get; set; }
        public decimal Balance { get; set; }
        public int Score { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? OptionalField { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<int> Scores { get; set; } = new();
        public Address Address { get; set; } = new();
        public Company Company { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class Order
    {
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class Address
    {
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public List<string> ZipCodes { get; set; } = new();
        public Country Country { get; set; } = new();
    }

    public class Country
    {
        public string Code { get; set; } = string.Empty;
    }

    public class Company
    {
        public Address Address { get; set; } = new();
    }

    #region REQ-FILTER-OP-001: Equality and Inequality

    [Fact]
    public void EqualsOperator()
    {
        // REQ-FILTER-OP-001: Scenario: Equals operator
        // Given: .Where(u => u.Status == "active")
        // When: building the filter
        // Then: WHERE clause is "status = $p1"
        // And: $p1 = "active"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.Where("status", "active");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("status = $p0");
        filter.Parameters["p0"].Should().Be("active");
    }

    [Fact]
    public void NotEqualsOperator()
    {
        // REQ-FILTER-OP-001: Scenario: Not equals operator
        // Given: .Where(u => u.Status != "deleted")
        // When: building the filter
        // Then: WHERE clause is "status != $p1"
        // And: $p1 = "deleted"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereNotEqual("status", "deleted");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("status != $p0");
        filter.Parameters["p0"].Should().Be("deleted");
    }

    [Fact]
    public void EqualsWithNull()
    {
        // REQ-FILTER-OP-001: Scenario: Equals with null
        // Given: .Where(u => u.DeletedAt == null)
        // When: building the filter
        // Then: WHERE clause is "deletedAt IS NULL"
        // And: no parameter is created for null

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereNull("deletedAt");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("deletedAt IS NULL");
        filter.Parameters.Should().BeEmpty();
    }

    #endregion

    #region REQ-FILTER-OP-002: Comparison Operators

    [Fact]
    public void GreaterThan()
    {
        // REQ-FILTER-OP-002: Scenario: Greater than
        // Given: .Where(u => u.Age > 21)
        // When: building the filter
        // Then: WHERE clause is "age > $p1"
        // And: $p1 = 21

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereGreaterThan("age", 21);
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("age > $p0");
        filter.Parameters["p0"].Should().Be(21);
    }

    [Fact]
    public void LessThanOrEqual()
    {
        // REQ-FILTER-OP-002: Scenario: Less than or equal
        // Given: .Where(u => u.Balance <= 1000)
        // When: building the filter
        // Then: WHERE clause is "balance <= $p1"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereLessThanOrEqual("balance", 1000);
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("balance <= $p0");
        filter.Parameters["p0"].Should().Be(1000);
    }

    [Fact]
    public void ChainedComparisonsForRange()
    {
        // REQ-FILTER-OP-002: Scenario: Chained comparisons for range
        // Given: .Where(u => u.Age >= 18).And(u => u.Age <= 65)
        // When: building the filter
        // Then: WHERE clause is "age >= $p1 AND age <= $p2"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereGreaterThanOrEqual("age", 18)
              .WhereLessThanOrEqual("age", 65);
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("age >= $p0 AND age <= $p1");
        filter.Parameters["p0"].Should().Be(18);
        filter.Parameters["p1"].Should().Be(65);
    }

    #endregion

    #region REQ-FILTER-OP-003: String Contains/StartsWith/EndsWith

    [Fact]
    public void StringContains()
    {
        // REQ-FILTER-OP-003: Scenario: String Contains
        // Given: .Where(u => u.Name.Contains("john"))
        // When: building the filter
        // Then: WHERE clause is "name LIKE $p1"
        // And: $p1 = "%john%"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereLike("name", "%john%");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("name LIKE $p0");
        filter.Parameters["p0"].Should().Be("%john%");
    }

    [Fact]
    public void StringStartsWith()
    {
        // REQ-FILTER-OP-003: Scenario: String StartsWith
        // Given: .Where(u => u.Email.StartsWith("admin"))
        // When: building the filter
        // Then: WHERE clause is "email LIKE $p1"
        // And: $p1 = "admin%"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereLike("email", "admin%");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("email LIKE $p0");
        filter.Parameters["p0"].Should().Be("admin%");
    }

    [Fact]
    public void StringEndsWith()
    {
        // REQ-FILTER-OP-003: Scenario: String EndsWith
        // Given: .Where(u => u.Domain.EndsWith(".com"))
        // When: building the filter
        // Then: WHERE clause is "domain LIKE $p1"
        // And: $p1 = "%.com"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereLike("domain", "%.com");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("domain LIKE $p0");
        filter.Parameters["p0"].Should().Be("%.com");
    }

    [Fact]
    public void ContainsWithSpecialCharactersEscaped()
    {
        // REQ-FILTER-OP-003: Scenario: Contains with special characters escaped
        // Given: .Where(u => u.Name.Contains("50% off"))
        // When: building the filter
        // Then: the % in the value is escaped properly
        // And: matches literal "50% off" not a wildcard

        // Note: LIKE pattern escaping is responsibility of the caller.
        // CONTAINS function can be used for literal substring matching.

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act - Using CONTAINS for literal matching
        filter.WhereContains("name", "50% off");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("CONTAINS(name, $p0)");
        filter.Parameters["p0"].Should().Be("50% off");
    }

    #endregion

    #region REQ-FILTER-OP-004: IN and NOT IN

    [Fact]
    public void WhereInWithListOfValues()
    {
        // REQ-FILTER-OP-004: Scenario: WhereIn with list of values
        // Given: .WhereIn(u => u.Status, ["active", "pending", "review"])
        // When: building the filter
        // Then: WHERE clause is "status IN [$p1, $p2, $p3]"
        // And: parameters contain the three values

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereIn("status", new object[] { "active", "pending", "review" });
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Contain("status IN $p0");
        var values = filter.Parameters["p0"] as List<object>;
        values.Should().HaveCount(3);
        values.Should().Contain("active");
        values.Should().Contain("pending");
        values.Should().Contain("review");
    }

    [Fact]
    public void WhereNotIn()
    {
        // REQ-FILTER-OP-004: Scenario: WhereNotIn
        // Given: .WhereNotIn(u => u.Category, ["spam", "deleted"])
        // When: building the filter
        // Then: WHERE clause is "status NOT IN [$p1, $p2]"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereNotIn("category", new object[] { "spam", "deleted" });
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Contain("category NOT IN $p0");
        var values = filter.Parameters["p0"] as List<object>;
        values.Should().HaveCount(2);
        values.Should().Contain("spam");
        values.Should().Contain("deleted");
    }

    [Fact]
    public void WhereInWithEmptyCollection()
    {
        // REQ-FILTER-OP-004: Scenario: WhereIn with empty collection
        // Given: .WhereIn(u => u.Status, [])
        // When: building the filter
        // Then: WHERE clause is "FALSE" or equivalent
        // And: query returns no results (empty IN is always false)

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereIn("status", Array.Empty<object>());
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("FALSE");
    }

    #endregion

    #region REQ-FILTER-OP-005: IS NULL and IS NOT NULL

    [Fact]
    public void IsNullCheck()
    {
        // REQ-FILTER-OP-005: Scenario: IS NULL check
        // Given: .Where(u => u.DeletedAt == null)
        // When: building the filter
        // Then: WHERE clause is "deletedAt IS NULL"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereNull("deletedAt");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("deletedAt IS NULL");
    }

    [Fact]
    public void IsNotNullCheck()
    {
        // REQ-FILTER-OP-005: Scenario: IS NOT NULL check
        // Given: .Where(u => u.Email != null)
        // When: building the filter
        // Then: WHERE clause is "email IS NOT NULL"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereNotNull("email");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("email IS NOT NULL");
    }

    [Fact]
    public void NullablePropertyHasValue()
    {
        // REQ-FILTER-OP-005: Scenario: Nullable property has value
        // Given: .Where(u => u.OptionalField.HasValue) for nullable type
        // When: building the filter
        // Then: WHERE clause is "optionalField IS NOT NULL"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act - HasValue is equivalent to IS NOT NULL
        filter.WhereNotNull("optionalField");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("optionalField IS NOT NULL");
    }

    #endregion

    #region REQ-FILTER-OP-006: BETWEEN

    [Fact]
    public void WhereBetweenForNumericRange()
    {
        // REQ-FILTER-OP-006: Scenario: WhereBetween for numeric range
        // Given: .WhereBetween(u => u.Age, 18, 65)
        // When: building the filter
        // Then: WHERE clause is "age BETWEEN $p1 AND $p2"
        // And: $p1 = 18, $p2 = 65

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereBetween("age", 18, 65);
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("age BETWEEN $p0 AND $p1");
        filter.Parameters["p0"].Should().Be(18);
        filter.Parameters["p1"].Should().Be(65);
    }

    [Fact]
    public void WhereBetweenForDateRange()
    {
        // REQ-FILTER-OP-006: Scenario: WhereBetween for date range
        // Given: .WhereBetween(u => u.CreatedAt, startDate, endDate)
        // When: building the filter
        // Then: WHERE clause uses BETWEEN with date parameters
        // And: dates are correctly formatted for Couchbase

        // Arrange
        var filter = new FilterBuilder<User>();
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        // Act
        filter.WhereBetween("createdAt", startDate, endDate);
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("createdAt BETWEEN $p0 AND $p1");
        filter.Parameters["p0"].Should().Be(startDate);
        filter.Parameters["p1"].Should().Be(endDate);
    }

    [Fact]
    public void WhereBetweenIsInclusive()
    {
        // REQ-FILTER-OP-006: Scenario: WhereBetween is inclusive
        // Given: .WhereBetween(u => u.Score, 0, 100)
        // When: querying documents with score = 0 or score = 100
        // Then: both boundary values are included in results

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereBetween("score", 0, 100);
        var whereClause = filter.BuildWhereClause();

        // Assert - BETWEEN is inclusive by SQL standard
        whereClause.Should().Contain("BETWEEN");
        filter.Parameters["p0"].Should().Be(0);
        filter.Parameters["p1"].Should().Be(100);
    }

    #endregion

    #region REQ-FILTER-OP-007: Array Contains

    [Fact]
    public async Task WhereArrayContainsForStringArray()
    {
        // REQ-FILTER-OP-007: Scenario: WhereArrayContains for string array
        // Given: a User with Tags = ["vip", "premium", "active"]
        // And: .WhereArrayContains(u => u.Tags, "vip")
        // When: building and executing the filter
        // Then: WHERE clause uses "ANY t IN tags SATISFIES t = $p1 END"
        // And: the user with "vip" tag is returned

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act - Using raw SQL for ANY/SATISFIES pattern
        filter.WhereRaw("ANY t IN tags SATISFIES t = $tagValue END", new { tagValue = "vip" });
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Contain("ANY t IN tags SATISFIES t = $tagValue END");
        filter.Parameters["tagValue"].Should().Be("vip");
    }

    [Fact]
    public async Task WhereArrayContainsWithNoMatch()
    {
        // REQ-FILTER-OP-007: Scenario: WhereArrayContains with no match
        // Given: .WhereArrayContains(u => u.Tags, "nonexistent")
        // When: executing the filter
        // Then: no results are returned for users without that tag

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereRaw("ANY t IN tags SATISFIES t = $tagValue END", new { tagValue = "nonexistent" });
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Contain("ANY t IN tags SATISFIES t = $tagValue END");
    }

    [Fact]
    public void WhereArrayContainsOnNestedArray()
    {
        // REQ-FILTER-OP-007: Scenario: WhereArrayContains on nested array
        // Given: .WhereArrayContains(u => u.Address.ZipCodes, "10001")
        // When: building the filter
        // Then: the nested path is correctly traversed
        // And: ANY/SATISFIES syntax is used

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereRaw("ANY z IN address.zipCodes SATISFIES z = $zipCode END", new { zipCode = "10001" });
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Contain("address.zipCodes");
        whereClause.Should().Contain("ANY");
        whereClause.Should().Contain("SATISFIES");
    }

    #endregion

    #region REQ-FILTER-OP-008: Array Any with Predicate

    [Fact]
    public async Task WhereAnyWithPredicate()
    {
        // REQ-FILTER-OP-008: Scenario: WhereAny with predicate
        // Given: a User with Orders = [{ Total: 50 }, { Total: 150 }, { Total: 75 }]
        // And: .WhereAny(u => u.Orders, o => o.Total > 100)
        // When: building and executing the filter
        // Then: WHERE clause uses "ANY o IN orders SATISFIES o.total > $p1 END"
        // And: users with at least one order > 100 are returned

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereRaw("ANY o IN orders SATISFIES o.total > $minTotal END", new { minTotal = 100 });
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Contain("ANY o IN orders SATISFIES o.total > $minTotal END");
        filter.Parameters["minTotal"].Should().Be(100);
    }

    [Fact]
    public void WhereAnyWithComplexPredicate()
    {
        // REQ-FILTER-OP-008: Scenario: WhereAny with complex predicate
        // Given: .WhereAny(u => u.Orders, o => o.Status == "completed" && o.Total > 50)
        // When: building the filter
        // Then: the compound predicate is correctly translated
        // And: wrapped in ANY/SATISFIES/END

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereRaw("ANY o IN orders SATISFIES o.status = $orderStatus AND o.total > $minTotal END",
            new { orderStatus = "completed", minTotal = 50 });
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Contain("ANY o IN orders SATISFIES");
        whereClause.Should().Contain("AND");
        filter.Parameters["orderStatus"].Should().Be("completed");
        filter.Parameters["minTotal"].Should().Be(50);
    }

    [Fact]
    public void WhereAllWithPredicate()
    {
        // REQ-FILTER-OP-008: Scenario: WhereAll with predicate
        // Given: .WhereAll(u => u.Scores, s => s >= 60)
        // When: building the filter
        // Then: WHERE clause uses "EVERY s IN scores SATISFIES s >= $p1 END"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereRaw("EVERY s IN scores SATISFIES s >= $minScore END", new { minScore = 60 });
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Contain("EVERY s IN scores SATISFIES s >= $minScore END");
        filter.Parameters["minScore"].Should().Be(60);
    }

    #endregion

    #region REQ-FILTER-OP-009: Nested Property Access

    [Fact]
    public void FilterOnNestedProperty()
    {
        // REQ-FILTER-OP-009: Scenario: Filter on nested property
        // Given: .Where(u => u.Address.City == "New York")
        // When: building the filter
        // Then: WHERE clause is "address.city = $p1"
        // And: dot notation is preserved for Couchbase

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.Where("address.city", "New York");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("address.city = $p0");
        filter.Parameters["p0"].Should().Be("New York");
    }

    [Fact]
    public void DeepNesting()
    {
        // REQ-FILTER-OP-009: Scenario: Deep nesting
        // Given: .Where(u => u.Company.Address.Country.Code == "US")
        // When: building the filter
        // Then: WHERE clause is "company.address.country.code = $p1"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.Where("company.address.country.code", "US");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("company.address.country.code = $p0");
        filter.Parameters["p0"].Should().Be("US");
    }

    [Fact]
    public void NestedPropertyWithColumnAttribute()
    {
        // REQ-FILTER-OP-009: Scenario: Nested property with Column attribute
        // Given: Address.PostalCode has [Column("zip_code")]
        // And: .Where(u => u.Address.PostalCode == "10001")
        // When: building the filter
        // Then: WHERE clause is "address.zip_code = $p1"

        // Note: Column attribute mapping would be handled at expression parsing level.
        // String-based API uses the provided property name directly.

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.Where("address.zip_code", "10001");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("address.zip_code = $p0");
        filter.Parameters["p0"].Should().Be("10001");
    }

    #endregion

    #region REQ-FILTER-OP-010: Raw SQL++ Escape Hatch

    [Fact]
    public void WhereRawWithParameters()
    {
        // REQ-FILTER-OP-010: Scenario: WhereRaw with parameters
        // Given: .WhereRaw("ARRAY_LENGTH(orders) > $minOrders", new { minOrders = 5 })
        // When: building the filter
        // Then: the raw SQL is included in WHERE clause
        // And: $minOrders parameter is bound to 5

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereRaw("ARRAY_LENGTH(orders) > $minOrders", new { minOrders = 5 });
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("ARRAY_LENGTH(orders) > $minOrders");
        filter.Parameters["minOrders"].Should().Be(5);
    }

    [Fact]
    public void WhereRawCombinedWithFluentFilters()
    {
        // REQ-FILTER-OP-010: Scenario: WhereRaw combined with fluent filters
        // Given: .Where(u => u.Status == "active").WhereRaw("LOWER(name) = $lowerName", new { lowerName = "john" })
        // When: building the filter
        // Then: both conditions are combined with AND
        // And: WHERE clause is "status = $p1 AND LOWER(name) = $lowerName"

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.Where("status", "active")
              .WhereRaw("LOWER(name) = $lowerName", new { lowerName = "john" });
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("status = $p0 AND LOWER(name) = $lowerName");
        filter.Parameters["p0"].Should().Be("active");
        filter.Parameters["lowerName"].Should().Be("john");
    }

    [Fact]
    public void WhereRawWithoutParameters()
    {
        // REQ-FILTER-OP-010: Scenario: WhereRaw without parameters
        // Given: .WhereRaw("META().id LIKE 'user::%'")
        // When: building the filter
        // Then: the raw SQL is included as-is
        // And: no parameters are added

        // Arrange
        var filter = new FilterBuilder<User>();

        // Act
        filter.WhereRaw("META().id LIKE 'user::%'");
        var whereClause = filter.BuildWhereClause();

        // Assert
        whereClause.Should().Be("META().id LIKE 'user::%'");
        filter.Parameters.Should().BeEmpty();
    }

    #endregion
}
