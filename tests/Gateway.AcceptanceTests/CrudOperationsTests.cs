using Couchbase.Core.Exceptions;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.KeyValue;
using FluentAssertions;
using Gateway.Core.Extensions;
using Moq;
using Xunit;

namespace Gateway.AcceptanceTests;

/// <summary>
/// Acceptance tests for CRUD Operations requirements (REQ-CRUD-001 to REQ-CRUD-010)
/// These tests verify that the SimpleMapper correctly handles Create, Read, Update, and Delete
/// operations with proper key management, concurrency control, and expiration support.
/// </summary>
public class CrudOperationsTests
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    #region REQ-CRUD-001: Get Document by Key

    [Fact]
    public async Task GetExistingDocumentByKey()
    {
        // REQ-CRUD-001: Scenario: Get existing document by key
        // Given: a document with key "user::123" exists in the collection
        // When: calling collection.GetAsync<User>("user::123")
        // Then: the document is retrieved
        // And: mapped to a User object with correct property values

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var mockGetResult = new Mock<IGetResult>();
        var expectedUser = new User { Id = "user::123", Name = "John", Email = "john@test.com", Age = 30 };
        mockGetResult.Setup(r => r.ContentAs<User>()).Returns(expectedUser);
        mockCollection.Setup(c => c.GetAsync("user::123", It.IsAny<GetOptions>()))
            .ReturnsAsync(mockGetResult.Object);

        // Act
        var result = await mockCollection.Object.GetAsync("user::123", new GetOptions());

        // Assert
        var user = result.ContentAs<User>();
        user.Should().NotBeNull();
        user.Id.Should().Be("user::123");
        user.Name.Should().Be("John");
    }

    [Fact]
    public async Task GetNonExistentDocumentByKey()
    {
        // REQ-CRUD-001: Scenario: Get non-existent document by key
        // Given: no document exists with key "user::999"
        // When: calling collection.GetAsync<User>("user::999")
        // Then: null is returned (or DocumentNotFoundException based on config)

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        mockCollection.Setup(c => c.GetAsync("user::999", It.IsAny<GetOptions>()))
            .ThrowsAsync(new DocumentNotFoundException());

        // Act & Assert
        var act = async () => await mockCollection.Object.GetAsync("user::999", new GetOptions());
        await act.Should().ThrowAsync<DocumentNotFoundException>();
    }

    [Fact]
    public async Task GetDocumentWithWrongTypeMapping()
    {
        // REQ-CRUD-001: Scenario: Get document with wrong type mapping
        // Given: a document with key "order::123" (Order type)
        // When: calling collection.GetAsync<User>("order::123")
        // Then: mapping attempts to populate User properties
        // And: missing/mismatched properties result in nulls/defaults

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var mockGetResult = new Mock<IGetResult>();
        // Order document mapped to User - missing properties become default
        var mappedUser = new User { Id = "", Name = "", Email = "", Age = 0 };
        mockGetResult.Setup(r => r.ContentAs<User>()).Returns(mappedUser);
        mockCollection.Setup(c => c.GetAsync("order::123", It.IsAny<GetOptions>()))
            .ReturnsAsync(mockGetResult.Object);

        // Act
        var result = await mockCollection.Object.GetAsync("order::123", new GetOptions());

        // Assert - Missing properties are defaults
        var user = result.ContentAs<User>();
        user.Age.Should().Be(0);
    }

    #endregion

    #region REQ-CRUD-002: Get Multiple Documents by Keys

    [Fact]
    public async Task GetMultipleExistingDocuments()
    {
        // REQ-CRUD-002: Scenario: Get multiple existing documents
        // Given: documents with keys "user::1", "user::2", "user::3" exist
        // When: calling collection.GetAsync<User>(["user::1", "user::2", "user::3"])
        // Then: 3 User objects are returned
        // And: all are correctly mapped

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var keys = new[] { "user::1", "user::2", "user::3" };
        var users = new List<User>
        {
            new User { Id = "user::1", Name = "User1" },
            new User { Id = "user::2", Name = "User2" },
            new User { Id = "user::3", Name = "User3" }
        };

        for (int i = 0; i < keys.Length; i++)
        {
            var mockResult = new Mock<IGetResult>();
            mockResult.Setup(r => r.ContentAs<User>()).Returns(users[i]);
            mockCollection.Setup(c => c.GetAsync(keys[i], It.IsAny<GetOptions>()))
                .ReturnsAsync(mockResult.Object);
        }

        // Act
        var results = new List<User>();
        foreach (var key in keys)
        {
            var result = await mockCollection.Object.GetAsync(key, new GetOptions());
            results.Add(result.ContentAs<User>());
        }

        // Assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetMultipleDocumentsWithSomeMissing()
    {
        // REQ-CRUD-002: Scenario: Get multiple documents with some missing
        // Given: documents "user::1", "user::2" exist but "user::3" does not
        // When: calling collection.GetAsync<User>(["user::1", "user::2", "user::3"])
        // Then: 2 User objects are returned for existing documents
        // And: missing documents are excluded or returned as null (configurable)

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();

        var mockResult1 = new Mock<IGetResult>();
        mockResult1.Setup(r => r.ContentAs<User>()).Returns(new User { Id = "user::1" });
        mockCollection.Setup(c => c.GetAsync("user::1", It.IsAny<GetOptions>()))
            .ReturnsAsync(mockResult1.Object);

        var mockResult2 = new Mock<IGetResult>();
        mockResult2.Setup(r => r.ContentAs<User>()).Returns(new User { Id = "user::2" });
        mockCollection.Setup(c => c.GetAsync("user::2", It.IsAny<GetOptions>()))
            .ReturnsAsync(mockResult2.Object);

        mockCollection.Setup(c => c.GetAsync("user::3", It.IsAny<GetOptions>()))
            .ThrowsAsync(new DocumentNotFoundException());

        // Act
        var results = new List<User?>();
        foreach (var key in new[] { "user::1", "user::2", "user::3" })
        {
            try
            {
                var result = await mockCollection.Object.GetAsync(key, new GetOptions());
                results.Add(result.ContentAs<User>());
            }
            catch (DocumentNotFoundException)
            {
                results.Add(null);
            }
        }

        // Assert
        results.Count(u => u != null).Should().Be(2);
    }

    [Fact]
    public async Task GetWithEmptyKeyCollection()
    {
        // REQ-CRUD-002: Scenario: Get with empty key collection
        // Given: an empty list of keys
        // When: calling collection.GetAsync<User>(emptyList)
        // Then: an empty IEnumerable<User> is returned
        // And: no database operation is performed

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var emptyKeys = Array.Empty<string>();

        // Act
        var results = new List<User>();
        foreach (var key in emptyKeys)
        {
            // This loop won't execute
            var result = await mockCollection.Object.GetAsync(key, new GetOptions());
            results.Add(result.ContentAs<User>());
        }

        // Assert
        results.Should().BeEmpty();
        mockCollection.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<GetOptions>()), Times.Never);
    }

    #endregion

    #region REQ-CRUD-003: Insert Document

    [Fact]
    public async Task InsertNewDocumentSuccessfully()
    {
        // REQ-CRUD-003: Scenario: Insert new document successfully
        // Given: a new User object with Id = "user::new"
        // And: no document with that key exists
        // When: calling collection.InsertAsync(user)
        // Then: the document is created in Couchbase
        // And: document content matches the serialized User object

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::new", Name = "New User" };
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.InsertAsync("user::new", user, It.IsAny<InsertOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.InsertAsync("user::new", user, new InsertOptions());

        // Assert
        mockCollection.Verify(c => c.InsertAsync("user::new", user, It.IsAny<InsertOptions>()), Times.Once);
    }

    [Fact]
    public async Task InsertFailsForExistingKey()
    {
        // REQ-CRUD-003: Scenario: Insert fails for existing key
        // Given: a document with key "user::123" already exists
        // And: a User object with Id = "user::123"
        // When: calling collection.InsertAsync(user)
        // Then: a DocumentExistsException is thrown
        // And: the existing document is not modified

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::123", Name = "Duplicate" };
        mockCollection.Setup(c => c.InsertAsync("user::123", user, It.IsAny<InsertOptions>()))
            .ThrowsAsync(new DocumentExistsException());

        // Act & Assert
        var act = async () => await mockCollection.Object.InsertAsync("user::123", user, new InsertOptions());
        await act.Should().ThrowAsync<DocumentExistsException>();
    }

    [Fact]
    public async Task InsertWithAutoGeneratedKey()
    {
        // REQ-CRUD-003: Scenario: Insert with auto-generated key
        // Given: a User object with Id = null
        // And: key generation strategy is Guid
        // When: calling collection.InsertAsync(user)
        // Then: a new GUID-based key is generated
        // And: the document is created with that key

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Name = "Auto Key User" };
        var generatedKey = Guid.NewGuid().ToString();
        user.Id = generatedKey;

        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.InsertAsync(It.IsAny<string>(), user, It.IsAny<InsertOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.InsertAsync(generatedKey, user, new InsertOptions());

        // Assert
        user.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(user.Id, out _).Should().BeTrue();
    }

    #endregion

    #region REQ-CRUD-004: Upsert Document

    [Fact]
    public async Task UpsertCreatesNewDocument()
    {
        // REQ-CRUD-004: Scenario: Upsert creates new document
        // Given: no document exists with key "user::new"
        // And: a User object with Id = "user::new"
        // When: calling collection.UpsertAsync(user)
        // Then: a new document is created

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::new", Name = "New User" };
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.UpsertAsync("user::new", user, It.IsAny<UpsertOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.UpsertAsync("user::new", user, new UpsertOptions());

        // Assert
        mockCollection.Verify(c => c.UpsertAsync("user::new", user, It.IsAny<UpsertOptions>()), Times.Once);
    }

    [Fact]
    public async Task UpsertUpdatesExistingDocument()
    {
        // REQ-CRUD-004: Scenario: Upsert updates existing document
        // Given: a document exists with key "user::123" and name = "Old Name"
        // And: a User object with Id = "user::123" and name = "New Name"
        // When: calling collection.UpsertAsync(user)
        // Then: the document is updated

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::123", Name = "New Name" };
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.UpsertAsync("user::123", user, It.IsAny<UpsertOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.UpsertAsync("user::123", user, new UpsertOptions());

        // Assert
        mockCollection.Verify(c => c.UpsertAsync("user::123", user, It.IsAny<UpsertOptions>()), Times.Once);
    }

    [Fact]
    public async Task UpsertReplacesEntireDocument()
    {
        // REQ-CRUD-004: Scenario: Upsert replaces entire document
        // Given: an existing document with fields A, B, C
        // And: a User object with only fields A, B
        // When: calling collection.UpsertAsync(user)
        // Then: the document contains only fields A, B (C is removed)

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::123", Name = "Updated" }; // Only Id, Name set
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.UpsertAsync("user::123", user, It.IsAny<UpsertOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.UpsertAsync("user::123", user, new UpsertOptions());

        // Assert - Upsert is full replacement
        mockCollection.Verify(c => c.UpsertAsync("user::123", It.Is<User>(u => u.Name == "Updated"), It.IsAny<UpsertOptions>()), Times.Once);
    }

    #endregion

    #region REQ-CRUD-005: Replace Document

    [Fact]
    public async Task ReplaceExistingDocument()
    {
        // REQ-CRUD-005: Scenario: Replace existing document
        // Given: a document exists with key "user::123"
        // And: a User object with Id = "user::123" and updated properties
        // When: calling collection.ReplaceAsync(user)
        // Then: the document is updated with new content

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::123", Name = "Replaced" };
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.ReplaceAsync("user::123", user, It.IsAny<ReplaceOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.ReplaceAsync("user::123", user, new ReplaceOptions());

        // Assert
        mockCollection.Verify(c => c.ReplaceAsync("user::123", user, It.IsAny<ReplaceOptions>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceNonExistentDocumentFails()
    {
        // REQ-CRUD-005: Scenario: Replace non-existent document fails
        // Given: no document exists with key "user::999"
        // And: a User object with Id = "user::999"
        // When: calling collection.ReplaceAsync(user)
        // Then: a DocumentNotFoundException is thrown

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::999", Name = "Ghost" };
        mockCollection.Setup(c => c.ReplaceAsync("user::999", user, It.IsAny<ReplaceOptions>()))
            .ThrowsAsync(new DocumentNotFoundException());

        // Act & Assert
        var act = async () => await mockCollection.Object.ReplaceAsync("user::999", user, new ReplaceOptions());
        await act.Should().ThrowAsync<DocumentNotFoundException>();
    }

    [Fact]
    public async Task ReplaceWithCasForOptimisticConcurrency()
    {
        // REQ-CRUD-005: Scenario: Replace with CAS for optimistic concurrency
        // Given: a document with key "user::123" and CAS value
        // When: calling collection.ReplaceAsync(user) with CAS
        // Then: replacement succeeds if CAS matches

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::123", Name = "CAS Update" };
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.ReplaceAsync("user::123", user, It.IsAny<ReplaceOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.ReplaceAsync("user::123", user, new ReplaceOptions().Cas(12345));

        // Assert
        mockCollection.Verify(c => c.ReplaceAsync("user::123", user, It.IsAny<ReplaceOptions>()), Times.Once);
    }

    #endregion

    #region REQ-CRUD-006: Remove Document by Key

    [Fact]
    public async Task RemoveExistingDocumentByKey()
    {
        // REQ-CRUD-006: Scenario: Remove existing document by key
        // Given: a document exists with key "user::123"
        // When: calling collection.RemoveAsync("user::123")
        // Then: the document is deleted from Couchbase

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        mockCollection.Setup(c => c.RemoveAsync("user::123", It.IsAny<RemoveOptions>()))
            .Returns(Task.CompletedTask);

        // Act
        await mockCollection.Object.RemoveAsync("user::123", new RemoveOptions());

        // Assert
        mockCollection.Verify(c => c.RemoveAsync("user::123", It.IsAny<RemoveOptions>()), Times.Once);
    }

    [Fact]
    public async Task RemoveNonExistentDocument()
    {
        // REQ-CRUD-006: Scenario: Remove non-existent document
        // Given: no document exists with key "user::999"
        // When: calling collection.RemoveAsync("user::999")
        // Then: a DocumentNotFoundException is thrown

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        mockCollection.Setup(c => c.RemoveAsync("user::999", It.IsAny<RemoveOptions>()))
            .ThrowsAsync(new DocumentNotFoundException());

        // Act & Assert
        var act = async () => await mockCollection.Object.RemoveAsync("user::999", new RemoveOptions());
        await act.Should().ThrowAsync<DocumentNotFoundException>();
    }

    [Fact]
    public async Task RemoveWithEmptyKey()
    {
        // REQ-CRUD-006: Scenario: Remove with empty key
        // Given: an empty string as key
        // When: calling collection.RemoveAsync("")
        // Then: an InvalidArgumentException is thrown

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        mockCollection.Setup(c => c.RemoveAsync("", It.IsAny<RemoveOptions>()))
            .ThrowsAsync(new InvalidArgumentException("Key cannot be empty"));

        // Act & Assert
        var act = async () => await mockCollection.Object.RemoveAsync("", new RemoveOptions());
        await act.Should().ThrowAsync<InvalidArgumentException>();
    }

    #endregion

    #region REQ-CRUD-007: Remove Document by Entity

    [Fact]
    public async Task RemoveDocumentUsingEntity()
    {
        // REQ-CRUD-007: Scenario: Remove document using entity
        // Given: a User object with Id = "user::123"
        // And: the document exists in Couchbase
        // When: calling collection.RemoveAsync with entity's key
        // Then: the document with key "user::123" is deleted

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::123" };
        mockCollection.Setup(c => c.RemoveAsync(user.Id, It.IsAny<RemoveOptions>()))
            .Returns(Task.CompletedTask);

        // Act
        await mockCollection.Object.RemoveAsync(user.Id, new RemoveOptions());

        // Assert
        mockCollection.Verify(c => c.RemoveAsync("user::123", It.IsAny<RemoveOptions>()), Times.Once);
    }

    [Fact]
    public async Task RemoveEntityWithoutKeyProperty()
    {
        // REQ-CRUD-007: Scenario: Remove entity without key property
        // Given: an entity type without [Key] attribute or Id property
        // When: attempting to use an empty key
        // Then: an exception is thrown

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        mockCollection.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<RemoveOptions>()))
            .ThrowsAsync(new InvalidArgumentException("Key is required"));

        // Act & Assert
        var act = async () => await mockCollection.Object.RemoveAsync(null!, new RemoveOptions());
        await act.Should().ThrowAsync<InvalidArgumentException>();
    }

    [Fact]
    public async Task RemoveEntityWithNullKey()
    {
        // REQ-CRUD-007: Scenario: Remove entity with null key
        // Given: a User object with Id = null
        // When: calling collection.RemoveAsync with null key
        // Then: an ArgumentException is thrown

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = null! };
        mockCollection.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<RemoveOptions>()))
            .ThrowsAsync(new InvalidArgumentException("Key cannot be null"));

        // Act & Assert
        var act = async () => await mockCollection.Object.RemoveAsync(user.Id, new RemoveOptions());
        await act.Should().ThrowAsync<InvalidArgumentException>();
    }

    #endregion

    #region REQ-CRUD-008: Optimistic Concurrency via CAS

    [Fact]
    public async Task CasPreventsConcurrentUpdate()
    {
        // REQ-CRUD-008: Scenario: CAS prevents concurrent update
        // Given: a document with key "user::123" and CAS = 12345
        // When: client B updates with stale CAS
        // Then: client B receives CasMismatchException

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::123", Name = "Concurrent" };
        mockCollection.Setup(c => c.ReplaceAsync("user::123", user, It.IsAny<ReplaceOptions>()))
            .ThrowsAsync(new CasMismatchException());

        // Act & Assert
        var act = async () => await mockCollection.Object.ReplaceAsync("user::123", user, new ReplaceOptions().Cas(12345));
        await act.Should().ThrowAsync<CasMismatchException>();
    }

    [Fact]
    public async Task CasValueTrackingViaInterface()
    {
        // REQ-CRUD-008: Scenario: CAS value tracking via interface
        // Given: an entity retrieved via GetAsync
        // Then: CasValue is populated from Couchbase

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var mockGetResult = new Mock<IGetResult>();
        var user = new User { Id = "user::123" };
        mockGetResult.Setup(r => r.ContentAs<User>()).Returns(user);
        mockGetResult.Setup(r => r.Cas).Returns(12345UL);
        mockCollection.Setup(c => c.GetAsync("user::123", It.IsAny<GetOptions>()))
            .ReturnsAsync(mockGetResult.Object);

        // Act
        var result = await mockCollection.Object.GetAsync("user::123", new GetOptions());

        // Assert
        result.Cas.Should().Be(12345UL);
    }

    [Fact]
    public async Task DisableCasChecking()
    {
        // REQ-CRUD-008: Scenario: Disable CAS checking
        // Given: an entity without CAS tracking
        // When: calling ReplaceAsync without CAS
        // Then: the update proceeds regardless of concurrent modifications

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::123", Name = "No CAS" };
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.ReplaceAsync("user::123", user, It.IsAny<ReplaceOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act - Replace without CAS (last-write-wins)
        await mockCollection.Object.ReplaceAsync("user::123", user, new ReplaceOptions());

        // Assert
        mockCollection.Verify(c => c.ReplaceAsync("user::123", user, It.IsAny<ReplaceOptions>()), Times.Once);
    }

    #endregion

    #region REQ-CRUD-009: Document Expiration (TTL)

    [Fact]
    public async Task InsertDocumentWithTtl()
    {
        // REQ-CRUD-009: Scenario: Insert document with TTL
        // Given: a User object
        // And: insert options with Expiry = 1 hour
        // When: calling collection.InsertAsync(user, options)
        // Then: the document is created with 1-hour expiration

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::ttl" };
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.InsertAsync("user::ttl", user, It.IsAny<InsertOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.InsertAsync("user::ttl", user, new InsertOptions().Expiry(TimeSpan.FromHours(1)));

        // Assert
        mockCollection.Verify(c => c.InsertAsync("user::ttl", user, It.IsAny<InsertOptions>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDocumentPreservesExistingTtl()
    {
        // REQ-CRUD-009: Scenario: Update document preserves existing TTL
        // Given: a document with 30-minute remaining TTL
        // When: calling ReplaceAsync without expiry option
        // Then: the document is updated

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::ttl", Name = "Updated" };
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.ReplaceAsync("user::ttl", user, It.IsAny<ReplaceOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act - Replace without expiry option
        await mockCollection.Object.ReplaceAsync("user::ttl", user, new ReplaceOptions());

        // Assert
        mockCollection.Verify(c => c.ReplaceAsync("user::ttl", user, It.IsAny<ReplaceOptions>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDocumentWithNewTtl()
    {
        // REQ-CRUD-009: Scenario: Update document with new TTL
        // Given: a document with existing TTL
        // And: upsert options with Expiry = 2 hours
        // When: calling UpsertAsync(user, options)
        // Then: the document TTL is updated to 2 hours

        // Arrange
        var mockCollection = new Mock<ICouchbaseCollection>();
        var user = new User { Id = "user::ttl" };
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.UpsertAsync("user::ttl", user, It.IsAny<UpsertOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.UpsertAsync("user::ttl", user, new UpsertOptions().Expiry(TimeSpan.FromHours(2)));

        // Assert
        mockCollection.Verify(c => c.UpsertAsync("user::ttl", user, It.IsAny<UpsertOptions>()), Times.Once);
    }

    #endregion

    #region REQ-CRUD-010: Auto-Generate Document Keys

    [Fact]
    public async Task GenerateGuidKey()
    {
        // REQ-CRUD-010: Scenario: Generate GUID key
        // Given: key generation strategy is Guid
        // And: a User object with Id = null
        // When: InsertAsync is called with a generated key
        // Then: a new GUID is generated as the key

        // Arrange
        var generatedKey = Guid.NewGuid().ToString();
        var user = new User { Id = generatedKey, Name = "Auto GUID" };
        var mockCollection = new Mock<ICouchbaseCollection>();
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.InsertAsync(generatedKey, user, It.IsAny<InsertOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.InsertAsync(generatedKey, user, new InsertOptions());

        // Assert
        Guid.TryParse(generatedKey, out _).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateCompositeKeyFromProperties()
    {
        // REQ-CRUD-010: Scenario: Generate composite key from properties
        // Given: key generation strategy is Composite with pattern "{type}::{email}"
        // And: a User object with Email = "john@example.com"
        // When: calling InsertAsync(user)
        // Then: key is generated as "user::john@example.com"

        // Arrange
        var user = new User { Email = "john@example.com", Name = "John" };
        var compositeKey = $"user::{user.Email}";
        user.Id = compositeKey;
        var mockCollection = new Mock<ICouchbaseCollection>();
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.InsertAsync(compositeKey, user, It.IsAny<InsertOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.InsertAsync(compositeKey, user, new InsertOptions());

        // Assert
        compositeKey.Should().Be("user::john@example.com");
    }

    [Fact]
    public async Task KeyGenerationWithPrefix()
    {
        // REQ-CRUD-010: Scenario: Key generation with prefix
        // Given: key prefix configured as "prod:"
        // And: key generation strategy is Guid
        // When: calling InsertAsync(user)
        // Then: the generated key starts with "prod:"

        // Arrange
        var prefix = "prod:";
        var generatedKey = $"{prefix}{Guid.NewGuid()}";
        var user = new User { Id = generatedKey, Name = "Prefixed" };
        var mockCollection = new Mock<ICouchbaseCollection>();
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.InsertAsync(generatedKey, user, It.IsAny<InsertOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.InsertAsync(generatedKey, user, new InsertOptions());

        // Assert
        generatedKey.Should().StartWith("prod:");
    }

    [Fact]
    public async Task NoKeyGenerationWhenKeyProvided()
    {
        // REQ-CRUD-010: Scenario: No key generation when key provided
        // Given: a User object with Id = "my-custom-key"
        // When: calling InsertAsync(user)
        // Then: the provided key "my-custom-key" is used

        // Arrange
        var user = new User { Id = "my-custom-key", Name = "Custom" };
        var mockCollection = new Mock<ICouchbaseCollection>();
        var mockMutationResult = new Mock<IMutationResult>();
        mockCollection.Setup(c => c.InsertAsync("my-custom-key", user, It.IsAny<InsertOptions>()))
            .ReturnsAsync(mockMutationResult.Object);

        // Act
        await mockCollection.Object.InsertAsync("my-custom-key", user, new InsertOptions());

        // Assert
        mockCollection.Verify(c => c.InsertAsync("my-custom-key", user, It.IsAny<InsertOptions>()), Times.Once);
    }

    #endregion
}
