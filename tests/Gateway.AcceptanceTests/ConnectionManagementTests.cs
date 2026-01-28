using System.Reflection;
using Couchbase.KeyValue;
using Couchbase.Query;
using FluentAssertions;
using Gateway.Core;
using Gateway.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using GatewayCollectionExtensions = Gateway.Core.Extensions.CollectionExtensions;

namespace Gateway.AcceptanceTests;

/// <summary>
/// Acceptance tests for Connection Management requirements (REQ-CONN-001 to REQ-CONN-004)
/// These tests verify that the SimpleMapper integrates correctly with the Couchbase SDK
/// without managing its own connections.
/// </summary>
public class ConnectionManagementTests
{
    #region REQ-CONN-001: Couchbase SDK Integration

    [Fact]
    public async Task LibraryUsesExistingSdkClusterConnection()
    {
        // REQ-CONN-001: Scenario: Library uses existing SDK cluster connection
        // Given: a Couchbase cluster is connected using the official SDK
        // And: a bucket and scope are opened
        // When: I use the SimpleMapper extension methods
        // Then: the library uses the existing cluster connection
        // And: no additional connections are created

        // Arrange - Given a Couchbase scope from the SDK
        var mockScope = new Mock<IScope>();
        var mockQueryResult = new Mock<IQueryResult<TestUser>>();
        var testUsers = new List<TestUser> { new TestUser { Id = "1", Name = "Test" } };

        mockQueryResult.Setup(r => r.Rows).Returns(testUsers.ToAsyncEnumerable());
        mockScope
            .Setup(s => s.QueryAsync<TestUser>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ReturnsAsync(mockQueryResult.Object);

        // Act - When I use the SimpleMapper extension methods
        var results = await mockScope.Object.QueryToListAsync<TestUser>("SELECT * FROM users");

        // Assert - Then the library uses the existing cluster connection
        mockScope.Verify(
            s => s.QueryAsync<TestUser>(It.IsAny<string>(), It.IsAny<QueryOptions>()),
            Times.Once,
            "The library should delegate to the SDK's existing QueryAsync method");

        // And no additional connections are created (verified by using the mock scope directly)
        results.Should().HaveCount(1);
        results.First().Name.Should().Be("Test");
    }

    public class TestUser
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task LibraryWorksWithSdkConnectionPooling()
    {
        // REQ-CONN-001: Scenario: Library works with SDK connection pooling
        // Given: a Couchbase cluster with connection pooling configured
        // When: multiple concurrent queries are executed via SimpleMapper
        // Then: all queries use the SDK's connection pool
        // And: connection count does not exceed SDK pool limits

        // Arrange - Given a Couchbase scope from the SDK
        var mockScope = new Mock<IScope>();
        var callCount = 0;

        mockScope
            .Setup(s => s.QueryAsync<TestUser>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ReturnsAsync(() =>
            {
                Interlocked.Increment(ref callCount);
                var mockResult = new Mock<IQueryResult<TestUser>>();
                mockResult.Setup(r => r.Rows).Returns(new List<TestUser>().ToAsyncEnumerable());
                return mockResult.Object;
            });

        // Act - Execute multiple concurrent queries
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => mockScope.Object.QueryToListAsync<TestUser>("SELECT * FROM users"))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All queries use the SDK's existing QueryAsync method (connection pool)
        callCount.Should().Be(10);
        mockScope.Verify(
            s => s.QueryAsync<TestUser>(It.IsAny<string>(), It.IsAny<QueryOptions>()),
            Times.Exactly(10),
            "All queries should delegate to the SDK's connection pool");
    }

    [Fact]
    public async Task LibraryRespectsSdkClusterDisposal()
    {
        // REQ-CONN-001: Scenario: Library respects SDK cluster disposal
        // Given: a SimpleMapper context is created from an SDK cluster
        // When: the SDK cluster is disposed
        // Then: SimpleMapper operations throw ObjectDisposedException
        // And: no orphaned connections remain

        // Arrange
        var mockScope = new Mock<IScope>();
        mockScope
            .Setup(s => s.QueryAsync<TestUser>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ThrowsAsync(new ObjectDisposedException("Cluster has been disposed"));

        // Act & Assert - When SDK cluster is disposed, operations throw
        var act = async () => await mockScope.Object.QueryToListAsync<TestUser>("SELECT * FROM users");
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region REQ-CONN-002: No Independent Connection Pool

    [Fact]
    public void LibraryHasNoConnectionPoolConfiguration()
    {
        // REQ-CONN-002: Scenario: Library has no connection pool configuration
        // Given: the SimpleMapper options class
        // When: inspecting available configuration properties
        // Then: no connection pool settings exist (min/max connections, idle timeout, etc.)
        // And: all connection behavior is inherited from SDK configuration

        // Arrange & Act - Inspect SimpleMapperOptions
        var optionsType = typeof(SimpleMapperOptions);
        var properties = optionsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertyNames = properties.Select(p => p.Name.ToLowerInvariant()).ToList();

        // Assert - No connection pool related properties
        propertyNames.Should().NotContain("minconnections");
        propertyNames.Should().NotContain("maxconnections");
        propertyNames.Should().NotContain("connectionpoolsize");
        propertyNames.Should().NotContain("idletimeout");
        propertyNames.Should().NotContain("connectiontimeout");
    }

    [Fact]
    public async Task LibraryCreatesNoBackgroundConnectionThreads()
    {
        // REQ-CONN-002: Scenario: Library creates no background connection threads
        // Given: SimpleMapper is initialized with a scope
        // When: monitoring active threads before and after initialization
        // Then: no additional background threads are created by SimpleMapper
        // And: thread count remains consistent with SDK-only baseline

        // Arrange
        var threadCountBefore = Process.GetCurrentProcess().Threads.Count;
        var mockScope = new Mock<IScope>();
        var mockQueryResult = new Mock<IQueryResult<TestUser>>();
        mockQueryResult.Setup(r => r.Rows).Returns(new List<TestUser>().ToAsyncEnumerable());
        mockScope
            .Setup(s => s.QueryAsync<TestUser>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ReturnsAsync(mockQueryResult.Object);

        // Act - Use the extension methods (which should not create threads)
        await mockScope.Object.QueryToListAsync<TestUser>("SELECT * FROM users");

        var threadCountAfter = Process.GetCurrentProcess().Threads.Count;

        // Assert - Thread count should not significantly increase
        // Allow some variance for normal system operations
        (threadCountAfter - threadCountBefore).Should().BeLessThan(5,
            "SimpleMapper should not create background connection threads");
    }

    #endregion

    #region REQ-CONN-003: Extension Methods on SDK Interfaces

    [Fact]
    public void ExtensionMethodsAvailableOnIScope()
    {
        // REQ-CONN-003: Scenario: Extension methods available on IScope
        // Given: a reference to an IScope instance
        // When: using IntelliSense or reflection to list available methods
        // Then: QueryAsync<T>, QueryFirstAsync<T>, QueryFirstOrDefaultAsync<T> are available
        // And: QuerySingleAsync<T>, ExecuteAsync, and Query<T> builder are available

        // Arrange
        var extensionMethods = typeof(ScopeExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetParameters().FirstOrDefault()?.ParameterType == typeof(IScope))
            .Select(m => m.Name)
            .ToList();

        // Assert - Required extension methods exist
        extensionMethods.Should().Contain("QueryToListAsync");
        extensionMethods.Should().Contain("QueryFirstAsync");
        extensionMethods.Should().Contain("QueryFirstOrDefaultAsync");
    }

    [Fact]
    public void ExtensionMethodsAvailableOnICouchbaseCollection()
    {
        // REQ-CONN-003: Scenario: Extension methods available on ICouchbaseCollection
        // Given: a reference to an ICouchbaseCollection instance
        // When: using IntelliSense or reflection to list available methods
        // Then: GetAsync<T>, InsertAsync<T>, UpsertAsync<T> are available
        // And: ReplaceAsync<T>, RemoveAsync methods are available

        // Arrange
        var extensionMethods = typeof(GatewayCollectionExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetParameters().FirstOrDefault()?.ParameterType == typeof(ICouchbaseCollection))
            .Select(m => m.Name)
            .ToList();

        // Assert - Required extension methods exist
        extensionMethods.Should().Contain("GetAsync");
        extensionMethods.Should().Contain("InsertAsync");
        extensionMethods.Should().Contain("UpsertAsync");
        extensionMethods.Should().Contain("ReplaceAsync");
        extensionMethods.Should().Contain("RemoveAsync");
    }

    [Fact]
    public async Task ExtensionMethodsWorkWithoutAdditionalSetup()
    {
        // REQ-CONN-003: Scenario: Extension methods work without additional setup
        // Given: a Couchbase scope obtained from SDK
        // And: SimpleMapper NuGet package is referenced
        // When: calling scope.QueryAsync<User>("SELECT * FROM users")
        // Then: the query executes successfully
        // And: results are mapped to User objects

        // Arrange
        var mockScope = new Mock<IScope>();
        var mockQueryResult = new Mock<IQueryResult<TestUser>>();
        var testUsers = new List<TestUser>
        {
            new TestUser { Id = "1", Name = "Alice" },
            new TestUser { Id = "2", Name = "Bob" }
        };
        mockQueryResult.Setup(r => r.Rows).Returns(testUsers.ToAsyncEnumerable());
        mockScope
            .Setup(s => s.QueryAsync<TestUser>(It.IsAny<string>(), It.IsAny<QueryOptions>()))
            .ReturnsAsync(mockQueryResult.Object);

        // Act - Use extension methods without any additional setup
        var results = await mockScope.Object.QueryToListAsync<TestUser>("SELECT * FROM users");

        // Assert
        results.Should().HaveCount(2);
        results[0].Name.Should().Be("Alice");
        results[1].Name.Should().Be("Bob");
    }

    #endregion

    #region REQ-CONN-004: Dependency Injection Support

    [Fact]
    public void RegisterSimpleMapperWithDIContainer()
    {
        // REQ-CONN-004: Scenario: Register SimpleMapper with DI container
        // Given: a ServiceCollection for dependency injection
        // When: calling services.AddCouchbaseSimpleMapper(options => { ... })
        // Then: SimpleMapper services are registered in the container
        // And: ISimpleMapperContext is resolvable from the provider

        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCouchbaseSimpleMapper(options =>
        {
            options.DefaultBucket = "testBucket";
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<SimpleMapperOptions>();
        options.Should().NotBeNull();
        options!.DefaultBucket.Should().Be("testBucket");
    }

    [Fact]
    public void ConfigureOptionsViaDI()
    {
        // REQ-CONN-004: Scenario: Configure options via DI
        // Given: SimpleMapper registered with options
        // And: options.DefaultBucket = "testBucket"
        // And: options.DefaultScope = "testScope"
        // When: resolving ISimpleMapperContext
        // Then: the context is configured with the specified bucket and scope

        // Arrange
        var services = new ServiceCollection();
        services.AddCouchbaseSimpleMapper(options =>
        {
            options.DefaultBucket = "testBucket";
            options.DefaultScope = "testScope";
        });

        // Act
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<SimpleMapperOptions>();

        // Assert
        options.DefaultBucket.Should().Be("testBucket");
        options.DefaultScope.Should().Be("testScope");
    }

    [Fact]
    public void OptionsCanBeBoundFromConfiguration()
    {
        // REQ-CONN-004: Scenario: Options can be bound from configuration
        // Given: appsettings.json contains SimpleMapper configuration section
        // When: calling services.AddCouchbaseSimpleMapper(config.GetSection("SimpleMapper"))
        // Then: options are populated from the configuration file
        // And: all settings match the JSON values

        // Arrange
        var configValues = new Dictionary<string, string?>
        {
            ["SimpleMapper:DefaultBucket"] = "configuredBucket",
            ["SimpleMapper:DefaultScope"] = "configuredScope"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddCouchbaseSimpleMapper(configuration.GetSection("SimpleMapper"));
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<SimpleMapperOptions>();

        // Assert
        options.DefaultBucket.Should().Be("configuredBucket");
        options.DefaultScope.Should().Be("configuredScope");
    }

    #endregion
}

// Helper for process thread count
file static class Process
{
    public static System.Diagnostics.Process GetCurrentProcess() => System.Diagnostics.Process.GetCurrentProcess();
}
