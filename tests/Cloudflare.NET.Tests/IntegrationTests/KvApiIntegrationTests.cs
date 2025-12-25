namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
using System.Text.Json;
using Accounts;
using Accounts.Kv;
using Accounts.Kv.Models;
using Cloudflare.NET.Core.Exceptions;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the Workers KV operations of <see cref="IKvApi" />. These tests interact with the
///   live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   This test class covers all Workers KV operations including:
///   <list type="bullet">
///     <item><description>Namespace CRUD operations (List, Create, Get, Rename, Delete)</description></item>
///     <item><description>Key operations (List keys with filters)</description></item>
///     <item><description>Value operations (Read, Write, Delete)</description></item>
///     <item><description>Bulk operations (BulkWrite, BulkDelete, BulkGet)</description></item>
///     <item><description>Metadata and expiration handling</description></item>
///   </list>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class KvApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>, IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IKvApi _sut;

  /// <summary>A unique title for the KV namespace used in this test run, to avoid collisions.</summary>
  private readonly string _namespaceTitle = $"cfnet-test-kv-{Guid.NewGuid():N}";

  /// <summary>The ID of the created namespace, populated during InitializeAsync.</summary>
  private string _namespaceId = string.Empty;

  /// <summary>The xUnit test output helper for writing warnings.</summary>
  private readonly ITestOutputHelper _output;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="KvApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public KvApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    // The SUT is resolved via the fixture's pre-configured DI container.
    _sut    = fixture.AccountsApi.Kv;
    _output = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Methods Impl

  /// <summary>Asynchronously creates the KV namespace required for the tests. This runs once before any tests in this class.</summary>
  public async Task InitializeAsync()
  {
    // Create a new KV namespace for the test run.
    var ns = await _sut.CreateAsync(_namespaceTitle);
    _namespaceId = ns.Id;

    _output.WriteLine($"Created test namespace: {_namespaceId} ({_namespaceTitle})");
  }

  /// <summary>Asynchronously deletes the KV namespace after all tests in this class have run.</summary>
  public async Task DisposeAsync()
  {
    // Clean up the KV namespace.
    if (!string.IsNullOrEmpty(_namespaceId))
    {
      try
      {
        await _sut.DeleteAsync(_namespaceId);
        _output.WriteLine($"Deleted test namespace: {_namespaceId}");
      }
      catch (Exception ex)
      {
        _output.WriteLine($"Failed to delete test namespace: {ex.Message}");
      }
    }
  }

  #endregion


  #region Namespace Operations

  /// <summary>Verifies that KV namespaces can be listed successfully.</summary>
  [IntegrationTest]
  public async Task ListAsync_CanListSuccessfully()
  {
    // Arrange (namespace is created in InitializeAsync)

    // Act
    var result = await _sut.ListAsync();

    // Assert
    result.Items.Should().NotBeEmpty("at least one namespace should exist");
    result.Items.Should().Contain(ns => ns.Id == _namespaceId, "the test namespace should be in the list");
  }

  /// <summary>Verifies that ListAllAsync can iterate through all namespaces.</summary>
  [IntegrationTest]
  public async Task ListAllAsync_CanIterateThroughAllNamespaces()
  {
    // Arrange - Create a second namespace to ensure multiple exist
    var secondNamespaceTitle = $"cfnet-test-kv-{Guid.NewGuid():N}";
    var secondNs = await _sut.CreateAsync(secondNamespaceTitle);

    try
    {
      // Act
      var allNamespaces = new List<KvNamespace>();
      await foreach (var ns in _sut.ListAllAsync())
        allNamespaces.Add(ns);

      // Assert
      allNamespaces.Should().NotBeEmpty();
      allNamespaces.Should().Contain(ns => ns.Id == _namespaceId, "the primary test namespace should be found");
      allNamespaces.Should().Contain(ns => ns.Id == secondNs.Id, "the second test namespace should be found");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteAsync(secondNs.Id);
    }
  }

  /// <summary>Verifies that pagination works correctly with small page size.</summary>
  [IntegrationTest]
  public async Task ListAllAsync_ShouldPaginateBeyondFirstPage()
  {
    // Arrange - Create multiple namespaces to force pagination
    var testPrefix = $"cfnet-page-{Guid.NewGuid():N}";
    var ns1 = await _sut.CreateAsync($"{testPrefix}-1");
    var ns2 = await _sut.CreateAsync($"{testPrefix}-2");
    var ns3 = await _sut.CreateAsync($"{testPrefix}-3");

    var createdIds = new[] { ns1.Id, ns2.Id, ns3.Id };

    try
    {
      // Act - Use PerPage=2 to force multiple pages for 3+ namespaces
      var allNamespaces = new List<KvNamespace>();
      var filters = new ListKvNamespacesFilters(PerPage: 2);

      await foreach (var ns in _sut.ListAllAsync(filters))
        allNamespaces.Add(ns);

      // Assert - Must find all 3 test namespaces across pages
      var testNamespaces = allNamespaces.Where(ns => createdIds.Contains(ns.Id)).ToList();
      testNamespaces.Should().HaveCount(3, "pagination should retrieve all namespaces across multiple pages");
    }
    finally
    {
      // Cleanup
      foreach (var id in createdIds)
      {
        try { await _sut.DeleteAsync(id); }
        catch (HttpRequestException) { /* Ignore cleanup errors */ }
      }
    }
  }

  /// <summary>Verifies that GetAsync retrieves namespace properties.</summary>
  [IntegrationTest]
  public async Task GetAsync_ReturnsNamespaceProperties()
  {
    // Arrange (namespace is created in InitializeAsync)

    // Act
    var result = await _sut.GetAsync(_namespaceId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(_namespaceId);
    result.Title.Should().Be(_namespaceTitle);
    result.SupportsUrlEncoding.Should().BeTrue("new namespaces support URL encoding by default");
  }

  /// <summary>Verifies that RenameAsync can rename a namespace.</summary>
  [IntegrationTest]
  public async Task RenameAsync_CanRenameNamespace()
  {
    // Arrange - Create a temporary namespace to rename
    var originalTitle = $"cfnet-rename-test-{Guid.NewGuid():N}";
    var newTitle = $"cfnet-renamed-{Guid.NewGuid():N}";
    var ns = await _sut.CreateAsync(originalTitle);

    try
    {
      // Act
      var result = await _sut.RenameAsync(ns.Id, newTitle);

      // Assert
      result.Should().NotBeNull();
      result.Id.Should().Be(ns.Id);
      result.Title.Should().Be(newTitle);

      // Verify by getting the namespace again
      var verify = await _sut.GetAsync(ns.Id);
      verify.Title.Should().Be(newTitle);
    }
    finally
    {
      // Cleanup
      await _sut.DeleteAsync(ns.Id);
    }
  }

  /// <summary>Verifies that creating a namespace with a duplicate title fails with the expected error.</summary>
  /// <remarks>
  ///   The SDK throws <see cref="HttpRequestException"/> for HTTP 4xx errors (like 400 Bad Request),
  ///   while <see cref="CloudflareApiException"/> is only thrown when HTTP status is 200 OK but
  ///   the response body contains <c>success: false</c>.
  /// </remarks>
  [IntegrationTest]
  public async Task CreateAsync_DuplicateTitle_ThrowsHttpRequestException()
  {
    // Arrange (namespace with _namespaceTitle is created in InitializeAsync)

    // Act
    var action = async () => await _sut.CreateAsync(_namespaceTitle);

    // Assert - The SDK throws HttpRequestException for 400 Bad Request responses
    var exception = await action.Should().ThrowAsync<HttpRequestException>(
      "creating a namespace with a duplicate title should fail");
    exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    exception.Which.Message.Should().Contain("10014",
      "the error should contain the duplicate namespace error code");
  }

  /// <summary>Verifies that a namespace can be created and deleted successfully.</summary>
  [IntegrationTest]
  public async Task CanCreateAndDeleteNamespace()
  {
    // Arrange
    var title = $"cfnet-standalone-{Guid.NewGuid():N}";

    // Act
    var createResult = await _sut.CreateAsync(title);

    // Assert
    createResult.Should().NotBeNull();
    createResult.Id.Should().NotBeNullOrEmpty();
    createResult.Title.Should().Be(title);

    // Cleanup & verify deletion works
    var deleteAction = async () => await _sut.DeleteAsync(createResult.Id);
    await deleteAction.Should().NotThrowAsync("deletion should succeed");
  }

  #endregion


  #region Value Operations

  /// <summary>Verifies that a string value can be written and read back.</summary>
  [IntegrationTest]
  public async Task CanWriteAndReadStringValue()
  {
    // Arrange
    var key = $"test-key-{Guid.NewGuid():N}";
    var value = "Hello, Workers KV!";

    // Act - Write the value
    await _sut.WriteValueAsync(_namespaceId, key, value);

    // Act - Read the value back
    var result = await _sut.GetValueAsync(_namespaceId, key);

    // Assert
    result.Should().Be(value);

    // Cleanup
    await _sut.DeleteValueAsync(_namespaceId, key);
  }

  /// <summary>Verifies that a binary value can be written and read back.</summary>
  [IntegrationTest]
  public async Task CanWriteAndReadBinaryValue()
  {
    // Arrange
    var key = $"test-binary-{Guid.NewGuid():N}";
    var value = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE, 0xFD };

    // Act - Write the value
    await _sut.WriteValueAsync(_namespaceId, key, value);

    // Act - Read the value back
    var result = await _sut.GetValueBytesAsync(_namespaceId, key);

    // Assert
    result.Should().BeEquivalentTo(value);

    // Cleanup
    await _sut.DeleteValueAsync(_namespaceId, key);
  }

  /// <summary>Verifies that a value can be written with metadata and retrieved.</summary>
  [IntegrationTest]
  public async Task CanWriteValueWithMetadata()
  {
    // Arrange
    var key = $"test-metadata-{Guid.NewGuid():N}";
    var value = "value with metadata";
    var metadata = JsonSerializer.SerializeToElement(new { category = "test", version = 1 });
    var options = new KvWriteOptions(Metadata: metadata);

    // Act - Write the value with metadata
    await _sut.WriteValueAsync(_namespaceId, key, value, options);

    // Act - Read the metadata
    var metadataResult = await _sut.GetMetadataAsync(_namespaceId, key);

    // Assert
    metadataResult.Should().NotBeNull();
    metadataResult!.Value.GetProperty("category").GetString().Should().Be("test");
    metadataResult.Value.GetProperty("version").GetInt32().Should().Be(1);

    // Verify the value is also correct
    var valueResult = await _sut.GetValueAsync(_namespaceId, key);
    valueResult.Should().Be(value);

    // Cleanup
    await _sut.DeleteValueAsync(_namespaceId, key);
  }

  /// <summary>Verifies that a value can be written with an expiration TTL.</summary>
  [IntegrationTest]
  public async Task CanWriteValueWithExpirationTtl()
  {
    // Arrange
    var key = $"test-expiration-{Guid.NewGuid():N}";
    var value = "expiring value";
    // Use minimum TTL of 60 seconds
    var options = new KvWriteOptions(ExpirationTtl: 60);

    // Act - Write the value with TTL
    await _sut.WriteValueAsync(_namespaceId, key, value, options);

    // Act - Read the value with expiration
    var result = await _sut.GetValueWithExpirationAsync(_namespaceId, key);

    // Assert
    result.Should().NotBeNull();
    result!.Value.Should().Be(value);
    // Note: The expiration header may not always be present immediately, depending on API behavior.
    // If present, it should be in the future.
    if (result.Expiration.HasValue)
    {
      var expirationTime = DateTimeOffset.FromUnixTimeSeconds(result.Expiration.Value);
      expirationTime.Should().BeAfter(DateTimeOffset.UtcNow, "expiration should be in the future");
    }

    // Cleanup
    await _sut.DeleteValueAsync(_namespaceId, key);
  }

  /// <summary>Verifies that GetValueAsync returns null for non-existent keys.</summary>
  [IntegrationTest]
  public async Task GetValueAsync_NonExistentKey_ReturnsNull()
  {
    // Arrange
    var key = $"non-existent-{Guid.NewGuid():N}";

    // Act
    var result = await _sut.GetValueAsync(_namespaceId, key);

    // Assert
    result.Should().BeNull("non-existent keys should return null");
  }

  /// <summary>Verifies that a value can be deleted.</summary>
  [IntegrationTest]
  public async Task DeleteValueAsync_CanDeleteExistingKey()
  {
    // Arrange
    var key = $"test-delete-{Guid.NewGuid():N}";
    var value = "value to delete";
    await _sut.WriteValueAsync(_namespaceId, key, value);

    // Verify it exists first
    var beforeDelete = await _sut.GetValueAsync(_namespaceId, key);
    beforeDelete.Should().Be(value);

    // Act
    await _sut.DeleteValueAsync(_namespaceId, key);

    // Assert - Value should no longer exist (retry for eventual consistency)
    const int maxRetries = 10;
    string? afterDelete = value;
    for (var attempt = 1; attempt <= maxRetries && afterDelete is not null; attempt++)
      afterDelete = await _sut.GetValueAsync(_namespaceId, key);

    afterDelete.Should().BeNull($"the key should be deleted after {maxRetries} retries");
  }

  /// <summary>Verifies that deleting a non-existent key does not throw an error.</summary>
  [IntegrationTest]
  public async Task DeleteValueAsync_NonExistentKey_DoesNotThrow()
  {
    // Arrange
    var key = $"non-existent-delete-{Guid.NewGuid():N}";

    // Act
    var action = async () => await _sut.DeleteValueAsync(_namespaceId, key);

    // Assert - Should not throw (DELETE is idempotent)
    await action.Should().NotThrowAsync("deleting a non-existent key should be idempotent");
  }

  /// <summary>Verifies that keys with special characters are handled correctly.</summary>
  [IntegrationTest]
  public async Task CanWriteAndReadKeyWithSpecialCharacters()
  {
    // Arrange - Key with special characters that need URL encoding
    var key = $"path/to/key with spaces+and+plus/value-{Guid.NewGuid():N}";
    var value = "special character key value";

    // Act - Write the value
    await _sut.WriteValueAsync(_namespaceId, key, value);

    // Act - Read the value back
    var result = await _sut.GetValueAsync(_namespaceId, key);

    // Assert
    result.Should().Be(value);

    // Cleanup
    await _sut.DeleteValueAsync(_namespaceId, key);
  }

  #endregion


  #region Key Operations

  /// <summary>Verifies that keys can be listed with a prefix filter.</summary>
  [IntegrationTest]
  public async Task ListKeysAsync_WithPrefix_FiltersCorrectly()
  {
    // Arrange - Create keys with different prefixes
    var prefix = $"prefix-{Guid.NewGuid():N}/";
    var key1 = $"{prefix}key1";
    var key2 = $"{prefix}key2";
    var otherKey = $"other-{Guid.NewGuid():N}/key3";

    await _sut.WriteValueAsync(_namespaceId, key1, "value1");
    await _sut.WriteValueAsync(_namespaceId, key2, "value2");
    await _sut.WriteValueAsync(_namespaceId, otherKey, "value3");

    try
    {
      // Act
      var result = await _sut.ListKeysAsync(_namespaceId, new ListKvKeysFilters(Prefix: prefix));

      // Assert
      result.Items.Should().HaveCount(2, "only keys with the prefix should be returned");
      result.Items.Select(k => k.Name).Should().Contain(key1);
      result.Items.Select(k => k.Name).Should().Contain(key2);
      result.Items.Select(k => k.Name).Should().NotContain(otherKey);
    }
    finally
    {
      // Cleanup
      await _sut.DeleteValueAsync(_namespaceId, key1);
      await _sut.DeleteValueAsync(_namespaceId, key2);
      await _sut.DeleteValueAsync(_namespaceId, otherKey);
    }
  }

  /// <summary>Verifies that ListAllKeysAsync handles cursor pagination correctly.</summary>
  [IntegrationTest]
  public async Task ListAllKeysAsync_ShouldIterateThroughAllKeys()
  {
    // Arrange - Create multiple keys
    var prefix = $"paginate-{Guid.NewGuid():N}/";
    var keys = Enumerable.Range(1, 5).Select(i => $"{prefix}key{i}").ToList();

    foreach (var key in keys)
      await _sut.WriteValueAsync(_namespaceId, key, $"value for {key}");

    try
    {
      // Act
      var allKeys = new List<KvKey>();
      await foreach (var key in _sut.ListAllKeysAsync(_namespaceId, prefix))
        allKeys.Add(key);

      // Assert
      allKeys.Should().HaveCount(5, "all keys with the prefix should be returned");
      allKeys.Select(k => k.Name).Should().BeEquivalentTo(keys);
    }
    finally
    {
      // Cleanup
      foreach (var key in keys)
        await _sut.DeleteValueAsync(_namespaceId, key);
    }
  }

  /// <summary>Verifies that ListKeysAsync returns keys with their metadata.</summary>
  [IntegrationTest]
  public async Task ListKeysAsync_IncludesKeyMetadata()
  {
    // Arrange
    var key = $"metadata-key-{Guid.NewGuid():N}";
    var metadata = JsonSerializer.SerializeToElement(new { listTest = true });
    await _sut.WriteValueAsync(_namespaceId, key, "value", new KvWriteOptions(Metadata: metadata));

    try
    {
      // Act
      var result = await _sut.ListKeysAsync(_namespaceId, new ListKvKeysFilters(Prefix: key));

      // Assert
      result.Items.Should().ContainSingle();
      var foundKey = result.Items[0];
      foundKey.Name.Should().Be(key);
      foundKey.Metadata.Should().NotBeNull("metadata should be included in list response");
      foundKey.Metadata!.Value.GetProperty("listTest").GetBoolean().Should().BeTrue();
    }
    finally
    {
      // Cleanup
      await _sut.DeleteValueAsync(_namespaceId, key);
    }
  }

  #endregion


  #region Bulk Operations

  /// <summary>Verifies that multiple key-value pairs can be written in a single bulk operation.</summary>
  [IntegrationTest]
  public async Task BulkWriteAsync_CanWriteMultipleKeys()
  {
    // Arrange
    var prefix = $"bulk-write-{Guid.NewGuid():N}/";
    var items = new[]
    {
      new KvBulkWriteItem($"{prefix}key1", "value1"),
      new KvBulkWriteItem($"{prefix}key2", "value2"),
      new KvBulkWriteItem($"{prefix}key3", "value3")
    };

    try
    {
      // Act
      var result = await _sut.BulkWriteAsync(_namespaceId, items);

      // Assert
      result.SuccessfulKeyCount.Should().Be(3);
      result.UnsuccessfulKeys.Should().BeNullOrEmpty();

      // Verify values were written
      var value1 = await _sut.GetValueAsync(_namespaceId, $"{prefix}key1");
      value1.Should().Be("value1");

      var value2 = await _sut.GetValueAsync(_namespaceId, $"{prefix}key2");
      value2.Should().Be("value2");

      var value3 = await _sut.GetValueAsync(_namespaceId, $"{prefix}key3");
      value3.Should().Be("value3");
    }
    finally
    {
      // Cleanup
      foreach (var item in items)
        await _sut.DeleteValueAsync(_namespaceId, item.Key);
    }
  }

  /// <summary>Verifies that bulk write can include metadata and expiration.</summary>
  [IntegrationTest]
  public async Task BulkWriteAsync_CanIncludeMetadataAndExpiration()
  {
    // Arrange
    var prefix = $"bulk-meta-{Guid.NewGuid():N}/";
    var metadata = JsonSerializer.SerializeToElement(new { bulk = true });
    var items = new[]
    {
      new KvBulkWriteItem(
        $"{prefix}key1",
        "value with metadata",
        Metadata: metadata,
        ExpirationTtl: 300) // 5 minutes
    };

    try
    {
      // Act
      var result = await _sut.BulkWriteAsync(_namespaceId, items);

      // Assert
      result.SuccessfulKeyCount.Should().Be(1);

      // Verify metadata was written
      var readMetadata = await _sut.GetMetadataAsync(_namespaceId, $"{prefix}key1");
      readMetadata.Should().NotBeNull();
      readMetadata!.Value.GetProperty("bulk").GetBoolean().Should().BeTrue();
    }
    finally
    {
      // Cleanup
      await _sut.DeleteValueAsync(_namespaceId, $"{prefix}key1");
    }
  }

  /// <summary>Verifies that multiple keys can be deleted in a single bulk operation.</summary>
  [IntegrationTest]
  public async Task BulkDeleteAsync_CanDeleteMultipleKeys()
  {
    // Arrange - Write some keys first
    var prefix = $"bulk-delete-{Guid.NewGuid():N}/";
    var keys = new[] { $"{prefix}key1", $"{prefix}key2", $"{prefix}key3" };

    foreach (var key in keys)
      await _sut.WriteValueAsync(_namespaceId, key, $"value for {key}");

    // Verify they exist
    foreach (var key in keys)
    {
      var value = await _sut.GetValueAsync(_namespaceId, key);
      value.Should().NotBeNull($"{key} should exist before deletion");
    }

    // Act
    var result = await _sut.BulkDeleteAsync(_namespaceId, keys);

    // Assert
    result.SuccessfulKeyCount.Should().Be(3);
    result.UnsuccessfulKeys.Should().BeNullOrEmpty();

    // Verify they are deleted (retry for eventual consistency - no delays, just retries)
    const int maxRetries = 10;
    foreach (var key in keys)
    {
      string? value = "not null";
      for (var attempt = 1; attempt <= maxRetries && value is not null; attempt++)
        value = await _sut.GetValueAsync(_namespaceId, key);

      value.Should().BeNull($"{key} should be deleted after {maxRetries} retries");
    }
  }

  /// <summary>Verifies that multiple values can be retrieved in a single bulk get operation.</summary>
  [IntegrationTest]
  public async Task BulkGetAsync_CanRetrieveMultipleValues()
  {
    // Arrange
    var prefix = $"bulk-get-{Guid.NewGuid():N}/";
    var keyValues = new Dictionary<string, string>
    {
      { $"{prefix}key1", "value1" },
      { $"{prefix}key2", "value2" },
      { $"{prefix}key3", "value3" }
    };

    foreach (var kv in keyValues)
      await _sut.WriteValueAsync(_namespaceId, kv.Key, kv.Value);

    try
    {
      // Act
      var result = await _sut.BulkGetAsync(_namespaceId, keyValues.Keys);

      // Assert
      result.Should().HaveCount(3);
      foreach (var kv in keyValues)
        result[kv.Key].Should().Be(kv.Value);
    }
    finally
    {
      // Cleanup
      foreach (var key in keyValues.Keys)
        await _sut.DeleteValueAsync(_namespaceId, key);
    }
  }

  /// <summary>Verifies that bulk get returns null for non-existent keys.</summary>
  [IntegrationTest]
  public async Task BulkGetAsync_ReturnsNullForNonExistentKeys()
  {
    // Arrange
    var existingKey = $"bulk-exist-{Guid.NewGuid():N}";
    var nonExistentKey = $"bulk-missing-{Guid.NewGuid():N}";

    await _sut.WriteValueAsync(_namespaceId, existingKey, "exists");

    try
    {
      // Act
      var result = await _sut.BulkGetAsync(_namespaceId, new[] { existingKey, nonExistentKey });

      // Assert
      result.Should().HaveCount(2);
      result[existingKey].Should().Be("exists");
      result[nonExistentKey].Should().BeNull("non-existent keys should have null values");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteValueAsync(_namespaceId, existingKey);
    }
  }

  /// <summary>Verifies that bulk get with metadata returns values and their metadata.</summary>
  [IntegrationTest]
  public async Task BulkGetWithMetadataAsync_ReturnsValuesAndMetadata()
  {
    // Arrange
    var prefix = $"bulk-meta-get-{Guid.NewGuid():N}/";
    var key = $"{prefix}key1";
    var metadata = JsonSerializer.SerializeToElement(new { category = "bulk-test" });

    await _sut.WriteValueAsync(_namespaceId, key, "value with metadata", new KvWriteOptions(Metadata: metadata));

    try
    {
      // Act
      var result = await _sut.BulkGetWithMetadataAsync(_namespaceId, new[] { key });

      // Assert
      result.Should().HaveCount(1);
      result[key].Should().NotBeNull();
      result[key]!.Value.Should().Be("value with metadata");
      result[key]!.Metadata.Should().NotBeNull();
      result[key]!.Metadata!.Value.GetProperty("category").GetString().Should().Be("bulk-test");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteValueAsync(_namespaceId, key);
    }
  }

  #endregion


  #region Error Handling

  /// <summary>Verifies that GetAsync for a non-existent namespace returns appropriate error.</summary>
  [IntegrationTest]
  public async Task GetAsync_NonExistentNamespace_ThrowsError()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid().ToString();

    // Act
    var action = async () => await _sut.GetAsync(nonExistentId);

    // Assert
    await action.Should().ThrowAsync<Exception>("accessing a non-existent namespace should fail");
  }

  /// <summary>Verifies that DeleteAsync for a non-existent namespace throws appropriate error.</summary>
  [IntegrationTest]
  public async Task DeleteAsync_NonExistentNamespace_ThrowsError()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid().ToString();

    // Act
    var action = async () => await _sut.DeleteAsync(nonExistentId);

    // Assert
    await action.Should().ThrowAsync<Exception>("deleting a non-existent namespace should fail");
  }

  #endregion
}
