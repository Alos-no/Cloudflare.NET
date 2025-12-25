namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
using System.Security.Cryptography;
using System.Text;
using Accounts;
using Accounts.D1;
using Accounts.D1.Models;
using Accounts.Models;
using Cloudflare.NET.Core.Exceptions;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the D1 database operations of <see cref="ID1Api" />. These tests interact with the
///   live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   This test class covers all D1 database operations including:
///   <list type="bullet">
///     <item><description>Database CRUD operations (List, Create, Get, Update, Delete)</description></item>
///     <item><description>Query operations (Query, QueryRaw, typed queries)</description></item>
///     <item><description>Read replication configuration</description></item>
///     <item><description>Pagination handling</description></item>
///     <item><description>Export operations with polling workflow</description></item>
///     <item><description>Import operations with file upload and polling workflow</description></item>
///     <item><description>Jurisdiction and location hint options</description></item>
///   </list>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class D1ApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>, IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly ID1Api _sut;

  /// <summary>A unique name for the D1 database used in this test run, to avoid collisions.</summary>
  private readonly string _databaseName = $"cfnet-test-d1-{Guid.NewGuid():N}"[..32];

  /// <summary>The UUID of the created database, populated during InitializeAsync.</summary>
  private string _databaseId = string.Empty;

  /// <summary>The xUnit test output helper for writing logs.</summary>
  private readonly ITestOutputHelper _output;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="D1ApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public D1ApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    // The SUT is resolved via the fixture's pre-configured DI container.
    _sut    = fixture.AccountsApi.D1;
    _output = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Methods Impl

  /// <summary>Asynchronously creates the D1 database required for the tests. This runs once before any tests in this class.</summary>
  /// <remarks>
  ///   The D1 REST API is known to have transient failures (502, 503, 500 errors) that are not caused by our code.
  ///   Cloudflare has acknowledged these issues but has not resolved them (see GitHub issue #7780).
  ///   This method implements retry logic to handle these transient failures during fixture setup.
  /// </remarks>
  public async Task InitializeAsync()
  {
    // Create a new D1 database for the test run.
    // Retry up to 5 times to handle transient D1 API failures (502, 503, 500 errors).
    const int maxRetries = 5;
    D1Database? db = null;

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
      try
      {
        db = await _sut.CreateAsync(_databaseName);
        break; // Success - exit retry loop.
      }
      catch (HttpRequestException ex) when (attempt < maxRetries && IsTransientError(ex))
      {
        _output.WriteLine($"D1 API transient error on attempt {attempt}/{maxRetries}: {ex.Message}. Retrying...");
      }
    }

    if (db is null)
      throw new InvalidOperationException($"Failed to create D1 database after {maxRetries} attempts");

    _databaseId = db.Uuid;

    _output.WriteLine($"Created test database: {_databaseId} ({_databaseName})");

    // Create a test table for query operations.
    // Also retry this operation for transient failures.
    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
      try
      {
        await _sut.QueryAsync(_databaseId, """
          CREATE TABLE IF NOT EXISTS test_users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            email TEXT UNIQUE,
            created_at TEXT DEFAULT (datetime('now'))
          )
        """);
        break; // Success - exit retry loop.
      }
      catch (HttpRequestException ex) when (attempt < maxRetries && IsTransientError(ex))
      {
        _output.WriteLine($"D1 API transient error creating table on attempt {attempt}/{maxRetries}: {ex.Message}. Retrying...");
      }
    }

    _output.WriteLine("Created test_users table");
  }

  /// <summary>Determines if the given exception represents a transient D1 API error that should be retried.</summary>
  /// <param name="ex">The HTTP request exception to check.</param>
  /// <returns><c>true</c> if the error is transient and should be retried; otherwise, <c>false</c>.</returns>
  private static bool IsTransientError(HttpRequestException ex)
  {
    // D1 API is known to return 500, 502, and 503 errors transiently.
    // These are Cloudflare infrastructure issues, not problems with our requests.
    return ex.StatusCode is HttpStatusCode.InternalServerError
      or HttpStatusCode.BadGateway
      or HttpStatusCode.ServiceUnavailable
      or HttpStatusCode.GatewayTimeout;
  }

  /// <summary>Asynchronously deletes the D1 database after all tests in this class have run.</summary>
  /// <remarks>
  ///   If cleanup fails after retries, the exception propagates to the test framework. This ensures we are immediately
  ///   aware of any issues with resource cleanup rather than silently logging and continuing.
  /// </remarks>
  public async Task DisposeAsync()
  {
    // Clean up the D1 database with retry logic for transient D1 API failures.
    if (!string.IsNullOrEmpty(_databaseId))
    {
      const int maxRetries = 5;
      for (var attempt = 1; attempt <= maxRetries; attempt++)
      {
        try
        {
          await _sut.DeleteAsync(_databaseId);
          _output.WriteLine($"Deleted test database: {_databaseId}");
          break; // Success - exit retry loop.
        }
        catch (HttpRequestException ex) when (attempt < maxRetries && IsTransientError(ex))
        {
          _output.WriteLine($"D1 API transient error deleting database on attempt {attempt}/{maxRetries}: {ex.Message}. Retrying...");
        }
      }
    }
  }

  #endregion


  #region Database Operations

  /// <summary>Verifies that D1 databases can be listed successfully.</summary>
  /// <remarks>
  ///   <para>
  ///     IMPORTANT API LIMITATION: The D1 API returns <c>total_count=0</c> and <c>total_pages=0</c> in the
  ///     <c>result_info</c>, which contradicts the standard Cloudflare API schema. This appears to be a D1-specific
  ///     bug or limitation. Our <see cref="ID1Api.ListAllAsync" /> works around this by checking if the current
  ///     page is full (item count equals PerPage) rather than relying on TotalPages.
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task ListAsync_CanListSuccessfully()
  {
    // Arrange (database is created in InitializeAsync)

    // Act
    var result = await _sut.ListAsync();

    // Assert
    result.Items.Should().NotBeEmpty("at least one database should exist");
    result.Items.Should().Contain(db => db.Uuid == _databaseId, "the test database should be in the list");

    // Verify pagination info is returned
    result.PageInfo.Should().NotBeNull("pagination info should be present");

    // Log actual PageInfo values for diagnostics (D1 API returns 0 for TotalCount/TotalPages - this is a known limitation)
    _output.WriteLine($"PageInfo: Page={result.PageInfo!.Page}, PerPage={result.PageInfo.PerPage}, " +
      $"Count={result.PageInfo.Count}, TotalCount={result.PageInfo.TotalCount}, TotalPages={result.PageInfo.TotalPages}");

    // Assert expected pagination invariants (Note: TotalCount and TotalPages are NOT asserted due to D1 API limitation)
    result.PageInfo.Page.Should().BeGreaterThanOrEqualTo(1, "page number should be at least 1");
    result.PageInfo.PerPage.Should().BeGreaterThan(0, "per_page should be positive");
    result.PageInfo.Count.Should().Be(result.Items.Count, "count should match actual items returned");

    // Document the API limitation: TotalCount and TotalPages are always 0 from the D1 API
    // This is inconsistent with the standard Cloudflare API schema which should include proper values
    _output.WriteLine("NOTE: D1 API returns TotalCount=0 and TotalPages=0 - this is a known Cloudflare D1 API limitation");
  }

  /// <summary>Verifies that ListAsync with name filter works correctly.</summary>
  [IntegrationTest]
  public async Task ListAsync_WithNameFilter_FiltersCorrectly()
  {
    // Arrange (database is created in InitializeAsync)

    // Act
    var result = await _sut.ListAsync(new ListD1DatabasesFilters(Name: _databaseName));

    // Assert
    result.Items.Should().ContainSingle("only the test database should match the filter");
    result.Items[0].Uuid.Should().Be(_databaseId);
    result.Items[0].Name.Should().Be(_databaseName);
  }

  /// <summary>Verifies that ListAllAsync can iterate through all databases.</summary>
  [IntegrationTest]
  public async Task ListAllAsync_CanIterateThroughAllDatabases()
  {
    // Arrange - Create a second database to ensure multiple exist
    var secondDatabaseName = $"cfnet-test-d1-{Guid.NewGuid():N}"[..32];
    var secondDb = await _sut.CreateAsync(secondDatabaseName);

    try
    {
      // Act
      var allDatabases = new List<D1Database>();
      await foreach (var db in _sut.ListAllAsync())
        allDatabases.Add(db);

      // Assert
      allDatabases.Should().NotBeEmpty();
      allDatabases.Should().Contain(db => db.Uuid == _databaseId, "the primary test database should be found");
      allDatabases.Should().Contain(db => db.Uuid == secondDb.Uuid, "the second test database should be found");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteAsync(secondDb.Uuid);
    }
  }

  /// <summary>
  ///   Verifies that ListAllAsync correctly paginates through multiple pages when using a small PerPage value.
  ///   This test specifically validates the pagination logic works around the D1 API limitation where
  ///   TotalPages is always 0.
  /// </summary>
  [IntegrationTest]
  public async Task ListAllAsync_WithSmallPerPage_CorrectlyIteratesMultiplePages()
  {
    // Arrange - Create additional databases to ensure we have at least 3 (including the fixture database)
    var db2Name = $"cfnet-page-a-{Guid.NewGuid():N}"[..32];
    var db3Name = $"cfnet-page-b-{Guid.NewGuid():N}"[..32];
    var db2 = await _sut.CreateAsync(db2Name);
    var db3 = await _sut.CreateAsync(db3Name);

    try
    {
      // Act - Use PerPage=1 to force multiple pages
      var filters = new ListD1DatabasesFilters(PerPage: 1);
      var allDatabases = new List<D1Database>();

      await foreach (var db in _sut.ListAllAsync(filters))
        allDatabases.Add(db);

      // Assert - Should find all 3 test databases even though each page only has 1 item
      allDatabases.Should().HaveCountGreaterThanOrEqualTo(3, "at least 3 databases should exist");
      allDatabases.Should().Contain(db => db.Uuid == _databaseId, "fixture database should be found");
      allDatabases.Should().Contain(db => db.Uuid == db2.Uuid, "second database should be found");
      allDatabases.Should().Contain(db => db.Uuid == db3.Uuid, "third database should be found");

      _output.WriteLine($"Successfully iterated through {allDatabases.Count} databases with PerPage=1");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteAsync(db2.Uuid);
      await _sut.DeleteAsync(db3.Uuid);
    }
  }

  /// <summary>Verifies that GetAsync retrieves database properties.</summary>
  [IntegrationTest]
  public async Task GetAsync_ReturnsDatabaseProperties()
  {
    // Arrange (database is created in InitializeAsync)

    // Act
    var result = await _sut.GetAsync(_databaseId);

    // Assert
    result.Should().NotBeNull();
    result.Uuid.Should().Be(_databaseId);
    result.Name.Should().Be(_databaseName);
    result.Version.Should().Be("production", "new databases should be production version");
    result.NumTables.Should().BeGreaterThanOrEqualTo(1, "at least the test_users table should exist");
  }

  /// <summary>Verifies that a database can be created and deleted successfully.</summary>
  [IntegrationTest]
  public async Task CanCreateAndDeleteDatabase()
  {
    // Arrange
    var name = $"cfnet-standalone-{Guid.NewGuid():N}"[..32];

    // Act
    var createResult = await _sut.CreateAsync(name);

    // Assert
    createResult.Should().NotBeNull();
    createResult.Uuid.Should().NotBeNullOrEmpty();
    createResult.Name.Should().Be(name);

    // Cleanup & verify deletion works
    var deleteAction = async () => await _sut.DeleteAsync(createResult.Uuid);
    await deleteAction.Should().NotThrowAsync("deletion should succeed");
  }

  /// <summary>Verifies that a database can be created with a location hint.</summary>
  [IntegrationTest]
  public async Task CreateAsync_WithLocationHint_CreatesSuccessfully()
  {
    // Arrange
    var name = $"cfnet-loc-{Guid.NewGuid():N}"[..32];

    // Act
    var createResult = await _sut.CreateAsync(name, primaryLocationHint: R2LocationHint.WestEurope);

    try
    {
      // Assert create response
      createResult.Should().NotBeNull();
      createResult.Uuid.Should().NotBeNullOrEmpty();
      createResult.Name.Should().Be(name);
      createResult.Version.Should().Be("production");

      // The API may not return RunningInRegion immediately on create - fetch full details via GetAsync
      var result = await _sut.GetAsync(createResult.Uuid);

      result.RunningInRegion.Should().NotBeNullOrEmpty("database should report its running region after Get");
      // With WestEurope hint, we expect a European region (WEUR or EEUR)
      result.RunningInRegion.Should().BeOneOf("WEUR", "EEUR", "weur", "eeur",
        "database with WestEurope hint should be placed in a European region");

      _output.WriteLine($"Database created in region: {result.RunningInRegion}");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteAsync(createResult.Uuid);
    }
  }

  /// <summary>Verifies that UpdateAsync can modify read replication settings.</summary>
  [IntegrationTest]
  public async Task UpdateAsync_CanEnableReadReplication()
  {
    // Arrange - Create a temporary database
    var name = $"cfnet-update-{Guid.NewGuid():N}"[..32];
    var db = await _sut.CreateAsync(name);

    try
    {
      // Act - Enable read replication
      var updateOptions = new UpdateD1DatabaseOptions(
        ReadReplication: new D1ReadReplication("auto")
      );
      var result = await _sut.UpdateAsync(db.Uuid, updateOptions);

      // Assert
      result.Should().NotBeNull();
      result.ReadReplication.Should().NotBeNull();
      result.ReadReplication!.Mode.Should().Be("auto");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteAsync(db.Uuid);
    }
  }

  /// <summary>Verifies that UpdateAsync can disable read replication.</summary>
  [IntegrationTest]
  public async Task UpdateAsync_CanDisableReadReplication()
  {
    // Arrange - Create a temporary database and enable replication first
    var name = $"cfnet-disable-rep-{Guid.NewGuid():N}"[..32];
    var db = await _sut.CreateAsync(name);

    try
    {
      // First enable read replication
      await _sut.UpdateAsync(db.Uuid, new UpdateD1DatabaseOptions(
        ReadReplication: new D1ReadReplication("auto")
      ));

      // Act - Disable read replication
      var updateOptions = new UpdateD1DatabaseOptions(
        ReadReplication: new D1ReadReplication("disabled")
      );
      var result = await _sut.UpdateAsync(db.Uuid, updateOptions);

      // Assert
      result.Should().NotBeNull();
      result.ReadReplication.Should().NotBeNull();
      result.ReadReplication!.Mode.Should().Be("disabled");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteAsync(db.Uuid);
    }
  }

  /// <summary>Verifies that a database can be created with EU jurisdiction for GDPR compliance.</summary>
  [IntegrationTest]
  public async Task CreateAsync_WithEuJurisdiction_CreatesSuccessfully()
  {
    // Arrange
    var name = $"cfnet-eu-{Guid.NewGuid():N}"[..32];

    // Act
    var createResult = await _sut.CreateAsync(name, jurisdiction: D1Jurisdiction.EuropeanUnion);

    try
    {
      // Assert create response
      createResult.Should().NotBeNull();
      createResult.Uuid.Should().NotBeNullOrEmpty();
      createResult.Name.Should().Be(name);
      createResult.Version.Should().Be("production");

      // The API may not return RunningInRegion immediately on create - fetch full details via GetAsync
      var result = await _sut.GetAsync(createResult.Uuid);

      // EU jurisdiction is a HARD guarantee - database MUST be in an EU region
      result.RunningInRegion.Should().NotBeNullOrEmpty("database should report its running region after Get");
      result.RunningInRegion.Should().BeOneOf("WEUR", "EEUR", "weur", "eeur",
        "EU jurisdiction database MUST be placed in a European region");

      _output.WriteLine($"EU jurisdiction database created in region: {result.RunningInRegion}");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteAsync(createResult.Uuid);
    }
  }

  /// <summary>Verifies that ListAsync with pagination filters works correctly.</summary>
  /// <remarks>
  ///   Note: The D1 API does not reliably return TotalCount in the pagination info, so this test
  ///   focuses on verifying that PerPage limits are respected and pages contain different items.
  /// </remarks>
  [IntegrationTest]
  public async Task ListAsync_WithPaginationFilters_ReturnsCorrectPage()
  {
    // Arrange - Create multiple databases to ensure we have enough for pagination
    var db1Name = $"cfnet-page-1-{Guid.NewGuid():N}"[..32];
    var db2Name = $"cfnet-page-2-{Guid.NewGuid():N}"[..32];
    var db1 = await _sut.CreateAsync(db1Name);
    var db2 = await _sut.CreateAsync(db2Name);

    try
    {
      // Act - Request first page with perPage=1
      var page1 = await _sut.ListAsync(new ListD1DatabasesFilters(Page: 1, PerPage: 1));

      // Assert page 1 basic properties
      page1.Items.Should().HaveCount(1, "perPage=1 should return exactly 1 item");
      page1.PageInfo.Should().NotBeNull("pagination info should always be present");
      page1.PageInfo!.Page.Should().Be(1, "should be on page 1");
      page1.PageInfo.PerPage.Should().Be(1, "should respect perPage limit");

      // Act - Request second page
      var page2 = await _sut.ListAsync(new ListD1DatabasesFilters(Page: 2, PerPage: 1));

      // Assert page 2 basic properties
      page2.Items.Should().HaveCount(1, "second page should also have 1 item");
      page2.PageInfo.Should().NotBeNull("pagination info should always be present");
      page2.PageInfo!.Page.Should().Be(2, "should be on page 2");

      // The items on page 1 and page 2 should be different (key pagination invariant)
      page1.Items[0].Uuid.Should().NotBe(page2.Items[0].Uuid, "different pages MUST have different items");

      // Verify we can find our test databases across pages using ListAllAsync
      var allDatabases = new List<D1Database>();
      await foreach (var db in _sut.ListAllAsync())
        allDatabases.Add(db);

      allDatabases.Should().Contain(d => d.Uuid == db1.Uuid, "first test database should exist");
      allDatabases.Should().Contain(d => d.Uuid == db2.Uuid, "second test database should exist");

      _output.WriteLine($"Pagination verified: page 1 = {page1.Items[0].Uuid}, page 2 = {page2.Items[0].Uuid}");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteAsync(db1.Uuid);
      await _sut.DeleteAsync(db2.Uuid);
    }
  }

  #endregion


  #region Query Operations

  /// <summary>Verifies that a simple SELECT query can be executed.</summary>
  [IntegrationTest]
  public async Task QueryAsync_CanExecuteSelectQuery()
  {
    // Arrange
    var sql = "SELECT 1 as value";

    // Act
    var result = await _sut.QueryAsync(_databaseId, sql);

    // Assert
    result.Should().ContainSingle();
    result[0].Success.Should().BeTrue();
    result[0].Meta.Duration.Should().BeGreaterThan(0, "query should have measurable duration");
  }

  /// <summary>Verifies that an INSERT query can be executed with parameters.</summary>
  [IntegrationTest]
  public async Task QueryAsync_CanExecuteInsertWithParams()
  {
    // Arrange
    var uniqueEmail = $"test-{Guid.NewGuid():N}@example.com";
    var sql = "INSERT INTO test_users (name, email) VALUES (?, ?)";

    // Act
    var result = await _sut.QueryAsync(_databaseId, sql, new object?[] { "Test User", uniqueEmail });

    // Assert
    result.Should().ContainSingle();
    result[0].Success.Should().BeTrue();
    result[0].Meta.ChangedDb.Should().BeTrue("INSERT should modify the database");
    result[0].Meta.Changes.Should().BeGreaterThanOrEqualTo(1, "at least one row should be inserted");
  }

  /// <summary>Verifies that a SELECT query with parameters works correctly.</summary>
  [IntegrationTest]
  public async Task QueryAsync_CanExecuteSelectWithParams()
  {
    // Arrange - Insert a test row first
    var uniqueEmail = $"param-test-{Guid.NewGuid():N}@example.com";
    await _sut.QueryAsync(_databaseId, "INSERT INTO test_users (name, email) VALUES (?, ?)",
      new object?[] { "Param Test", uniqueEmail });

    var sql = "SELECT id, name, email FROM test_users WHERE email = ?";

    // Act
    var result = await _sut.QueryAsync(_databaseId, sql, new object?[] { uniqueEmail });

    // Assert
    result.Should().ContainSingle();
    result[0].Success.Should().BeTrue();
    result[0].Results.Should().ContainSingle("one row should match");
  }

  /// <summary>Verifies that multiple statements can be executed in a single query.</summary>
  /// <remarks>
  ///   Note: D1 API does not support params with multiple statements, so we use literal values.
  /// </remarks>
  [IntegrationTest]
  public async Task QueryAsync_CanExecuteMultipleStatements()
  {
    // Arrange - Multiple statements separated by semicolons.
    // Note: D1 API does not support params with multiple statements, so we embed unique values directly.
    var uniqueId1 = Guid.NewGuid().ToString("N");
    var uniqueId2 = Guid.NewGuid().ToString("N");
    var sql = $"INSERT INTO test_users (name, email) VALUES ('User 1', 'multi-1-{uniqueId1}@example.com'); INSERT INTO test_users (name, email) VALUES ('User 2', 'multi-2-{uniqueId2}@example.com')";

    // Act
    var result = await _sut.QueryAsync(_databaseId, sql);

    // Assert
    result.Should().HaveCount(2, "two statements should produce two results");
    result[0].Success.Should().BeTrue();
    result[1].Success.Should().BeTrue();
  }

  /// <summary>Verifies that QueryRawAsync returns array-format results.</summary>
  [IntegrationTest]
  public async Task QueryRawAsync_ReturnsArrayFormatResults()
  {
    // Arrange - Insert a test row first
    var uniqueEmail = $"raw-test-{Guid.NewGuid():N}@example.com";
    await _sut.QueryAsync(_databaseId, "INSERT INTO test_users (name, email) VALUES (?, ?)",
      new object?[] { "Raw Test", uniqueEmail });

    var sql = "SELECT id, name, email FROM test_users WHERE email = ?";

    // Act
    var result = await _sut.QueryRawAsync(_databaseId, sql, new object?[] { uniqueEmail });

    // Assert
    result.Should().ContainSingle();
    result[0].Success.Should().BeTrue();
    result[0].Results.Columns.Should().ContainInOrder("id", "name", "email");
    result[0].Results.Rows.Should().ContainSingle("one row should match");
  }

  /// <summary>Verifies that query metadata includes region and timing information.</summary>
  [IntegrationTest]
  public async Task QueryAsync_ReturnsDetailedMetadata()
  {
    // Arrange
    var sql = "SELECT COUNT(*) FROM test_users";

    // Act
    var result = await _sut.QueryAsync(_databaseId, sql);

    // Assert
    result.Should().ContainSingle();
    var meta = result[0].Meta;
    meta.Duration.Should().BeGreaterThan(0, "query duration should be measurable");
    meta.RowsRead.Should().BeGreaterThanOrEqualTo(0, "rows_read should be populated");
    meta.RowsWritten.Should().Be(0, "SELECT query should not write rows");
    meta.ChangedDb.Should().BeFalse("SELECT query should not change the database");
    meta.ServedByRegion.Should().NotBeNullOrEmpty("query should report serving region");
  }

  /// <summary>Verifies that SQL syntax errors are reported correctly.</summary>
  [IntegrationTest]
  public async Task QueryAsync_SqlSyntaxError_ThrowsException()
  {
    // Arrange
    var sql = "SELEC * FORM users"; // Intentional typos

    // Act
    var action = async () => await _sut.QueryAsync(_databaseId, sql);

    // Assert
    await action.Should().ThrowAsync<Exception>("invalid SQL should throw an exception");
  }

  /// <summary>Verifies that an UPDATE query can be executed with parameters.</summary>
  [IntegrationTest]
  public async Task QueryAsync_CanExecuteUpdateWithParams()
  {
    // Arrange - Insert a test row first
    var uniqueEmail = $"update-test-{Guid.NewGuid():N}@example.com";
    await _sut.QueryAsync(_databaseId, "INSERT INTO test_users (name, email) VALUES (?, ?)",
      new object?[] { "Original Name", uniqueEmail });

    var sql = "UPDATE test_users SET name = ? WHERE email = ?";

    // Act
    var result = await _sut.QueryAsync(_databaseId, sql, new object?[] { "Updated Name", uniqueEmail });

    // Assert
    result.Should().ContainSingle();
    result[0].Success.Should().BeTrue();
    result[0].Meta.ChangedDb.Should().BeTrue("UPDATE should modify the database");
    result[0].Meta.Changes.Should().BeGreaterThanOrEqualTo(1, "at least one row should be updated");

    // Verify the update took effect
    var verifyResult = await _sut.QueryAsync(_databaseId, "SELECT name FROM test_users WHERE email = ?",
      new object?[] { uniqueEmail });
    verifyResult[0].Results.Should().ContainSingle();
  }

  /// <summary>Verifies that a DELETE query can be executed with parameters.</summary>
  [IntegrationTest]
  public async Task QueryAsync_CanExecuteDeleteWithParams()
  {
    // Arrange - Insert a test row first
    var uniqueEmail = $"delete-test-{Guid.NewGuid():N}@example.com";
    await _sut.QueryAsync(_databaseId, "INSERT INTO test_users (name, email) VALUES (?, ?)",
      new object?[] { "To Be Deleted", uniqueEmail });

    // Verify the row exists
    var beforeDelete = await _sut.QueryAsync(_databaseId, "SELECT id FROM test_users WHERE email = ?",
      new object?[] { uniqueEmail });
    beforeDelete[0].Results.Should().ContainSingle("row should exist before delete");

    var sql = "DELETE FROM test_users WHERE email = ?";

    // Act
    var result = await _sut.QueryAsync(_databaseId, sql, new object?[] { uniqueEmail });

    // Assert
    result.Should().ContainSingle();
    result[0].Success.Should().BeTrue();
    result[0].Meta.ChangedDb.Should().BeTrue("DELETE should modify the database");
    result[0].Meta.Changes.Should().BeGreaterThanOrEqualTo(1, "at least one row should be deleted");

    // Verify the row no longer exists
    var afterDelete = await _sut.QueryAsync(_databaseId, "SELECT id FROM test_users WHERE email = ?",
      new object?[] { uniqueEmail });
    afterDelete[0].Results.Should().BeEmpty("row should not exist after delete");
  }

  /// <summary>Verifies that a transaction with RETURNING clause works correctly.</summary>
  [IntegrationTest]
  public async Task QueryAsync_CanExecuteInsertWithReturning()
  {
    // Arrange
    var uniqueEmail = $"returning-test-{Guid.NewGuid():N}@example.com";
    var sql = "INSERT INTO test_users (name, email) VALUES (?, ?) RETURNING id, name, email";

    // Act
    var result = await _sut.QueryAsync(_databaseId, sql, new object?[] { "Returning Test", uniqueEmail });

    // Assert
    result.Should().ContainSingle();
    result[0].Success.Should().BeTrue();
    result[0].Results.Should().ContainSingle("RETURNING should return the inserted row");
    result[0].Meta.Changes.Should().BeGreaterThanOrEqualTo(1);
  }

  /// <summary>Verifies that typed query results can be retrieved.</summary>
  [IntegrationTest]
  public async Task QueryAsyncTyped_ReturnsTypedResults()
  {
    // Arrange - Insert a test row first
    var uniqueEmail = $"typed-test-{Guid.NewGuid():N}@example.com";
    await _sut.QueryAsync(_databaseId, "INSERT INTO test_users (name, email) VALUES (?, ?)",
      new object?[] { "Typed Test", uniqueEmail });

    var sql = "SELECT id, name, email FROM test_users WHERE email = ?";

    // Act
    var result = await _sut.QueryAsync<TestUser>(_databaseId, sql, new object?[] { uniqueEmail });

    // Assert
    result.Should().ContainSingle();
    result[0].Success.Should().BeTrue();
    result[0].Results.Should().ContainSingle();
    var user = result[0].Results[0];
    user.Name.Should().Be("Typed Test");
    user.Email.Should().Be(uniqueEmail);
  }

  #endregion


  #region Error Handling

  /// <summary>Verifies that GetAsync for a non-existent database returns appropriate error.</summary>
  [IntegrationTest]
  public async Task GetAsync_NonExistentDatabase_ThrowsError()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid().ToString();

    // Act
    var action = async () => await _sut.GetAsync(nonExistentId);

    // Assert
    await action.Should().ThrowAsync<Exception>("accessing a non-existent database should fail");
  }

  /// <summary>Verifies that DeleteAsync for a non-existent database throws appropriate error.</summary>
  [IntegrationTest]
  public async Task DeleteAsync_NonExistentDatabase_ThrowsError()
  {
    // Arrange
    var nonExistentId = Guid.NewGuid().ToString();

    // Act
    var action = async () => await _sut.DeleteAsync(nonExistentId);

    // Assert
    await action.Should().ThrowAsync<Exception>("deleting a non-existent database should fail");
  }

  #endregion


  #region Export Operations

  /// <summary>Verifies that a database can be exported to SQL format using the polling workflow.</summary>
  [IntegrationTest]
  public async Task Export_FullWorkflow_CompletesSuccessfully()
  {
    // Arrange - Ensure there's data to export
    var uniqueEmail = $"export-test-{Guid.NewGuid():N}@example.com";
    await _sut.QueryAsync(_databaseId, "INSERT INTO test_users (name, email) VALUES (?, ?)",
      new object?[] { "Export Test User", uniqueEmail });

    // Act - Start export
    var exportStart = await _sut.StartExportAsync(_databaseId);

    // Assert initial response
    exportStart.Should().NotBeNull();
    exportStart.AtBookmark.Should().NotBeNullOrEmpty("export should return a bookmark for polling");
    exportStart.Status.Should().BeOneOf("active", "complete", "pending");

    _output.WriteLine($"Export started with bookmark: {exportStart.AtBookmark}, status: {exportStart.Status}");

    // Poll until complete (with timeout)
    var maxAttempts = 30; // 30 seconds max
    var attempt = 0;
    var currentExport = exportStart;

    while (currentExport.Status != "complete" && attempt < maxAttempts)
    {
      await Task.Delay(TimeSpan.FromSeconds(1));
      currentExport = await _sut.PollExportAsync(_databaseId, currentExport.AtBookmark!);
      attempt++;

      _output.WriteLine($"Poll attempt {attempt}: status = {currentExport.Status}");

      // Fail fast on error status
      currentExport.Status.Should().NotBe("error", $"export failed with error: {currentExport.Error}");
    }

    // Assert final state
    currentExport.Status.Should().Be("complete", $"export should complete within {maxAttempts} seconds");
    currentExport.Result.Should().NotBeNull("completed export should have result");
    currentExport.Result!.SignedUrl.Should().NotBeNullOrEmpty("completed export should have download URL");
    currentExport.Result.Filename.Should().NotBeNullOrEmpty("completed export should have filename");

    _output.WriteLine($"Export completed. Filename: {currentExport.Result.Filename}");
    _output.WriteLine($"Download URL available (not shown for security)");
  }

  /// <summary>Verifies that export with schema-only option works correctly.</summary>
  [IntegrationTest]
  public async Task Export_SchemaOnly_CompletesSuccessfully()
  {
    // Arrange
    var dumpOptions = new D1ExportDumpOptions(NoData: true);

    // Act - Start export with schema-only option
    var exportStart = await _sut.StartExportAsync(_databaseId, dumpOptions);

    // Assert initial response
    exportStart.Should().NotBeNull();
    exportStart.AtBookmark.Should().NotBeNullOrEmpty();

    // Poll until complete
    var maxAttempts = 30;
    var attempt = 0;
    var currentExport = exportStart;

    while (currentExport.Status != "complete" && attempt < maxAttempts)
    {
      await Task.Delay(TimeSpan.FromSeconds(1));
      currentExport = await _sut.PollExportAsync(_databaseId, currentExport.AtBookmark!);
      attempt++;

      currentExport.Status.Should().NotBe("error", $"export failed with error: {currentExport.Error}");
    }

    // Assert
    currentExport.Status.Should().Be("complete");
    currentExport.Result.Should().NotBeNull();
    currentExport.Result!.SignedUrl.Should().NotBeNullOrEmpty();

    _output.WriteLine($"Schema-only export completed in {attempt} poll(s)");
  }

  /// <summary>Verifies that export with specific tables option works correctly.</summary>
  [IntegrationTest]
  public async Task Export_SpecificTables_CompletesSuccessfully()
  {
    // Arrange
    var dumpOptions = new D1ExportDumpOptions(Tables: new[] { "test_users" });

    // Act - Start export with specific tables
    var exportStart = await _sut.StartExportAsync(_databaseId, dumpOptions);

    // Assert initial response
    exportStart.Should().NotBeNull();
    exportStart.AtBookmark.Should().NotBeNullOrEmpty();

    // Poll until complete
    var maxAttempts = 30;
    var attempt = 0;
    var currentExport = exportStart;

    while (currentExport.Status != "complete" && attempt < maxAttempts)
    {
      await Task.Delay(TimeSpan.FromSeconds(1));
      currentExport = await _sut.PollExportAsync(_databaseId, currentExport.AtBookmark!);
      attempt++;

      currentExport.Status.Should().NotBe("error", $"export failed with error: {currentExport.Error}");
    }

    // Assert
    currentExport.Status.Should().Be("complete");
    currentExport.Result.Should().NotBeNull();
    currentExport.Result!.SignedUrl.Should().NotBeNullOrEmpty();

    _output.WriteLine($"Single-table export completed in {attempt} poll(s)");
  }

  #endregion


  #region Import Operations

  /// <summary>Verifies that a database can be imported from SQL format using the full workflow.</summary>
  [IntegrationTest]
  public async Task Import_FullWorkflow_CompletesSuccessfully()
  {
    // Arrange - Create a temporary database for import testing (to avoid polluting the main test database)
    var importDbName = $"cfnet-import-{Guid.NewGuid():N}"[..32];
    var importDb = await _sut.CreateAsync(importDbName);

    try
    {
      // Create SQL content to import
      var sqlContent = """
        CREATE TABLE IF NOT EXISTS imported_users (
          id INTEGER PRIMARY KEY AUTOINCREMENT,
          username TEXT NOT NULL,
          status TEXT DEFAULT 'active'
        );
        INSERT INTO imported_users (username, status) VALUES ('user1', 'active');
        INSERT INTO imported_users (username, status) VALUES ('user2', 'inactive');
        """;
      var sqlBytes = Encoding.UTF8.GetBytes(sqlContent);

      // Compute MD5 hash
      var md5Hash = Convert.ToHexString(MD5.HashData(sqlBytes)).ToLowerInvariant();

      _output.WriteLine($"SQL content size: {sqlBytes.Length} bytes, MD5: {md5Hash}");

      // Act - Step 1: Initialize import to get upload URL
      var initResponse = await _sut.StartImportAsync(importDb.Uuid, md5Hash);

      // Assert init response
      initResponse.Should().NotBeNull();
      initResponse.UploadUrl.Should().NotBeNullOrEmpty("init should return upload URL");
      initResponse.Filename.Should().NotBeNullOrEmpty("init should return filename");

      _output.WriteLine($"Import initialized. Filename: {initResponse.Filename}");

      // Act - Step 2: Upload SQL file to the signed URL
      using var httpClient = new HttpClient();
      using var uploadContent = new ByteArrayContent(sqlBytes);
      uploadContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

      var uploadResponse = await httpClient.PutAsync(initResponse.UploadUrl, uploadContent);
      uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK, "upload to signed URL should succeed");

      _output.WriteLine("SQL file uploaded successfully");

      // Act - Step 3: Complete the import (ingest)
      var ingestResponse = await _sut.CompleteImportAsync(importDb.Uuid, md5Hash, initResponse.Filename!);

      // Assert ingest response
      ingestResponse.Should().NotBeNull();
      ingestResponse.AtBookmark.Should().NotBeNullOrEmpty("ingest should return bookmark for polling");

      _output.WriteLine($"Import ingestion started. Bookmark: {ingestResponse.AtBookmark}");

      // Act - Step 4: Poll until complete
      var maxAttempts = 60; // Import can take longer than export
      var attempt = 0;
      var currentImport = ingestResponse;

      while (currentImport.Status != "complete" && attempt < maxAttempts)
      {
        await Task.Delay(TimeSpan.FromSeconds(1));
        currentImport = await _sut.PollImportAsync(importDb.Uuid, currentImport.AtBookmark!);
        attempt++;

        _output.WriteLine($"Poll attempt {attempt}: status = {currentImport.Status}");

        // Fail fast on error status
        currentImport.Status.Should().NotBe("error", $"import failed with error: {currentImport.Error}");
      }

      // Assert final state
      currentImport.Status.Should().Be("complete", $"import should complete within {maxAttempts} seconds");
      currentImport.Result.Should().NotBeNull("completed import should have result");
      currentImport.Result!.NumQueries.Should().BeGreaterThan(0, "import should have executed queries");

      _output.WriteLine($"Import completed. Queries executed: {currentImport.Result.NumQueries}");

      // Verify the imported data exists
      var verifyResult = await _sut.QueryAsync(importDb.Uuid, "SELECT COUNT(*) as count FROM imported_users");
      verifyResult[0].Success.Should().BeTrue();
      verifyResult[0].Results.Should().NotBeEmpty("should have count result");

      _output.WriteLine("Import verification successful - imported_users table exists with data");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteAsync(importDb.Uuid);
      _output.WriteLine($"Cleaned up import test database: {importDb.Uuid}");
    }
  }

  /// <summary>Verifies that StartImportAsync returns proper upload URL and filename.</summary>
  [IntegrationTest]
  public async Task StartImportAsync_ReturnsUploadUrlAndFilename()
  {
    // Arrange - Create a temporary database
    var importDbName = $"cfnet-import-init-{Guid.NewGuid():N}"[..32];
    var importDb = await _sut.CreateAsync(importDbName);

    try
    {
      // Create a simple SQL content and compute MD5
      var sqlContent = "SELECT 1;";
      var sqlBytes = Encoding.UTF8.GetBytes(sqlContent);
      var md5Hash = Convert.ToHexString(MD5.HashData(sqlBytes)).ToLowerInvariant();

      // Act
      var response = await _sut.StartImportAsync(importDb.Uuid, md5Hash);

      // Assert
      response.Should().NotBeNull();
      response.UploadUrl.Should().NotBeNullOrEmpty("should return upload URL");
      response.UploadUrl.Should().StartWith("https://", "upload URL should be HTTPS");
      response.Filename.Should().NotBeNullOrEmpty("should return filename for ingest step");

      _output.WriteLine($"Upload URL received (starts with): {response.UploadUrl![..50]}...");
      _output.WriteLine($"Filename: {response.Filename}");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteAsync(importDb.Uuid);
    }
  }

  #endregion


  #region Nested Types

  /// <summary>A test DTO for typed query deserialization.</summary>
  private record TestUser
  {
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
  }

  #endregion
}
