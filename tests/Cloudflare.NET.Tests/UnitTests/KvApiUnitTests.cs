namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Accounts.Kv;
using Accounts.Kv.Models;
using Cloudflare.NET.Core.Exceptions;
using Cloudflare.NET.Core.Models;
using Cloudflare.NET.Security.Firewall.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>Contains unit tests for the <see cref="KvApi" /> class.</summary>
/// <remarks>
///   This test class covers all Workers KV operations including:
///   <list type="bullet">
///     <item><description>Namespace operations (List, Create, Get, Rename, Delete)</description></item>
///     <item><description>Key operations (List keys, List all keys)</description></item>
///     <item><description>Value operations (Get, Write, Delete)</description></item>
///     <item><description>Bulk operations (BulkWrite, BulkDelete, BulkGet)</description></item>
///     <item><description>Error handling and edge cases</description></item>
///   </list>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class KvApiUnitTests
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

  /// <summary>Initializes a new instance of the <see cref="KvApiUnitTests" /> class.</summary>
  /// <param name="output">The xUnit test output helper.</param>
  public KvApiUnitTests(ITestOutputHelper output)
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
  /// <returns>A configured <see cref="KvApi" /> instance.</returns>
  private KvApi CreateSut(
    string                                         responseContent,
    HttpStatusCode                                 statusCode = HttpStatusCode.OK,
    Action<HttpRequestMessage, CancellationToken>? callback   = null)
  {
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseContent, statusCode, callback);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options     = Options.Create(new CloudflareApiOptions { AccountId = TestAccountId });

    return new KvApi(httpClient, options, _loggerFactory);
  }

  /// <summary>Creates the system under test with a mocked HTTP handler that returns response headers.</summary>
  /// <param name="responseContent">The content to return.</param>
  /// <param name="statusCode">The HTTP status code to return.</param>
  /// <param name="headers">Headers to include in the response.</param>
  /// <param name="callback">Optional callback to capture the request.</param>
  /// <returns>A configured <see cref="KvApi" /> instance.</returns>
  private KvApi CreateSutWithHeaders(
    string                                         responseContent,
    HttpStatusCode                                 statusCode,
    IEnumerable<KeyValuePair<string, string>>      headers,
    Action<HttpRequestMessage, CancellationToken>? callback = null)
  {
    var mockHandler = new Mock<HttpMessageHandler>();
    var response = new HttpResponseMessage
    {
      StatusCode = statusCode,
      Content    = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/octet-stream")
    };

    // Add custom headers to the response.
    foreach (var header in headers)
      response.Headers.TryAddWithoutValidation(header.Key, header.Value);

    var setup = mockHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>()
      );

    if (callback is not null)
      setup.Callback(callback);

    setup.ReturnsAsync(response);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = TestAccountId });

    return new KvApi(httpClient, options, _loggerFactory);
  }

  /// <summary>Creates a paginated response for namespace listings.</summary>
  /// <param name="namespaces">The namespaces to include.</param>
  /// <param name="page">Current page number.</param>
  /// <param name="perPage">Items per page.</param>
  /// <param name="totalCount">Total number of items.</param>
  /// <returns>JSON string representing the paginated response.</returns>
  private string CreatePaginatedNamespaceResponse(KvNamespace[] namespaces, int page, int perPage, int totalCount)
  {
    var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / perPage);
    var response = new
    {
      success     = true,
      errors      = Array.Empty<object>(),
      messages    = Array.Empty<object>(),
      result      = namespaces,
      result_info = new
      {
        page,
        per_page    = perPage,
        count       = namespaces.Length,
        total_count = totalCount,
        total_pages = totalPages
      }
    };
    return JsonSerializer.Serialize(response, _serializerOptions);
  }

  /// <summary>Creates a cursor-paginated response for key listings.</summary>
  /// <param name="keys">The keys to include.</param>
  /// <param name="perPage">Items per page.</param>
  /// <param name="cursor">Optional cursor for next page.</param>
  /// <returns>JSON string representing the cursor-paginated response.</returns>
  private string CreateCursorPaginatedKeyResponse(KvKey[] keys, int perPage, string? cursor = null)
  {
    var response = new
    {
      success           = true,
      errors            = Array.Empty<object>(),
      messages          = Array.Empty<object>(),
      result            = keys,
      result_info       = new { count = keys.Length, per_page = perPage, cursor = (string?)null },
      cursor_result_info = new { count = keys.Length, per_page = perPage, cursor }
    };
    return JsonSerializer.Serialize(response, _serializerOptions);
  }

  #endregion


  #region Namespace Operations - ListAsync

  /// <summary>Verifies that ListAsync sends a correctly formatted GET request with no filters.</summary>
  [Fact]
  public async Task ListAsync_WithNoFilters_SendsCorrectRequest()
  {
    // Arrange
    var namespaces = new[] { new KvNamespace("ns-1", "My Namespace", true) };
    var response = CreatePaginatedNamespaceResponse(namespaces, 1, 20, 1);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.ListAsync();

    // Assert
    result.Items.Should().HaveCount(1);
    result.Items[0].Id.Should().Be("ns-1");
    result.Items[0].Title.Should().Be("My Namespace");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces");
  }

  /// <summary>Verifies that ListAsync includes page filter in query string.</summary>
  [Fact]
  public async Task ListAsync_WithPageFilter_SendsCorrectRequest()
  {
    // Arrange
    var response = CreatePaginatedNamespaceResponse(Array.Empty<KvNamespace>(), 2, 20, 40);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(new ListKvNamespacesFilters(Page: 2));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("page=2");
  }

  /// <summary>Verifies that ListAsync includes per_page filter in query string.</summary>
  [Fact]
  public async Task ListAsync_WithPerPageFilter_SendsCorrectRequest()
  {
    // Arrange
    var response = CreatePaginatedNamespaceResponse(Array.Empty<KvNamespace>(), 1, 50, 0);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(new ListKvNamespacesFilters(PerPage: 50));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("per_page=50");
  }

  /// <summary>Verifies that ListAsync includes order filter in query string.</summary>
  [Fact]
  public async Task ListAsync_WithOrderFilter_SendsCorrectRequest()
  {
    // Arrange
    var response = CreatePaginatedNamespaceResponse(Array.Empty<KvNamespace>(), 1, 20, 0);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(new ListKvNamespacesFilters(Order: KvNamespaceOrderField.Title));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("order=title");
  }

  /// <summary>Verifies that ListAsync includes direction filter in query string.</summary>
  [Fact]
  public async Task ListAsync_WithDirectionFilter_SendsCorrectRequest()
  {
    // Arrange
    var response = CreatePaginatedNamespaceResponse(Array.Empty<KvNamespace>(), 1, 20, 0);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(new ListKvNamespacesFilters(Direction: ListOrderDirection.Descending));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("direction=desc");
  }

  /// <summary>Verifies that ListAsync includes all filters in query string.</summary>
  [Fact]
  public async Task ListAsync_WithAllFilters_SendsCorrectRequest()
  {
    // Arrange
    var filters = new ListKvNamespacesFilters(
      Page: 2,
      PerPage: 50,
      Order: KvNamespaceOrderField.Id,
      Direction: ListOrderDirection.Ascending
    );
    var response = CreatePaginatedNamespaceResponse(Array.Empty<KvNamespace>(), 2, 50, 100);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.ToString();
    uri.Should().Contain("page=2");
    uri.Should().Contain("per_page=50");
    uri.Should().Contain("order=id");
    uri.Should().Contain("direction=asc");
  }

  #endregion


  #region Namespace Operations - ListAllAsync

  /// <summary>Verifies that ListAllAsync handles pagination correctly.</summary>
  [Fact]
  public async Task ListAllAsync_ShouldHandlePaginationCorrectly()
  {
    // Arrange
    var ns1 = new KvNamespace("ns-1", "Namespace 1", true);
    var ns2 = new KvNamespace("ns-2", "Namespace 2", true);

    // First page response.
    var responsePage1 = CreatePaginatedNamespaceResponse(new[] { ns1 }, 1, 1, 2);

    // Second page response.
    var responsePage2 = CreatePaginatedNamespaceResponse(new[] { ns2 }, 2, 1, 2);

    var capturedRequests = new List<HttpRequestMessage>();
    var mockHandler      = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
               .Returns((HttpRequestMessage req, CancellationToken _) =>
               {
                 if (req.RequestUri!.ToString().Contains("page=2"))
                   return Task.FromResult(
                     new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage2) });

                 return Task.FromResult(
                   new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage1) });
               });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = TestAccountId });
    var sut        = new KvApi(httpClient, options, _loggerFactory);

    // Act
    var allNamespaces = new List<KvNamespace>();
    await foreach (var ns in sut.ListAllAsync())
      allNamespaces.Add(ns);

    // Assert
    capturedRequests.Should().HaveCount(2);
    capturedRequests[0].RequestUri!.Query.Should().Contain("page=1");
    capturedRequests[1].RequestUri!.Query.Should().Contain("page=2");
    allNamespaces.Should().HaveCount(2);
    allNamespaces.Select(n => n.Id).Should().ContainInOrder("ns-1", "ns-2");
  }

  /// <summary>Verifies that ListAllAsync preserves filters across pagination requests.</summary>
  [Fact]
  public async Task ListAllAsync_WithFilters_ShouldPreserveFiltersAcrossPagination()
  {
    // Arrange
    var ns = new KvNamespace("ns-1", "Namespace 1", true);
    var response = CreatePaginatedNamespaceResponse(new[] { ns }, 1, 50, 1);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    var allNamespaces = new List<KvNamespace>();
    await foreach (var n in sut.ListAllAsync(new ListKvNamespacesFilters(
      PerPage: 50,
      Order: KvNamespaceOrderField.Title,
      Direction: ListOrderDirection.Descending)))
      allNamespaces.Add(n);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.ToString();
    uri.Should().Contain("per_page=50");
    uri.Should().Contain("order=title");
    uri.Should().Contain("direction=desc");
  }

  #endregion


  #region Namespace Operations - CreateAsync

  /// <summary>Verifies that CreateAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateAsync_SendsCorrectRequest()
  {
    // Arrange
    var title          = "My New Namespace";
    var expectedResult = new KvNamespace("ns-new-id", title, true);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.CreateAsync(title);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces");

    // Verify JSON body contains the title
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("title").GetString().Should().Be(title);
  }

  #endregion


  #region Namespace Operations - GetAsync

  /// <summary>Verifies that GetAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetAsync_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId    = "ns-123";
    var expectedResult = new KvNamespace(namespaceId, "My Namespace", true);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetAsync(namespaceId);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces/{namespaceId}");
  }

  /// <summary>Verifies that GetAsync URL-encodes special characters in namespace IDs.</summary>
  [Fact]
  public async Task GetAsync_UrlEncodesNamespaceId()
  {
    // Arrange
    var namespaceId     = "ns id+special";
    var expectedResult  = new KvNamespace(namespaceId, "My Namespace", true);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetAsync(namespaceId);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    // Verify the namespace ID is URL-encoded using OriginalString to avoid automatic decoding.
    capturedRequest!.RequestUri!.OriginalString.Should().Contain("ns%20id%2Bspecial");
  }

  #endregion


  #region Namespace Operations - RenameAsync

  /// <summary>Verifies that RenameAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task RenameAsync_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId    = "ns-123";
    var newTitle       = "Renamed Namespace";
    var expectedResult = new KvNamespace(namespaceId, newTitle, true);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.RenameAsync(namespaceId, newTitle);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces/{namespaceId}");

    // Verify JSON body contains the new title.
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("title").GetString().Should().Be(newTitle);
  }

  #endregion


  #region Namespace Operations - DeleteAsync

  /// <summary>Verifies that DeleteAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DeleteAsync_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId     = "ns-to-delete";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.DeleteAsync(namespaceId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces/{namespaceId}");
  }

  #endregion


  #region Key Operations - ListKeysAsync

  /// <summary>Verifies that ListKeysAsync sends a correctly formatted GET request with no filters.</summary>
  [Fact]
  public async Task ListKeysAsync_WithNoFilters_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId = "ns-123";
    var keys = new[] { new KvKey("key1", 1735689600, null) };
    var response = CreateCursorPaginatedKeyResponse(keys, 1000);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.ListKeysAsync(namespaceId);

    // Assert
    result.Items.Should().HaveCount(1);
    result.Items[0].Name.Should().Be("key1");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces/{namespaceId}/keys");
  }

  /// <summary>Verifies that ListKeysAsync includes prefix filter in query string.</summary>
  [Fact]
  public async Task ListKeysAsync_WithPrefixFilter_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId = "ns-123";
    var response = CreateCursorPaginatedKeyResponse(Array.Empty<KvKey>(), 1000);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListKeysAsync(namespaceId, new ListKvKeysFilters(Prefix: "users/"));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("prefix=users%2F");
  }

  /// <summary>Verifies that ListKeysAsync includes limit filter in query string.</summary>
  [Fact]
  public async Task ListKeysAsync_WithLimitFilter_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId = "ns-123";
    var response = CreateCursorPaginatedKeyResponse(Array.Empty<KvKey>(), 100);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListKeysAsync(namespaceId, new ListKvKeysFilters(Limit: 100));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("limit=100");
  }

  /// <summary>Verifies that ListKeysAsync includes cursor filter in query string.</summary>
  [Fact]
  public async Task ListKeysAsync_WithCursorFilter_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId = "ns-123";
    var cursor = "abc123def";
    var response = CreateCursorPaginatedKeyResponse(Array.Empty<KvKey>(), 1000);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListKeysAsync(namespaceId, new ListKvKeysFilters(Cursor: cursor));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain($"cursor={cursor}");
  }

  /// <summary>Verifies that ListKeysAsync includes all filters in query string.</summary>
  [Fact]
  public async Task ListKeysAsync_WithAllFilters_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId = "ns-123";
    var filters = new ListKvKeysFilters(Prefix: "config/", Limit: 50, Cursor: "cursor123");
    var response = CreateCursorPaginatedKeyResponse(Array.Empty<KvKey>(), 50);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListKeysAsync(namespaceId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.ToString();
    uri.Should().Contain("prefix=config%2F");
    uri.Should().Contain("limit=50");
    uri.Should().Contain("cursor=cursor123");
  }

  /// <summary>Verifies that ListKeysAsync URL-encodes special characters in prefix.</summary>
  [Fact]
  public async Task ListKeysAsync_WithSpecialCharactersInPrefix_ShouldUrlEncodeValues()
  {
    // Arrange
    var namespaceId = "ns-123";
    var response = CreateCursorPaginatedKeyResponse(Array.Empty<KvKey>(), 1000);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListKeysAsync(namespaceId, new ListKvKeysFilters(Prefix: "data+backup/"));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.OriginalString.Should().Contain("prefix=data%2Bbackup%2F");
  }

  #endregion


  #region Key Operations - ListAllKeysAsync

  /// <summary>Verifies that ListAllKeysAsync handles cursor pagination correctly.</summary>
  [Fact]
  public async Task ListAllKeysAsync_ShouldHandleCursorPaginationCorrectly()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key1 = new KvKey("key1", null, null);
    var key2 = new KvKey("key2", null, null);
    var cursor = "next_page_cursor";

    // First page response with a cursor.
    var responsePage1 = CreateCursorPaginatedKeyResponse(new[] { key1 }, 1, cursor);

    // Second page response without a cursor (end of pagination).
    var responsePage2 = CreateCursorPaginatedKeyResponse(new[] { key2 }, 1, null);

    var capturedRequests = new List<HttpRequestMessage>();
    var mockHandler      = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequests.Add(req))
               .Returns((HttpRequestMessage req, CancellationToken _) =>
               {
                 if (req.RequestUri!.ToString().Contains(cursor))
                   return Task.FromResult(
                     new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage2) });

                 return Task.FromResult(
                   new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage1) });
               });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = TestAccountId });
    var sut        = new KvApi(httpClient, options, _loggerFactory);

    // Act
    var allKeys = new List<KvKey>();
    await foreach (var key in sut.ListAllKeysAsync(namespaceId))
      allKeys.Add(key);

    // Assert
    capturedRequests.Should().HaveCount(2);
    capturedRequests[0].RequestUri!.Query.Should().NotContain("cursor");
    capturedRequests[1].RequestUri!.Query.Should().Contain($"cursor={cursor}");
    allKeys.Should().HaveCount(2);
    allKeys.Select(k => k.Name).Should().ContainInOrder("key1", "key2");
  }

  /// <summary>Verifies that ListAllKeysAsync preserves prefix filter across pagination.</summary>
  [Fact]
  public async Task ListAllKeysAsync_WithPrefix_ShouldPreservePrefixAcrossPagination()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = new KvKey("users/user1", null, null);
    var response = CreateCursorPaginatedKeyResponse(new[] { key }, 1000, null);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(response, callback: (req, _) => capturedRequest = req);

    // Act
    var allKeys = new List<KvKey>();
    await foreach (var k in sut.ListAllKeysAsync(namespaceId, "users/"))
      allKeys.Add(k);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("prefix=users%2F");
  }

  #endregion


  #region Value Operations - GetValueAsync

  /// <summary>Verifies that GetValueAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetValueAsync_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my-key";
    var value = "my-value";

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSutWithHeaders(value, HttpStatusCode.OK, [], (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetValueAsync(namespaceId, key);

    // Assert
    result.Should().Be(value);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces/{namespaceId}/values/{key}");
  }

  /// <summary>Verifies that GetValueAsync returns null for non-existent keys.</summary>
  [Fact]
  public async Task GetValueAsync_WhenKeyNotFound_ReturnsNull()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "non-existent-key";
    var sut = CreateSutWithHeaders("", HttpStatusCode.NotFound, []);

    // Act
    var result = await sut.GetValueAsync(namespaceId, key);

    // Assert
    result.Should().BeNull();
  }

  /// <summary>Verifies that GetValueAsync URL-encodes special characters in key names.</summary>
  [Fact]
  public async Task GetValueAsync_UrlEncodesKeyName()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my key+special/chars";
    var value = "value";

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSutWithHeaders(value, HttpStatusCode.OK, [], (req, _) => capturedRequest = req);

    // Act
    await sut.GetValueAsync(namespaceId, key);

    // Assert
    capturedRequest.Should().NotBeNull();
    // Verify the key is URL-encoded.
    capturedRequest!.RequestUri!.OriginalString.Should().Contain("my%20key%2Bspecial%2Fchars");
  }

  #endregion


  #region Value Operations - GetValueWithExpirationAsync

  /// <summary>Verifies that GetValueWithExpirationAsync extracts expiration from response header.</summary>
  [Fact]
  public async Task GetValueWithExpirationAsync_ExtractsExpirationFromHeader()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my-key";
    var value = "my-value";
    var expiration = 1735689600L;

    var sut = CreateSutWithHeaders(
      value,
      HttpStatusCode.OK,
      new[] { new KeyValuePair<string, string>("expiration", expiration.ToString()) });

    // Act
    var result = await sut.GetValueWithExpirationAsync(namespaceId, key);

    // Assert
    result.Should().NotBeNull();
    result!.Value.Should().Be(value);
    result.Expiration.Should().Be(expiration);
  }

  /// <summary>Verifies that GetValueWithExpirationAsync returns null expiration when header is absent.</summary>
  [Fact]
  public async Task GetValueWithExpirationAsync_WhenNoExpirationHeader_ReturnsNullExpiration()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my-key";
    var value = "my-value";

    var sut = CreateSutWithHeaders(value, HttpStatusCode.OK, []);

    // Act
    var result = await sut.GetValueWithExpirationAsync(namespaceId, key);

    // Assert
    result.Should().NotBeNull();
    result!.Value.Should().Be(value);
    result.Expiration.Should().BeNull();
  }

  /// <summary>Verifies that GetValueWithExpirationAsync returns null for non-existent keys.</summary>
  [Fact]
  public async Task GetValueWithExpirationAsync_WhenKeyNotFound_ReturnsNull()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "non-existent-key";
    var sut = CreateSutWithHeaders("", HttpStatusCode.NotFound, []);

    // Act
    var result = await sut.GetValueWithExpirationAsync(namespaceId, key);

    // Assert
    result.Should().BeNull();
  }

  #endregion


  #region Value Operations - GetValueBytesAsync

  /// <summary>Verifies that GetValueBytesAsync returns binary data correctly.</summary>
  [Fact]
  public async Task GetValueBytesAsync_ReturnsBinaryData()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "binary-key";
    var valueBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };

    var mockHandler = new Mock<HttpMessageHandler>();
    var response = new HttpResponseMessage
    {
      StatusCode = HttpStatusCode.OK,
      Content    = new ByteArrayContent(valueBytes)
    };
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(response);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = TestAccountId });
    var sut        = new KvApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.GetValueBytesAsync(namespaceId, key);

    // Assert
    result.Should().BeEquivalentTo(valueBytes);
  }

  /// <summary>Verifies that GetValueBytesAsync returns null for non-existent keys.</summary>
  [Fact]
  public async Task GetValueBytesAsync_WhenKeyNotFound_ReturnsNull()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "non-existent-key";
    var sut = CreateSutWithHeaders("", HttpStatusCode.NotFound, []);

    // Act
    var result = await sut.GetValueBytesAsync(namespaceId, key);

    // Assert
    result.Should().BeNull();
  }

  #endregion


  #region Value Operations - GetMetadataAsync

  /// <summary>Verifies that GetMetadataAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetMetadataAsync_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my-key";
    var metadata = new { category = "config", version = 1 };
    var successResponse = HttpFixtures.CreateSuccessResponse(metadata);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetMetadataAsync(namespaceId, key);

    // Assert
    result.Should().NotBeNull();
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces/{namespaceId}/metadata/{key}");
  }

  /// <summary>Verifies that GetMetadataAsync URL-encodes special characters in key names.</summary>
  [Fact]
  public async Task GetMetadataAsync_UrlEncodesKeyName()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my key/with+special";
    var successResponse = HttpFixtures.CreateSuccessResponse(new { });

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.GetMetadataAsync(namespaceId, key);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.OriginalString.Should().Contain("my%20key%2Fwith%2Bspecial");
  }

  #endregion


  #region Value Operations - WriteValueAsync (String)

  /// <summary>Verifies that WriteValueAsync sends a correctly formatted PUT request for string values.</summary>
  [Fact]
  public async Task WriteValueAsync_String_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my-key";
    var value = "my-value";

    HttpRequestMessage? capturedRequest = null;
    string? capturedContent = null;
    var sut = CreateSutWithHeaders("", HttpStatusCode.OK, [], (req, _) =>
    {
      capturedRequest = req;
      capturedContent = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    await sut.WriteValueAsync(namespaceId, key, value);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces/{namespaceId}/values/{key}");
    capturedContent.Should().Be(value);
  }

  /// <summary>Verifies that WriteValueAsync includes expiration query parameter.</summary>
  [Fact]
  public async Task WriteValueAsync_WithExpiration_IncludesExpirationInQueryString()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my-key";
    var value = "my-value";
    var expiration = 1735689600L;

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSutWithHeaders("", HttpStatusCode.OK, [], (req, _) => capturedRequest = req);

    // Act
    await sut.WriteValueAsync(namespaceId, key, value, new KvWriteOptions(Expiration: expiration));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain($"expiration={expiration}");
  }

  /// <summary>Verifies that WriteValueAsync includes expiration_ttl query parameter.</summary>
  [Fact]
  public async Task WriteValueAsync_WithExpirationTtl_IncludesExpirationTtlInQueryString()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my-key";
    var value = "my-value";
    var ttl = 3600;

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSutWithHeaders("", HttpStatusCode.OK, [], (req, _) => capturedRequest = req);

    // Act
    await sut.WriteValueAsync(namespaceId, key, value, new KvWriteOptions(ExpirationTtl: ttl));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain($"expiration_ttl={ttl}");
  }

  /// <summary>Verifies that WriteValueAsync uses multipart when metadata is provided.</summary>
  [Fact]
  public async Task WriteValueAsync_WithMetadata_UsesMultipartFormData()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my-key";
    var value = "my-value";
    var metadata = JsonSerializer.SerializeToElement(new { category = "config" });

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSutWithHeaders("", HttpStatusCode.OK, [], (req, _) => capturedRequest = req);

    // Act
    await sut.WriteValueAsync(namespaceId, key, value, new KvWriteOptions(Metadata: metadata));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Content.Should().BeOfType<MultipartFormDataContent>();
  }

  /// <summary>Verifies that WriteValueAsync URL-encodes special characters in key names.</summary>
  [Fact]
  public async Task WriteValueAsync_String_UrlEncodesKeyName()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my key/special+chars";
    var value = "my-value";

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSutWithHeaders("", HttpStatusCode.OK, [], (req, _) => capturedRequest = req);

    // Act
    await sut.WriteValueAsync(namespaceId, key, value);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.OriginalString.Should().Contain("my%20key%2Fspecial%2Bchars");
  }

  #endregion


  #region Value Operations - WriteValueAsync (Bytes)

  /// <summary>Verifies that WriteValueAsync sends a correctly formatted PUT request for binary values.</summary>
  [Fact]
  public async Task WriteValueAsync_Bytes_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "binary-key";
    var value = new byte[] { 0x01, 0x02, 0x03, 0x04 };

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSutWithHeaders("", HttpStatusCode.OK, [], (req, _) => capturedRequest = req);

    // Act
    await sut.WriteValueAsync(namespaceId, key, value);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.Content.Should().BeOfType<ByteArrayContent>();
    capturedRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/octet-stream");
  }

  /// <summary>Verifies that WriteValueAsync (bytes) uses multipart when metadata is provided.</summary>
  [Fact]
  public async Task WriteValueAsync_Bytes_WithMetadata_UsesMultipartFormData()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "binary-key";
    var value = new byte[] { 0x01, 0x02, 0x03 };
    var metadata = JsonSerializer.SerializeToElement(new { type = "binary" });

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSutWithHeaders("", HttpStatusCode.OK, [], (req, _) => capturedRequest = req);

    // Act
    await sut.WriteValueAsync(namespaceId, key, value, new KvWriteOptions(Metadata: metadata));

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Content.Should().BeOfType<MultipartFormDataContent>();
  }

  #endregion


  #region Value Operations - DeleteValueAsync

  /// <summary>Verifies that DeleteValueAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DeleteValueAsync_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId     = "ns-123";
    var key             = "key-to-delete";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.DeleteValueAsync(namespaceId, key);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces/{namespaceId}/values/{key}");
  }

  /// <summary>Verifies that DeleteValueAsync URL-encodes special characters in key names.</summary>
  [Fact]
  public async Task DeleteValueAsync_UrlEncodesKeyName()
  {
    // Arrange
    var namespaceId     = "ns-123";
    var key             = "key with/special+chars";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.DeleteValueAsync(namespaceId, key);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.OriginalString.Should().Contain("key%20with%2Fspecial%2Bchars");
  }

  #endregion


  #region Bulk Operations - BulkWriteAsync

  /// <summary>Verifies that BulkWriteAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task BulkWriteAsync_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId = "ns-123";
    var items = new[]
    {
      new KvBulkWriteItem("key1", "value1"),
      new KvBulkWriteItem("key2", "value2", Expiration: 1735689600)
    };
    var expectedResult = new KvBulkWriteResult(2, null);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.BulkWriteAsync(namespaceId, items);

    // Assert
    result.SuccessfulKeyCount.Should().Be(2);
    result.UnsuccessfulKeys.Should().BeNull();
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces/{namespaceId}/bulk");

    // Verify the JSON body is an array of items.
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetArrayLength().Should().Be(2);
  }

  /// <summary>Verifies that BulkWriteAsync handles partial failures correctly.</summary>
  [Fact]
  public async Task BulkWriteAsync_WithPartialFailure_ReturnsUnsuccessfulKeys()
  {
    // Arrange
    var namespaceId = "ns-123";
    var items = new[]
    {
      new KvBulkWriteItem("key1", "value1"),
      new KvBulkWriteItem("key2", "value2")
    };
    var expectedResult = new KvBulkWriteResult(1, new[] { "key2" });
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    var sut = CreateSut(successResponse);

    // Act
    var result = await sut.BulkWriteAsync(namespaceId, items);

    // Assert
    result.SuccessfulKeyCount.Should().Be(1);
    result.UnsuccessfulKeys.Should().ContainSingle().Which.Should().Be("key2");
  }

  #endregion


  #region Bulk Operations - BulkDeleteAsync

  /// <summary>Verifies that BulkDeleteAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task BulkDeleteAsync_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId = "ns-123";
    var keys = new[] { "key1", "key2", "key3" };
    var expectedResult = new KvBulkDeleteResult(3, null);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.BulkDeleteAsync(namespaceId, keys);

    // Assert
    result.SuccessfulKeyCount.Should().Be(3);
    result.UnsuccessfulKeys.Should().BeNull();
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces/{namespaceId}/bulk/delete");

    // Verify the JSON body is an array of key names.
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetArrayLength().Should().Be(3);
  }

  #endregion


  #region Bulk Operations - BulkGetAsync

  /// <summary>Verifies that BulkGetAsync sends a correctly formatted POST request with camelCase body.</summary>
  [Fact]
  public async Task BulkGetAsync_SendsCorrectRequest()
  {
    // Arrange
    var namespaceId = "ns-123";
    var keys = new[] { "key1", "key2" };
    var resultValues = new Dictionary<string, string?> { { "key1", "value1" }, { "key2", "value2" } };
    var successResponse = HttpFixtures.CreateSuccessResponse(new { values = resultValues });

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.BulkGetAsync(namespaceId, keys);

    // Assert
    result.Should().HaveCount(2);
    result["key1"].Should().Be("value1");
    result["key2"].Should().Be("value2");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/storage/kv/namespaces/{namespaceId}/bulk/get");

    // Verify the JSON body uses camelCase.
    capturedJsonBody.Should().NotBeNull();
    capturedJsonBody.Should().Contain("\"keys\"");
    capturedJsonBody.Should().NotContain("\"withMetadata\""); // null should be omitted
  }

  /// <summary>Verifies that BulkGetAsync returns null values for non-existent keys.</summary>
  [Fact]
  public async Task BulkGetAsync_WhenKeyNotFound_ReturnsNullValue()
  {
    // Arrange
    var namespaceId = "ns-123";
    var keys = new[] { "key1", "non-existent" };
    var resultValues = new Dictionary<string, string?> { { "key1", "value1" }, { "non-existent", null } };
    var successResponse = HttpFixtures.CreateSuccessResponse(new { values = resultValues });

    var sut = CreateSut(successResponse);

    // Act
    var result = await sut.BulkGetAsync(namespaceId, keys);

    // Assert
    result.Should().HaveCount(2);
    result["key1"].Should().Be("value1");
    result["non-existent"].Should().BeNull();
  }

  #endregion


  #region Bulk Operations - BulkGetWithMetadataAsync

  /// <summary>Verifies that BulkGetWithMetadataAsync sends withMetadata=true in the request body.</summary>
  [Fact]
  public async Task BulkGetWithMetadataAsync_SendsWithMetadataFlag()
  {
    // Arrange
    var namespaceId = "ns-123";
    var keys = new[] { "key1" };
    var resultValues = new Dictionary<string, KvBulkGetItemWithMetadata?>
    {
      { "key1", new KvBulkGetItemWithMetadata("value1", JsonSerializer.SerializeToElement(new { category = "test" })) }
    };
    var successResponse = HttpFixtures.CreateSuccessResponse(new { values = resultValues });

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.BulkGetWithMetadataAsync(namespaceId, keys);

    // Assert
    result.Should().HaveCount(1);
    result["key1"]!.Value.Should().Be("value1");
    capturedJsonBody.Should().NotBeNull();
    // Verify camelCase withMetadata (not with_metadata).
    capturedJsonBody.Should().Contain("\"withMetadata\":true");
  }

  #endregion


  #region Error Handling

  /// <summary>Verifies that API errors are properly propagated as CloudflareApiException.</summary>
  [Fact]
  public async Task CreateAsync_WhenApiReturnsError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10014, "Namespace title already exists");
    var sut           = CreateSut(errorResponse);

    // Act
    var action = async () => await sut.CreateAsync("Existing Namespace");

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareApiException>();
    ex.Which.Message.Should().Contain("10014");
    ex.Which.Errors.Should().ContainSingle().Which.Code.Should().Be(10014);
  }

  /// <summary>Verifies that GetValueAsync throws on non-404 errors.</summary>
  [Fact]
  public async Task GetValueAsync_WhenApiReturnsServerError_ThrowsHttpRequestException()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my-key";
    var sut = CreateSutWithHeaders("Internal Server Error", HttpStatusCode.InternalServerError, []);

    // Act
    var action = async () => await sut.GetValueAsync(namespaceId, key);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>();
  }

  /// <summary>Verifies that WriteValueAsync throws on errors.</summary>
  [Fact]
  public async Task WriteValueAsync_WhenApiReturnsError_ThrowsHttpRequestException()
  {
    // Arrange
    var namespaceId = "ns-123";
    var key = "my-key";
    var sut = CreateSutWithHeaders("Bad Request", HttpStatusCode.BadRequest, []);

    // Act
    var action = async () => await sut.WriteValueAsync(namespaceId, key, "value");

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>();
  }

  /// <summary>Verifies that DeleteAsync propagates API errors correctly.</summary>
  [Fact]
  public async Task DeleteAsync_WhenApiReturnsError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10007, "Namespace not found");
    var sut           = CreateSut(errorResponse);

    // Act
    var action = async () => await sut.DeleteAsync("non-existent-namespace");

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareApiException>();
    ex.Which.Errors.Should().ContainSingle().Which.Code.Should().Be(10007);
  }

  /// <summary>Verifies that BulkWriteAsync propagates API errors correctly.</summary>
  [Fact]
  public async Task BulkWriteAsync_WhenApiReturnsError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10013, "Request body too large");
    var sut           = CreateSut(errorResponse);

    // Act
    var action = async () => await sut.BulkWriteAsync("ns-123", new[] { new KvBulkWriteItem("key", "value") });

    // Assert
    await action.Should().ThrowAsync<CloudflareApiException>();
  }

  #endregion
}
