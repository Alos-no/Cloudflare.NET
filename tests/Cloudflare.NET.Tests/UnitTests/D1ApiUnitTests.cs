namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Accounts.D1;
using Accounts.D1.Models;
using Accounts.Models;
using Cloudflare.NET.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>Contains unit tests for the <see cref="D1Api" /> class.</summary>
/// <remarks>
///   This test class covers all D1 database operations including:
///   <list type="bullet">
///     <item><description>Database operations (List, Create, Get, Update, Delete)</description></item>
///     <item><description>Query operations (Query, QueryRaw)</description></item>
///     <item><description>Export operations (StartExport, PollExport)</description></item>
///     <item><description>Import operations (StartImport, CompleteImport, PollImport)</description></item>
///     <item><description>Error handling and edge cases</description></item>
///   </list>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class D1ApiUnitTests
{
  #region Properties & Fields - Non-Public

  /// <summary>The logger factory for creating loggers.</summary>
  private readonly ILoggerFactory _loggerFactory;

  /// <summary>JSON serializer options for snake_case property naming.</summary>
  private readonly JsonSerializerOptions _serializerOptions =
    new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

  /// <summary>The test account ID used in all tests.</summary>
  private const string TestAccountId = "test-account-id";

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="D1ApiUnitTests" /> class.</summary>
  /// <param name="output">The xUnit test output helper.</param>
  public D1ApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Helper Methods

  /// <summary>Creates the system under test with a mocked HTTP handler.</summary>
  /// <param name="responseContent">The JSON content to return.</param>
  /// <param name="statusCode">The HTTP status code to return.</param>
  /// <param name="callback">Optional callback to capture the request.</param>
  /// <returns>A configured <see cref="D1Api" /> instance.</returns>
  private D1Api CreateSut(
    string                                         responseContent,
    HttpStatusCode                                 statusCode = HttpStatusCode.OK,
    Action<HttpRequestMessage, CancellationToken>? callback   = null)
  {
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseContent, statusCode, callback);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options     = Options.Create(new CloudflareApiOptions { AccountId = TestAccountId });

    return new D1Api(httpClient, options, _loggerFactory);
  }

  /// <summary>Creates a paginated response for database listings.</summary>
  /// <param name="databases">The databases to include.</param>
  /// <param name="page">Current page number.</param>
  /// <param name="perPage">Items per page.</param>
  /// <param name="totalCount">Total number of items.</param>
  /// <returns>JSON string representing the paginated response.</returns>
  private string CreatePaginatedDatabaseResponse(D1Database[] databases, int page, int perPage, int totalCount)
  {
    var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / perPage);
    var response = new
    {
      success     = true,
      errors      = Array.Empty<object>(),
      messages    = Array.Empty<object>(),
      result      = databases,
      result_info = new
      {
        page,
        per_page    = perPage,
        count       = databases.Length,
        total_count = totalCount,
        total_pages = totalPages
      }
    };
    return JsonSerializer.Serialize(response, _serializerOptions);
  }

  /// <summary>Creates a sample D1Database for testing.</summary>
  /// <param name="uuid">The database UUID.</param>
  /// <param name="name">The database name.</param>
  /// <returns>A D1Database instance.</returns>
  private static D1Database CreateTestDatabase(string uuid, string name) =>
    new(
      Uuid: uuid,
      Name: name,
      CreatedAt: DateTimeOffset.Parse("2024-01-01T00:00:00Z"),
      FileSize: 1024,
      NumTables: 5,
      Version: "production",
      ReadReplication: new D1ReadReplication("auto"),
      RunningInRegion: "WEUR"
    );

  #endregion


  #region Database Operations - ListAsync

  /// <summary>Verifies that ListAsync sends a correctly formatted GET request with no filters.</summary>
  [Fact]
  public async Task ListAsync_WithNoFilters_SendsCorrectRequest()
  {
    // Arrange
    var databases = new[] { CreateTestDatabase("db-1", "My Database") };
    var response  = CreatePaginatedDatabaseResponse(databases, 1, 20, 1);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.ListAsync();

    // Assert
    result.Items.Should().HaveCount(1);
    result.Items[0].Uuid.Should().Be("db-1");
    result.Items[0].Name.Should().Be("My Database");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/d1/database");
  }

  /// <summary>Verifies that ListAsync includes page filter in query string.</summary>
  [Fact]
  public async Task ListAsync_WithPageFilter_SendsCorrectRequest()
  {
    // Arrange
    var response = CreatePaginatedDatabaseResponse(Array.Empty<D1Database>(), 2, 20, 40);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(new ListD1DatabasesFilters(Page: 2));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("page=2");
  }

  /// <summary>Verifies that ListAsync includes per_page filter in query string.</summary>
  [Fact]
  public async Task ListAsync_WithPerPageFilter_SendsCorrectRequest()
  {
    // Arrange
    var response = CreatePaginatedDatabaseResponse(Array.Empty<D1Database>(), 1, 50, 0);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(new ListD1DatabasesFilters(PerPage: 50));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("per_page=50");
  }

  /// <summary>Verifies that ListAsync includes name filter in query string.</summary>
  [Fact]
  public async Task ListAsync_WithNameFilter_SendsCorrectRequest()
  {
    // Arrange
    var response = CreatePaginatedDatabaseResponse(Array.Empty<D1Database>(), 1, 20, 0);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(new ListD1DatabasesFilters(Name: "my-database"));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("name=my-database");
  }

  /// <summary>Verifies that ListAsync includes all filters in query string.</summary>
  [Fact]
  public async Task ListAsync_WithAllFilters_SendsCorrectRequest()
  {
    // Arrange
    var filters = new ListD1DatabasesFilters(
      Name: "test-db",
      Page: 2,
      PerPage: 50
    );
    var response = CreatePaginatedDatabaseResponse(Array.Empty<D1Database>(), 2, 50, 100);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.ToString();
    uri.Should().Contain("name=test-db");
    uri.Should().Contain("page=2");
    uri.Should().Contain("per_page=50");
  }

  #endregion


  #region Database Operations - ListAllAsync

  /// <summary>Verifies that ListAllAsync handles pagination correctly.</summary>
  /// <remarks>
  ///   The D1 API returns TotalPages=0, so ListAllAsync uses the "is page full?" pattern to determine
  ///   if there are more pages. This test verifies that pattern works correctly.
  /// </remarks>
  [Fact]
  public async Task ListAllAsync_ShouldHandlePaginationCorrectly()
  {
    // Arrange
    var db1 = CreateTestDatabase("db-1", "Database 1");
    var db2 = CreateTestDatabase("db-2", "Database 2");

    // First page response - page is "full" (1 item with perPage=1), so more pages exist.
    var responsePage1 = CreatePaginatedDatabaseResponse(new[] { db1 }, 1, 1, 2);

    // Second page response - page is NOT full (1 item with perPage=1 but it's the last page).
    // Note: The implementation checks Items.Count >= perPage, so we need page 2 to have fewer items
    // to signal "no more pages". In this case, we return 1 item but the next request would return 0.
    var responsePage2 = CreatePaginatedDatabaseResponse(new[] { db2 }, 2, 1, 2);

    // Empty third page to signal end of pagination.
    var responsePage3 = CreatePaginatedDatabaseResponse(Array.Empty<D1Database>(), 3, 1, 2);

    var capturedRequests = new List<HttpRequestMessage>();
    var mockHandler      = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
               .Returns((HttpRequestMessage req, CancellationToken _) =>
               {
                 if (req.RequestUri!.ToString().Contains("page=3"))
                   return Task.FromResult(
                     new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage3) });

                 if (req.RequestUri!.ToString().Contains("page=2"))
                   return Task.FromResult(
                     new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage2) });

                 return Task.FromResult(
                   new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage1) });
               });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = TestAccountId });
    var sut        = new D1Api(httpClient, options, _loggerFactory);

    // Act - Must pass PerPage=1 to match the mock data (default is 100).
    var allDatabases = new List<D1Database>();
    await foreach (var db in sut.ListAllAsync(new ListD1DatabasesFilters(PerPage: 1)))
      allDatabases.Add(db);

    // Assert - 3 requests: page 1 (full), page 2 (full), page 3 (empty = stop).
    capturedRequests.Should().HaveCount(3);
    capturedRequests[0].RequestUri!.Query.Should().Contain("page=1");
    capturedRequests[1].RequestUri!.Query.Should().Contain("page=2");
    capturedRequests[2].RequestUri!.Query.Should().Contain("page=3");
    allDatabases.Should().HaveCount(2);
    allDatabases.Select(d => d.Uuid).Should().ContainInOrder("db-1", "db-2");
  }

  #endregion


  #region Database Operations - CreateAsync

  /// <summary>Verifies that CreateAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateAsync_SendsCorrectRequest()
  {
    // Arrange
    var name           = "My New Database";
    var expectedResult = CreateTestDatabase("db-new-id", name);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.CreateAsync(name);

    // Assert
    result.Name.Should().Be(name);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/d1/database");

    // Verify JSON body contains the name.
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("name").GetString().Should().Be(name);
  }

  /// <summary>Verifies that CreateAsync includes location hint when provided.</summary>
  [Fact]
  public async Task CreateAsync_WithLocationHint_IncludesLocationHint()
  {
    // Arrange
    var name = "My Database";
    var expectedResult = CreateTestDatabase("db-id", name);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    await sut.CreateAsync(name, primaryLocationHint: R2LocationHint.WestEurope);

    // Assert
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("primary_location_hint").GetString().Should().Be("weur");
  }

  /// <summary>Verifies that CreateAsync includes jurisdiction when provided.</summary>
  [Fact]
  public async Task CreateAsync_WithJurisdiction_IncludesJurisdiction()
  {
    // Arrange
    var name = "EU Database";
    var expectedResult = CreateTestDatabase("db-id", name);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    await sut.CreateAsync(name, jurisdiction: D1Jurisdiction.EuropeanUnion);

    // Assert
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("jurisdiction").GetString().Should().Be("eu");
  }

  #endregion


  #region Database Operations - GetAsync

  /// <summary>Verifies that GetAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetAsync_SendsCorrectRequest()
  {
    // Arrange
    var databaseId     = "db-123";
    var expectedResult = CreateTestDatabase(databaseId, "My Database");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetAsync(databaseId);

    // Assert
    result.Uuid.Should().Be(databaseId);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/d1/database/{databaseId}");
  }

  /// <summary>Verifies that GetAsync URL-encodes special characters in database IDs.</summary>
  [Fact]
  public async Task GetAsync_UrlEncodesDatabaseId()
  {
    // Arrange
    var databaseId      = "db id+special";
    var expectedResult  = CreateTestDatabase(databaseId, "My Database");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.GetAsync(databaseId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.OriginalString.Should().Contain("db%20id%2Bspecial");
  }

  #endregion


  #region Database Operations - UpdateAsync

  /// <summary>Verifies that UpdateAsync sends a correctly formatted PATCH request.</summary>
  [Fact]
  public async Task UpdateAsync_SendsCorrectRequest()
  {
    // Arrange
    var databaseId     = "db-123";
    var expectedResult = CreateTestDatabase(databaseId, "My Database");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var options = new UpdateD1DatabaseOptions(
      ReadReplication: new D1ReadReplication("auto")
    );

    // Act
    var result = await sut.UpdateAsync(databaseId, options);

    // Assert
    result.Uuid.Should().Be(databaseId);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/d1/database/{databaseId}");

    // Verify JSON body contains read_replication.
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("read_replication").GetProperty("mode").GetString().Should().Be("auto");
  }

  #endregion


  #region Database Operations - DeleteAsync

  /// <summary>Verifies that DeleteAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DeleteAsync_SendsCorrectRequest()
  {
    // Arrange
    var databaseId      = "db-to-delete";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.DeleteAsync(databaseId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/d1/database/{databaseId}");
  }

  #endregion


  #region Query Operations - QueryAsync

  /// <summary>Verifies that QueryAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task QueryAsync_SendsCorrectRequest()
  {
    // Arrange
    var databaseId = "db-123";
    var sql        = "SELECT * FROM users WHERE id = ?";
    var queryResult = new D1QueryResult(
      Meta: new D1QueryMeta(false, 0, 1.5, 0, 10, 0),
      Results: new List<JsonElement>(),
      Success: true
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(new[] { queryResult });

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.QueryAsync(databaseId, sql, new object?[] { "user-1" });

    // Assert
    result.Should().HaveCount(1);
    result[0].Success.Should().BeTrue();
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/d1/database/{databaseId}/query");

    // Verify JSON body contains sql and params.
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("sql").GetString().Should().Be(sql);
    doc.RootElement.GetProperty("params").GetArrayLength().Should().Be(1);
  }

  /// <summary>Verifies that QueryAsync works without parameters.</summary>
  [Fact]
  public async Task QueryAsync_WithoutParams_SendsCorrectRequest()
  {
    // Arrange
    var databaseId = "db-123";
    var sql        = "SELECT * FROM users";
    var queryResult = new D1QueryResult(
      Meta: new D1QueryMeta(false, 0, 1.0, 0, 5, 0),
      Results: new List<JsonElement>(),
      Success: true
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(new[] { queryResult });

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    await sut.QueryAsync(databaseId, sql);

    // Assert
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("sql").GetString().Should().Be(sql);
    // params should be null or not present.
    doc.RootElement.TryGetProperty("params", out var paramsElement).Should().BeFalse();
  }

  #endregion


  #region Query Operations - QueryRawAsync

  /// <summary>Verifies that QueryRawAsync sends a correctly formatted POST request to the raw endpoint.</summary>
  [Fact]
  public async Task QueryRawAsync_SendsCorrectRequest()
  {
    // Arrange
    var databaseId = "db-123";
    var sql        = "SELECT id, name FROM users";
    var rawResult = new D1RawQueryResult(
      Meta: new D1QueryMeta(false, 0, 0.8, 0, 3, 0),
      Results: new D1RawQueryResultSet(
        Columns: new[] { "id", "name" },
        Rows: new List<IReadOnlyList<JsonElement>>()
      ),
      Success: true
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(new[] { rawResult });

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.QueryRawAsync(databaseId, sql);

    // Assert
    result.Should().HaveCount(1);
    result[0].Success.Should().BeTrue();
    result[0].Results.Columns.Should().ContainInOrder("id", "name");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/d1/database/{databaseId}/raw");
  }

  #endregion


  #region Export Operations - StartExportAsync

  /// <summary>Verifies that StartExportAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task StartExportAsync_SendsCorrectRequest()
  {
    // Arrange
    var databaseId = "db-123";
    var exportResponse = new D1ExportResponse(
      AtBookmark: "bookmark-123",
      Status: "active",
      Result: null,
      Error: null,
      Type: "export",
      Success: true
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(exportResponse);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.StartExportAsync(databaseId);

    // Assert
    result.AtBookmark.Should().Be("bookmark-123");
    result.Status.Should().Be("active");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/d1/database/{databaseId}/export");

    // Verify JSON body contains output_format.
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("output_format").GetString().Should().Be("polling");
  }

  /// <summary>Verifies that StartExportAsync includes dump options when provided.</summary>
  [Fact]
  public async Task StartExportAsync_WithDumpOptions_IncludesDumpOptions()
  {
    // Arrange
    var databaseId = "db-123";
    var exportResponse = new D1ExportResponse(
      AtBookmark: "bookmark-123",
      Status: "active",
      Success: true
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(exportResponse);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var dumpOptions = new D1ExportDumpOptions(
      NoData: true,
      Tables: new[] { "users", "orders" }
    );

    // Act
    await sut.StartExportAsync(databaseId, dumpOptions);

    // Assert
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    var dumpOptionsElement = doc.RootElement.GetProperty("dump_options");
    dumpOptionsElement.GetProperty("no_data").GetBoolean().Should().BeTrue();
    dumpOptionsElement.GetProperty("tables").GetArrayLength().Should().Be(2);
  }

  #endregion


  #region Export Operations - PollExportAsync

  /// <summary>Verifies that PollExportAsync sends a correctly formatted POST request with bookmark.</summary>
  [Fact]
  public async Task PollExportAsync_SendsCorrectRequest()
  {
    // Arrange
    var databaseId = "db-123";
    var bookmark   = "bookmark-abc";
    var exportResponse = new D1ExportResponse(
      AtBookmark: bookmark,
      Status: "complete",
      Result: new D1ExportResultDetails(
        Filename: "export.sql",
        SignedUrl: "https://example.com/signed-url"
      ),
      Success: true
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(exportResponse);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.PollExportAsync(databaseId, bookmark);

    // Assert
    result.Status.Should().Be("complete");
    result.Result!.SignedUrl.Should().Be("https://example.com/signed-url");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);

    // Verify JSON body contains current_bookmark.
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("current_bookmark").GetString().Should().Be(bookmark);
  }

  #endregion


  #region Import Operations - StartImportAsync

  /// <summary>Verifies that StartImportAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task StartImportAsync_SendsCorrectRequest()
  {
    // Arrange
    var databaseId = "db-123";
    var etag       = "abc123def456";
    var importResponse = new D1ImportResponse(
      Filename: "import-file.sql",
      UploadUrl: "https://example.com/upload-url",
      Success: true
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(importResponse);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.StartImportAsync(databaseId, etag);

    // Assert
    result.Filename.Should().Be("import-file.sql");
    result.UploadUrl.Should().Be("https://example.com/upload-url");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/d1/database/{databaseId}/import");

    // Verify JSON body contains action and etag.
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("action").GetString().Should().Be("init");
    doc.RootElement.GetProperty("etag").GetString().Should().Be(etag);
  }

  #endregion


  #region Import Operations - CompleteImportAsync

  /// <summary>Verifies that CompleteImportAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CompleteImportAsync_SendsCorrectRequest()
  {
    // Arrange
    var databaseId = "db-123";
    var etag       = "abc123def456";
    var filename   = "import-file.sql";
    var importResponse = new D1ImportResponse(
      AtBookmark: "bookmark-xyz",
      Status: "active",
      Success: true
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(importResponse);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.CompleteImportAsync(databaseId, etag, filename);

    // Assert
    result.AtBookmark.Should().Be("bookmark-xyz");
    result.Status.Should().Be("active");

    // Verify JSON body contains action, etag, and filename.
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("action").GetString().Should().Be("ingest");
    doc.RootElement.GetProperty("etag").GetString().Should().Be(etag);
    doc.RootElement.GetProperty("filename").GetString().Should().Be(filename);
  }

  #endregion


  #region Import Operations - PollImportAsync

  /// <summary>Verifies that PollImportAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task PollImportAsync_SendsCorrectRequest()
  {
    // Arrange
    var databaseId = "db-123";
    var bookmark   = "bookmark-xyz";
    var importResponse = new D1ImportResponse(
      AtBookmark: bookmark,
      Status: "complete",
      Result: new D1ImportResultDetails(
        NumQueries: 100,
        Meta: new D1QueryMeta(true, 50, 500.0, 1, 200, 150)
      ),
      Success: true
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(importResponse);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.PollImportAsync(databaseId, bookmark);

    // Assert
    result.Status.Should().Be("complete");
    result.Result!.NumQueries.Should().Be(100);

    // Verify JSON body contains action and current_bookmark.
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("action").GetString().Should().Be("poll");
    doc.RootElement.GetProperty("current_bookmark").GetString().Should().Be(bookmark);
  }

  #endregion


  #region Error Handling

  /// <summary>Verifies that API errors are properly propagated as CloudflareApiException.</summary>
  [Fact]
  public async Task CreateAsync_WhenApiReturnsError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(7003, "Invalid database name");
    var sut           = CreateSut(errorResponse);

    // Act
    var action = async () => await sut.CreateAsync("Invalid Name!");

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareApiException>();
    ex.Which.Message.Should().Contain("7003");
    ex.Which.Errors.Should().ContainSingle().Which.Code.Should().Be(7003);
  }

  /// <summary>Verifies that GetAsync throws on non-existent database.</summary>
  [Fact]
  public async Task GetAsync_WhenDatabaseNotFound_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(7000, "Database not found");
    var sut           = CreateSut(errorResponse);

    // Act
    var action = async () => await sut.GetAsync("non-existent-db");

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareApiException>();
    ex.Which.Errors.Should().ContainSingle().Which.Code.Should().Be(7000);
  }

  /// <summary>Verifies that DeleteAsync propagates API errors correctly.</summary>
  [Fact]
  public async Task DeleteAsync_WhenApiReturnsError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(7000, "Database not found");
    var sut           = CreateSut(errorResponse);

    // Act
    var action = async () => await sut.DeleteAsync("non-existent-db");

    // Assert
    await action.Should().ThrowAsync<CloudflareApiException>();
  }

  /// <summary>Verifies that QueryAsync propagates API errors correctly.</summary>
  [Fact]
  public async Task QueryAsync_WhenApiReturnsError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(7500, "SQL syntax error");
    var sut           = CreateSut(errorResponse);

    // Act
    var action = async () => await sut.QueryAsync("db-123", "SELECT * FORM users");

    // Assert
    await action.Should().ThrowAsync<CloudflareApiException>();
  }

  #endregion


  #region Model Serialization Tests

  /// <summary>Verifies that D1Jurisdiction serializes correctly.</summary>
  [Fact]
  public void D1Jurisdiction_SerializesCorrectly()
  {
    // Arrange
    var jurisdiction = D1Jurisdiction.EuropeanUnion;

    // Act
    var json = JsonSerializer.Serialize(jurisdiction);

    // Assert
    json.Should().Be("\"eu\"");
  }

  /// <summary>Verifies that D1Jurisdiction deserializes correctly.</summary>
  [Fact]
  public void D1Jurisdiction_DeserializesCorrectly()
  {
    // Arrange
    var json = "\"fedramp\"";

    // Act
    var jurisdiction = JsonSerializer.Deserialize<D1Jurisdiction>(json);

    // Assert
    jurisdiction.Should().Be(D1Jurisdiction.FedRamp);
  }

  /// <summary>Verifies that D1Jurisdiction handles custom values.</summary>
  [Fact]
  public void D1Jurisdiction_CustomValue_WorksCorrectly()
  {
    // Arrange
    var customJurisdiction = new D1Jurisdiction("custom-region");

    // Act
    var json         = JsonSerializer.Serialize(customJurisdiction);
    var deserialized = JsonSerializer.Deserialize<D1Jurisdiction>(json);

    // Assert
    json.Should().Be("\"custom-region\"");
    deserialized.Value.Should().Be("custom-region");
  }

  /// <summary>Verifies that D1Database deserializes correctly from API response format.</summary>
  [Fact]
  public void D1Database_DeserializesFromApiResponse()
  {
    // Arrange
    var json = """
    {
      "uuid": "test-uuid-123",
      "name": "my-database",
      "created_at": "2024-06-15T10:30:00Z",
      "file_size": 2048,
      "num_tables": 3,
      "version": "production",
      "read_replication": {
        "mode": "auto"
      },
      "running_in_region": "ENAM"
    }
    """;

    // Act
    var database = JsonSerializer.Deserialize<D1Database>(json, _serializerOptions);

    // Assert
    database.Should().NotBeNull();
    database!.Uuid.Should().Be("test-uuid-123");
    database.Name.Should().Be("my-database");
    database.FileSize.Should().Be(2048);
    database.NumTables.Should().Be(3);
    database.Version.Should().Be("production");
    database.ReadReplication!.Mode.Should().Be("auto");
    database.RunningInRegion.Should().Be("ENAM");
  }

  /// <summary>Verifies that D1QueryMeta deserializes correctly from API response format.</summary>
  [Fact]
  public void D1QueryMeta_DeserializesFromApiResponse()
  {
    // Arrange
    var json = """
    {
      "changed_db": true,
      "changes": 5,
      "duration": 2.5,
      "last_row_id": 42,
      "rows_read": 100,
      "rows_written": 5,
      "served_by": "v1.2.3",
      "served_by_primary": true,
      "served_by_region": "WEUR",
      "size_after": 4096,
      "timings": {
        "sql_duration_ms": 2.3
      },
      "total_attempts": 1
    }
    """;

    // Act
    var meta = JsonSerializer.Deserialize<D1QueryMeta>(json, _serializerOptions);

    // Assert
    meta.Should().NotBeNull();
    meta!.ChangedDb.Should().BeTrue();
    meta.Changes.Should().Be(5);
    meta.Duration.Should().Be(2.5);
    meta.LastRowId.Should().Be(42);
    meta.RowsRead.Should().Be(100);
    meta.RowsWritten.Should().Be(5);
    meta.ServedBy.Should().Be("v1.2.3");
    meta.ServedByPrimary.Should().BeTrue();
    meta.ServedByRegion.Should().Be("WEUR");
    meta.SizeAfter.Should().Be(4096);
    meta.Timings!.SqlDurationMs.Should().Be(2.3);
    meta.TotalAttempts.Should().Be(1);
  }

  #endregion
}
