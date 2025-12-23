namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Accounts;
using Accounts.Buckets;
using Accounts.Models;
using Cloudflare.NET.Core.Exceptions;
using Cloudflare.NET.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>Contains unit tests for the <see cref="R2BucketsApi" /> class.</summary>
/// <remarks>
///   This test class covers all R2 bucket operations including:
///   <list type="bullet">
///     <item><description>Core bucket CRUD operations (Create, Get, List, Update, Delete)</description></item>
///     <item><description>CORS policy management</description></item>
///     <item><description>Lifecycle policy management</description></item>
///     <item><description>Custom domain management</description></item>
///     <item><description>Managed domain (r2.dev) management</description></item>
///     <item><description>Bucket lock configuration</description></item>
///     <item><description>Sippy (incremental migration) configuration</description></item>
///     <item><description>Temporary credentials generation</description></item>
///   </list>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class R2BucketsApiUnitTests
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

  /// <summary>Initializes a new instance of the <see cref="R2BucketsApiUnitTests" /> class.</summary>
  /// <param name="output">The xUnit test output helper.</param>
  public R2BucketsApiUnitTests(ITestOutputHelper output)
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
  /// <returns>A configured <see cref="R2BucketsApi" /> instance.</returns>
  private R2BucketsApi CreateSut(
    string                                              responseContent,
    HttpStatusCode                                      statusCode = HttpStatusCode.OK,
    Action<HttpRequestMessage, CancellationToken>? callback   = null)
  {
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseContent, statusCode, callback);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options     = Options.Create(new CloudflareApiOptions { AccountId = TestAccountId });

    return new R2BucketsApi(httpClient, options, _loggerFactory);
  }

  #endregion


  #region Core Bucket Operations - CreateAsync

  /// <summary>Verifies that CreateAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName     = "test-bucket";
    var expectedResult = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), "wnam", null, "Standard");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.CreateAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets");

    // Verify JSON body contains only the bucket name
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("name").GetString().Should().Be(bucketName);
  }

  /// <summary>Verifies that CreateAsync with location hint sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateAsync_WithLocationHint_SendsCorrectRequest()
  {
    // Arrange
    var bucketName     = "eu-bucket";
    var expectedResult = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), R2LocationHint.WestEurope, null, R2StorageClass.Standard);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.CreateAsync(bucketName, R2LocationHint.WestEurope);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);

    // Verify JSON contains the location hint
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("name").GetString().Should().Be(bucketName);
    doc.RootElement.GetProperty("locationHint").GetString().Should().Be("weur");
  }

  /// <summary>Verifies that CreateAsync with all options sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateAsync_WithAllOptions_SendsCorrectRequest()
  {
    // Arrange
    var bucketName     = "gdpr-bucket";
    var expectedResult = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), R2LocationHint.WestEurope, R2Jurisdiction.EuropeanUnion, R2StorageClass.InfrequentAccess);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.CreateAsync(
      bucketName,
      locationHint: R2LocationHint.WestEurope,
      jurisdiction: R2Jurisdiction.EuropeanUnion,
      storageClass: R2StorageClass.InfrequentAccess
    );

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);

    // Verify JSON body contains locationHint and storageClass (NOT jurisdiction - it's a header)
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("name").GetString().Should().Be(bucketName);
    doc.RootElement.GetProperty("locationHint").GetString().Should().Be("weur");
    doc.RootElement.GetProperty("storageClass").GetString().Should().Be("InfrequentAccess");

    // Jurisdiction is NOT in the body - verify it's absent
    doc.RootElement.TryGetProperty("jurisdiction", out _).Should().BeFalse(
      "jurisdiction should be passed as HTTP header, not in request body");

    // Verify jurisdiction is passed as HTTP header (cf-r2-jurisdiction)
    capturedRequest.Headers.TryGetValues("cf-r2-jurisdiction", out var jurisdictionValues).Should().BeTrue(
      "jurisdiction should be passed as 'cf-r2-jurisdiction' HTTP header");
    jurisdictionValues.Should().ContainSingle().Which.Should().Be("eu");
  }

  /// <summary>Verifies that CreateAsync without jurisdiction does NOT send the header.</summary>
  [Fact]
  public async Task CreateAsync_WithoutJurisdiction_DoesNotSendHeader()
  {
    // Arrange
    var bucketName     = "simple-bucket";
    var expectedResult = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), R2LocationHint.EastNorthAmerica, null, R2StorageClass.Standard);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.CreateAsync(bucketName, locationHint: R2LocationHint.EastNorthAmerica);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();

    // Verify jurisdiction header is NOT present when jurisdiction is null
    capturedRequest!.Headers.TryGetValues("cf-r2-jurisdiction", out _).Should().BeFalse(
      "jurisdiction header should not be sent when jurisdiction is null");
  }

  #endregion


  #region Core Bucket Operations - GetAsync

  /// <summary>Verifies that GetAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName     = "my-bucket";
    var expectedResult = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), "wnam", null, "Standard");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}");
  }

  /// <summary>Verifies that GetAsync URL-encodes special characters in bucket names.</summary>
  [Fact]
  public async Task GetAsync_UrlEncodesBucketName()
  {
    // Arrange
    var bucketName     = "my bucket+special";
    var expectedResult = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), "wnam", null, "Standard");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    // Verify the bucket name is URL-encoded using OriginalString to avoid automatic decoding
    capturedRequest!.RequestUri!.OriginalString.Should().Contain("my%20bucket%2Bspecial");
  }

  /// <summary>Verifies that GetAsync with jurisdiction sends the cf-r2-jurisdiction header.</summary>
  [Fact]
  public async Task GetAsync_WithJurisdiction_SendsJurisdictionHeader()
  {
    // Arrange
    var bucketName     = "eu-bucket";
    var expectedResult = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), "weur", R2Jurisdiction.EuropeanUnion, "Standard");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetAsync(bucketName, R2Jurisdiction.EuropeanUnion);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);

    // Verify jurisdiction header is sent
    capturedRequest.Headers.TryGetValues("cf-r2-jurisdiction", out var jurisdictionValues).Should().BeTrue(
      "jurisdiction should be passed as 'cf-r2-jurisdiction' HTTP header for jurisdictional buckets");
    jurisdictionValues.Should().ContainSingle().Which.Should().Be("eu");
  }

  /// <summary>Verifies that GetAsync without jurisdiction does NOT send the header.</summary>
  [Fact]
  public async Task GetAsync_WithoutJurisdiction_DoesNotSendHeader()
  {
    // Arrange
    var bucketName      = "standard-bucket";
    var expectedResult  = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), "wnam", null, "Standard");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();

    // Verify jurisdiction header is NOT present
    capturedRequest!.Headers.TryGetValues("cf-r2-jurisdiction", out _).Should().BeFalse(
      "jurisdiction header should not be sent when jurisdiction is null");
  }

  #endregion


  #region Core Bucket Operations - ListAsync & ListAllAsync

  /// <summary>Verifies that ListAsync constructs the correct URI with no filters.</summary>
  [Fact]
  public async Task ListAsync_ShouldConstructCorrectRequestUri_WithNoFilters()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse(new ListR2BucketsResponse(Array.Empty<R2Bucket>()));

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync();

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets");
  }

  /// <summary>Verifies that ListAsync constructs the correct URI with pagination filters.</summary>
  [Fact]
  public async Task ListAsync_ShouldConstructCorrectRequestUri_WithPaginationFilters()
  {
    // Arrange
    var filters         = new ListR2BucketsFilters(50, "abc123def");
    var successResponse = HttpFixtures.CreateSuccessResponse(new ListR2BucketsResponse(Array.Empty<R2Bucket>()));

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets?per_page=50&cursor=abc123def");
  }

  /// <summary>Verifies that ListAsync includes the NameContains filter in the query string.</summary>
  [Fact]
  public async Task ListAsync_WithNameContainsFilter_ShouldConstructCorrectRequestUri()
  {
    // Arrange
    var filters         = new ListR2BucketsFilters(NameContains: "backup");
    var successResponse = HttpFixtures.CreateSuccessResponse(new ListR2BucketsResponse(Array.Empty<R2Bucket>()));

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets?name_contains=backup");
  }

  /// <summary>Verifies that ListAsync includes the Order filter in the query string.</summary>
  [Fact]
  public async Task ListAsync_WithOrderFilter_ShouldConstructCorrectRequestUri()
  {
    // Arrange
    var filters         = new ListR2BucketsFilters(Order: "name");
    var successResponse = HttpFixtures.CreateSuccessResponse(new ListR2BucketsResponse(Array.Empty<R2Bucket>()));

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets?order=name");
  }

  /// <summary>Verifies that ListAsync includes the Direction filter in the query string.</summary>
  [Fact]
  public async Task ListAsync_WithDirectionFilter_ShouldConstructCorrectRequestUri()
  {
    // Arrange
    var filters         = new ListR2BucketsFilters(Direction: "desc");
    var successResponse = HttpFixtures.CreateSuccessResponse(new ListR2BucketsResponse(Array.Empty<R2Bucket>()));

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets?direction=desc");
  }

  /// <summary>Verifies that ListAsync includes the StartAfter filter in the query string.</summary>
  [Fact]
  public async Task ListAsync_WithStartAfterFilter_ShouldConstructCorrectRequestUri()
  {
    // Arrange
    var filters         = new ListR2BucketsFilters(StartAfter: "my-bucket");
    var successResponse = HttpFixtures.CreateSuccessResponse(new ListR2BucketsResponse(Array.Empty<R2Bucket>()));

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets?start_after=my-bucket");
  }

  /// <summary>Verifies that ListAsync constructs the correct URI with all filter parameters.</summary>
  [Fact]
  public async Task ListAsync_WithAllFilters_ShouldConstructCorrectRequestUri()
  {
    // Arrange
    var filters = new ListR2BucketsFilters(
      PerPage: 50,
      Cursor: "cursor123",
      NameContains: "backup",
      Order: "name",
      Direction: "asc",
      StartAfter: "archive"
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(new ListR2BucketsResponse(Array.Empty<R2Bucket>()));

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.ToString();
    uri.Should().Contain("per_page=50");
    uri.Should().Contain("cursor=cursor123");
    uri.Should().Contain("name_contains=backup");
    uri.Should().Contain("order=name");
    uri.Should().Contain("direction=asc");
    uri.Should().Contain("start_after=archive");
  }

  /// <summary>Verifies that ListAsync URL-encodes special characters in filter values.</summary>
  [Fact]
  public async Task ListAsync_WithSpecialCharactersInFilters_ShouldUrlEncodeValues()
  {
    // Arrange
    var filters         = new ListR2BucketsFilters(NameContains: "backup+archive", StartAfter: "my bucket/test");
    var successResponse = HttpFixtures.CreateSuccessResponse(new ListR2BucketsResponse(Array.Empty<R2Bucket>()));

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.OriginalString;
    uri.Should().Contain("name_contains=backup%2Barchive");
    uri.Should().Contain("start_after=my%20bucket%2Ftest");
  }

  /// <summary>Verifies that ListAsync sends the jurisdiction header when specified.</summary>
  [Fact]
  public async Task ListAsync_WithJurisdiction_ShouldSendJurisdictionHeader()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse(new ListR2BucketsResponse(Array.Empty<R2Bucket>()));

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(jurisdiction: R2Jurisdiction.EuropeanUnion);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Headers.TryGetValues("cf-r2-jurisdiction", out var jurisdictionValues).Should().BeTrue(
      "jurisdiction should be passed as 'cf-r2-jurisdiction' HTTP header");
    jurisdictionValues.Should().ContainSingle().Which.Should().Be("eu");
  }

  /// <summary>Verifies that ListAsync sends both filters and jurisdiction correctly.</summary>
  [Fact]
  public async Task ListAsync_WithFiltersAndJurisdiction_ShouldSendBoth()
  {
    // Arrange
    var filters         = new ListR2BucketsFilters(PerPage: 25, NameContains: "eu-data");
    var successResponse = HttpFixtures.CreateSuccessResponse(new ListR2BucketsResponse(Array.Empty<R2Bucket>()));

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.ListAsync(filters, R2Jurisdiction.EuropeanUnion);

    // Assert
    capturedRequest.Should().NotBeNull();

    // Verify query parameters
    var uri = capturedRequest!.RequestUri!.ToString();
    uri.Should().Contain("per_page=25");
    uri.Should().Contain("name_contains=eu-data");

    // Verify jurisdiction header
    capturedRequest.Headers.TryGetValues("cf-r2-jurisdiction", out var jurisdictionValues).Should().BeTrue();
    jurisdictionValues.Should().ContainSingle().Which.Should().Be("eu");
  }

  /// <summary>Verifies that ListAllAsync handles pagination correctly.</summary>
  [Fact]
  public async Task ListAllAsync_ShouldHandlePaginationCorrectly()
  {
    // Arrange
    var bucket1 = new R2Bucket("bucket1", DateTime.UtcNow, "loc", null, "class");
    var bucket2 = new R2Bucket("bucket2", DateTime.UtcNow, "loc", null, "class");
    var cursor  = "next_page_cursor";

    // First page response with a cursor. The Cloudflare R2 API returns cursor pagination info
    // in the standard "result_info" field, not a separate "cursor_result_info" field.
    var responsePage1 =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new { buckets = new[] { bucket1 } },
          result_info = new { count = 1, per_page = 1, cursor, page = 0, total_count = 0, total_pages = 0 }
        },
        _serializerOptions);

    // Second page response without a cursor (end of pagination).
    var responsePage2 =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new { buckets = new[] { bucket2 } },
          result_info = new { count = 1, per_page = 1, cursor = (string?)null, page = 0, total_count = 0, total_pages = 0 }
        },
        _serializerOptions);

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
    var sut        = new R2BucketsApi(httpClient, options, _loggerFactory);

    // Act
    var allBuckets = new List<R2Bucket>();
    await foreach (var bucket in sut.ListAllAsync())
      allBuckets.Add(bucket);

    // Assert
    capturedRequests.Should().HaveCount(2);
    capturedRequests[0].RequestUri!.Query.Should().NotContain("cursor");
    capturedRequests[1].RequestUri!.Query.Should().Contain($"cursor={cursor}");
    allBuckets.Should().HaveCount(2);
    allBuckets.Select(b => b.Name).Should().ContainInOrder("bucket1", "bucket2");
  }

  /// <summary>
  ///   Verifies that ListAllAsync constructs proper URLs without malformed query strings.
  ///   This specifically tests that the second page request (with cursor but no perPage) doesn't
  ///   produce a malformed URL like "?&amp;cursor=..." instead of "?cursor=...".
  /// </summary>
  [Fact]
  public async Task ListAllAsync_ShouldConstructProperUrlsWithoutMalformedQueryStrings()
  {
    // Arrange
    var bucket1 = new R2Bucket("bucket1", DateTime.UtcNow, "loc", null, "class");
    var bucket2 = new R2Bucket("bucket2", DateTime.UtcNow, "loc", null, "class");
    var cursor  = "next_page_cursor";

    // First page response with a cursor (no perPage specified by user).
    var responsePage1 =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new { buckets = new[] { bucket1 } },
          result_info = new { count = 1, per_page = 10, cursor, page = 0, total_count = 0, total_pages = 0 }
        },
        _serializerOptions);

    // Second page response without a cursor (end of pagination).
    var responsePage2 =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new { buckets = new[] { bucket2 } },
          result_info = new { count = 1, per_page = 10, cursor = (string?)null, page = 0, total_count = 0, total_pages = 0 }
        },
        _serializerOptions);

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
    var sut        = new R2BucketsApi(httpClient, options, _loggerFactory);

    // Act - ListAllAsync WITHOUT any filters (no perPage specified)
    var allBuckets = new List<R2Bucket>();
    await foreach (var bucket in sut.ListAllAsync())
      allBuckets.Add(bucket);

    // Assert - Verify URLs are properly formed
    capturedRequests.Should().HaveCount(2);

    // First request should not have any query string (no perPage, no cursor)
    var firstRequestUri = capturedRequests[0].RequestUri!.ToString();
    firstRequestUri.Should().NotContain("?&", "the first request should not have malformed query string");
    firstRequestUri.Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets");

    // Second request should have cursor as the only query param, properly formatted
    var secondRequestUri = capturedRequests[1].RequestUri!.ToString();
    secondRequestUri.Should().NotContain("?&", "the second request should not have malformed query string like '?&cursor='");
    secondRequestUri.Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets?cursor={cursor}");

    // Verify we got all buckets from both pages
    allBuckets.Should().HaveCount(2);
  }

  /// <summary>Verifies that ListAllAsync includes filter parameters in the base URL.</summary>
  [Fact]
  public async Task ListAllAsync_WithFilters_ShouldIncludeFiltersInBaseUrl()
  {
    // Arrange
    var bucket1 = new R2Bucket("backup-bucket1", DateTime.UtcNow, "loc", null, "class");

    var responsePage =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new { buckets = new[] { bucket1 } },
          result_info = new { count = 1, per_page = 50, cursor = (string?)null, page = 0, total_count = 0, total_pages = 0 }
        },
        _serializerOptions);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(responsePage, callback: (req, _) => capturedRequest = req);

    // Act
    var allBuckets = new List<R2Bucket>();
    await foreach (var bucket in sut.ListAllAsync(new ListR2BucketsFilters(
      PerPage: 50,
      NameContains: "backup",
      Order: "name",
      Direction: "desc"
    )))
      allBuckets.Add(bucket);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.ToString();
    uri.Should().Contain("name_contains=backup");
    uri.Should().Contain("order=name");
    uri.Should().Contain("direction=desc");
    uri.Should().Contain("per_page=50");
    allBuckets.Should().HaveCount(1);
  }

  /// <summary>Verifies that ListAllAsync preserves filters across pagination requests.</summary>
  [Fact]
  public async Task ListAllAsync_WithFilters_ShouldPreserveFiltersAcrossPagination()
  {
    // Arrange
    var bucket1 = new R2Bucket("backup-1", DateTime.UtcNow, "loc", null, "class");
    var bucket2 = new R2Bucket("backup-2", DateTime.UtcNow, "loc", null, "class");
    var cursor  = "page2_cursor";

    // First page response with a cursor
    var responsePage1 =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new { buckets = new[] { bucket1 } },
          result_info = new { count = 1, per_page = 1, cursor, page = 0, total_count = 0, total_pages = 0 }
        },
        _serializerOptions);

    // Second page response without a cursor (end of pagination)
    var responsePage2 =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new { buckets = new[] { bucket2 } },
          result_info = new { count = 1, per_page = 1, cursor = (string?)null, page = 0, total_count = 0, total_pages = 0 }
        },
        _serializerOptions);

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
    var sut        = new R2BucketsApi(httpClient, options, _loggerFactory);

    // Act
    var allBuckets = new List<R2Bucket>();
    await foreach (var bucket in sut.ListAllAsync(new ListR2BucketsFilters(
      PerPage: 1,
      NameContains: "backup",
      Order: "name",
      Direction: "asc"
    )))
      allBuckets.Add(bucket);

    // Assert
    capturedRequests.Should().HaveCount(2);

    // First request should have all filters
    var firstUri = capturedRequests[0].RequestUri!.ToString();
    firstUri.Should().Contain("name_contains=backup");
    firstUri.Should().Contain("order=name");
    firstUri.Should().Contain("direction=asc");
    firstUri.Should().NotContain("cursor");

    // Second request should have cursor appended to the filtered URL
    var secondUri = capturedRequests[1].RequestUri!.ToString();
    secondUri.Should().Contain("name_contains=backup");
    secondUri.Should().Contain("order=name");
    secondUri.Should().Contain("direction=asc");
    secondUri.Should().Contain($"cursor={cursor}");

    allBuckets.Should().HaveCount(2);
    allBuckets.Select(b => b.Name).Should().ContainInOrder("backup-1", "backup-2");
  }

  /// <summary>Verifies that ListAllAsync sends the jurisdiction header when specified.</summary>
  [Fact]
  public async Task ListAllAsync_WithJurisdiction_ShouldSendJurisdictionHeader()
  {
    // Arrange
    var bucket = new R2Bucket("eu-bucket", DateTime.UtcNow, "weur", R2Jurisdiction.EuropeanUnion, "Standard");

    var responsePage =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new { buckets = new[] { bucket } },
          result_info = new { count = 1, per_page = 50, cursor = (string?)null, page = 0, total_count = 0, total_pages = 0 }
        },
        _serializerOptions);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(responsePage, callback: (req, _) => capturedRequest = req);

    // Act
    var allBuckets = new List<R2Bucket>();
    await foreach (var b in sut.ListAllAsync(jurisdiction: R2Jurisdiction.EuropeanUnion))
      allBuckets.Add(b);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Headers.TryGetValues("cf-r2-jurisdiction", out var jurisdictionValues).Should().BeTrue(
      "jurisdiction should be passed as 'cf-r2-jurisdiction' HTTP header");
    jurisdictionValues.Should().ContainSingle().Which.Should().Be("eu");
  }

  /// <summary>Verifies that ListAllAsync sends both filters and jurisdiction correctly.</summary>
  [Fact]
  public async Task ListAllAsync_WithFiltersAndJurisdiction_ShouldSendBoth()
  {
    // Arrange
    var bucket = new R2Bucket("eu-backup", DateTime.UtcNow, "weur", R2Jurisdiction.EuropeanUnion, "Standard");

    var responsePage =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new { buckets = new[] { bucket } },
          result_info = new { count = 1, per_page = 25, cursor = (string?)null, page = 0, total_count = 0, total_pages = 0 }
        },
        _serializerOptions);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(responsePage, callback: (req, _) => capturedRequest = req);

    // Act
    var allBuckets = new List<R2Bucket>();
    await foreach (var b in sut.ListAllAsync(
      new ListR2BucketsFilters(PerPage: 25, NameContains: "backup"),
      R2Jurisdiction.EuropeanUnion))
      allBuckets.Add(b);

    // Assert
    capturedRequest.Should().NotBeNull();

    // Verify query parameters
    var uri = capturedRequest!.RequestUri!.ToString();
    uri.Should().Contain("name_contains=backup");

    // Verify jurisdiction header
    capturedRequest.Headers.TryGetValues("cf-r2-jurisdiction", out var jurisdictionValues).Should().BeTrue();
    jurisdictionValues.Should().ContainSingle().Which.Should().Be("eu");
  }

  /// <summary>Verifies that ListAllAsync URL-encodes special characters in filter values.</summary>
  [Fact]
  public async Task ListAllAsync_WithSpecialCharactersInFilters_ShouldUrlEncodeValues()
  {
    // Arrange
    var bucket = new R2Bucket("test-bucket", DateTime.UtcNow, "loc", null, "class");

    var responsePage =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new { buckets = new[] { bucket } },
          result_info = new { count = 1, per_page = 50, cursor = (string?)null, page = 0, total_count = 0, total_pages = 0 }
        },
        _serializerOptions);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(responsePage, callback: (req, _) => capturedRequest = req);

    // Act
    var allBuckets = new List<R2Bucket>();
    await foreach (var b in sut.ListAllAsync(new ListR2BucketsFilters(
      NameContains: "data+backup",
      StartAfter: "bucket with spaces"
    )))
      allBuckets.Add(b);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.OriginalString;
    uri.Should().Contain("name_contains=data%2Bbackup");
    uri.Should().Contain("start_after=bucket%20with%20spaces");
  }

  #endregion


  #region Core Bucket Operations - DeleteAsync

  /// <summary>Verifies that DeleteAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DeleteAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName      = "test-bucket-to-delete";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.DeleteAsync(bucketName);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}");
  }

  /// <summary>Verifies that DeleteAsync with jurisdiction sends the cf-r2-jurisdiction header.</summary>
  [Fact]
  public async Task DeleteAsync_WithJurisdiction_SendsJurisdictionHeader()
  {
    // Arrange
    var bucketName      = "eu-bucket-to-delete";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.DeleteAsync(bucketName, R2Jurisdiction.EuropeanUnion);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);

    // Verify jurisdiction header is sent
    capturedRequest.Headers.TryGetValues("cf-r2-jurisdiction", out var jurisdictionValues).Should().BeTrue(
      "jurisdiction should be passed as 'cf-r2-jurisdiction' HTTP header for jurisdictional buckets");
    jurisdictionValues.Should().ContainSingle().Which.Should().Be("eu");
  }

  #endregion


  #region Core Bucket Operations - UpdateAsync

  /// <summary>Verifies that UpdateAsync sends a correctly formatted PATCH request with cf-r2-storage-class header.</summary>
  [Fact]
  public async Task UpdateAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName     = "test-bucket";
    var storageClass   = R2StorageClass.InfrequentAccess;
    var expectedResult = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), "wnam", null, storageClass);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.UpdateAsync(bucketName, storageClass);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}");

    // Verify cf-r2-storage-class is passed as HTTP header (follows same pattern as cf-r2-jurisdiction)
    capturedRequest.Headers.TryGetValues("cf-r2-storage-class", out var storageClassValues).Should().BeTrue(
      "cf-r2-storage-class should be passed as HTTP header");
    storageClassValues.Should().ContainSingle().Which.Should().Be("InfrequentAccess");
  }

  /// <summary>Verifies that UpdateAsync with jurisdiction sends both storage_class and jurisdiction headers.</summary>
  [Fact]
  public async Task UpdateAsync_WithJurisdiction_SendsBothHeaders()
  {
    // Arrange
    var bucketName     = "eu-bucket";
    var storageClass   = R2StorageClass.Standard;
    var expectedResult = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), "weur", R2Jurisdiction.EuropeanUnion, storageClass);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.UpdateAsync(bucketName, storageClass, R2Jurisdiction.EuropeanUnion);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);

    // Verify cf-r2-storage-class header
    capturedRequest.Headers.TryGetValues("cf-r2-storage-class", out var storageClassValues).Should().BeTrue(
      "cf-r2-storage-class should be passed as HTTP header");
    storageClassValues.Should().ContainSingle().Which.Should().Be("Standard");

    // Verify cf-r2-jurisdiction header
    capturedRequest.Headers.TryGetValues("cf-r2-jurisdiction", out var jurisdictionValues).Should().BeTrue(
      "jurisdiction should be passed as 'cf-r2-jurisdiction' HTTP header for jurisdictional buckets");
    jurisdictionValues.Should().ContainSingle().Which.Should().Be("eu");
  }

  /// <summary>Verifies that UpdateAsync without jurisdiction does NOT send the jurisdiction header.</summary>
  [Fact]
  public async Task UpdateAsync_WithoutJurisdiction_DoesNotSendJurisdictionHeader()
  {
    // Arrange
    var bucketName     = "standard-bucket";
    var storageClass   = R2StorageClass.InfrequentAccess;
    var expectedResult = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), "wnam", null, storageClass);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.UpdateAsync(bucketName, storageClass);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();

    // Verify cf-r2-storage-class header is present
    capturedRequest!.Headers.TryGetValues("cf-r2-storage-class", out _).Should().BeTrue(
      "cf-r2-storage-class should be passed as HTTP header");

    // Verify jurisdiction header is NOT present
    capturedRequest.Headers.TryGetValues("cf-r2-jurisdiction", out _).Should().BeFalse(
      "jurisdiction header should not be sent when jurisdiction is null");
  }

  /// <summary>Verifies that UpdateAsync URL-encodes special characters in bucket names.</summary>
  [Fact]
  public async Task UpdateAsync_UrlEncodesBucketName()
  {
    // Arrange
    var bucketName     = "my bucket+special";
    var storageClass   = R2StorageClass.Standard;
    var expectedResult = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), "wnam", null, storageClass);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.UpdateAsync(bucketName, storageClass);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    // Verify the bucket name is URL-encoded using OriginalString to avoid automatic decoding
    capturedRequest!.RequestUri!.OriginalString.Should().Contain("my%20bucket%2Bspecial");
  }

  /// <summary>Verifies that UpdateAsync sends an empty body (storage_class is in header, not body).</summary>
  [Fact]
  public async Task UpdateAsync_SendsEmptyBody()
  {
    // Arrange
    var bucketName     = "test-bucket";
    var storageClass   = R2StorageClass.InfrequentAccess;
    var expectedResult = new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), "wnam", null, storageClass);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.UpdateAsync(bucketName, storageClass);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();

    // Verify the body is empty (storage_class is passed via cf-r2-storage-class header)
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.EnumerateObject().Should().BeEmpty(
      "storage_class should be in HTTP header, not in request body");

    // Verify the header is present
    capturedRequest!.Headers.TryGetValues("cf-r2-storage-class", out var values).Should().BeTrue();
    values.Should().ContainSingle().Which.Should().Be("InfrequentAccess");
  }

  #endregion


  #region CORS Policy Operations

  /// <summary>Verifies that GetCorsAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetCorsAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var corsRule = new CorsRule(
      new CorsAllowed(new[] { "GET", "PUT" }, new[] { "https://example.com" }, new[] { "Content-Type" }),
      "Allow Example",
      new[] { "ETag" },
      3600
    );
    var expectedResult  = new BucketCorsPolicy(new[] { corsRule });
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetCorsAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/cors");
  }

  /// <summary>Verifies that SetCorsAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task SetCorsAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var corsPolicy = new BucketCorsPolicy(
      new[]
      {
        new CorsRule(
          new CorsAllowed(new[] { "GET", "PUT", "POST" }, new[] { "https://example.com", "https://app.example.com" }, new[] { "Content-Type", "Authorization" }),
          "Production CORS",
          new[] { "ETag", "Content-Length" },
          7200
        )
      }
    );
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.SetCorsAsync(bucketName, corsPolicy);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/cors");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<BucketCorsPolicy>();
    content.Should().BeEquivalentTo(corsPolicy);
  }

  /// <summary>Verifies that DeleteCorsAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DeleteCorsAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName      = "test-bucket";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.DeleteCorsAsync(bucketName);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/cors");
  }

  #endregion


  #region Lifecycle Policy Operations

  /// <summary>Verifies that GetLifecycleAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetLifecycleAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var lifecycleRule = new LifecycleRule(
      "Delete old logs",
      true,
      new LifecycleRuleConditions("logs/"),
      DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(90))
    );
    var expectedResult  = new BucketLifecyclePolicy(new[] { lifecycleRule });
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetLifecycleAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/lifecycle");
  }

  /// <summary>Verifies that SetLifecycleAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task SetLifecycleAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var lifecyclePolicy = new BucketLifecyclePolicy(
      new[]
      {
        new LifecycleRule(
          "Delete old objects",
          true,
          new LifecycleRuleConditions("temp/"),
          DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(90))
        )
      }
    );
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    await sut.SetLifecycleAsync(bucketName, lifecyclePolicy);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/lifecycle");
    capturedJsonBody.Should().NotBeNullOrEmpty();
  }

  /// <summary>Verifies that DeleteLifecycleAsync sends a PUT request with an empty rules array.</summary>
  [Fact]
  public async Task DeleteLifecycleAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName      = "test-bucket";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    await sut.DeleteLifecycleAsync(bucketName);

    // Assert - Cloudflare R2 uses PUT with empty rules array to clear lifecycle policy
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/lifecycle");
    capturedJsonBody.Should().NotBeNullOrEmpty();
    var content = JsonSerializer.Deserialize<BucketLifecyclePolicy>(capturedJsonBody!);
    content.Should().NotBeNull();
    content!.Rules.Should().BeEmpty();
  }

  #endregion


  #region Custom Domain Operations

  /// <summary>Verifies that ListCustomDomainsAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task ListCustomDomainsAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var expectedDomains = new List<CustomDomain>
    {
      new("cdn.example.com", true, new CustomDomainStatusObject("active", "active"), null, "zone-123"),
      new("media.example.com", true, new CustomDomainStatusObject("pending", "pending"), null, "zone-456")
    };
    var responseJson = HttpFixtures.CreateSuccessResponse(new ListCustomDomainsResponse(expectedDomains));

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(responseJson, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.ListCustomDomainsAsync(bucketName);

    // Assert
    result.Should().HaveCount(2);
    result[0].Domain.Should().Be("cdn.example.com");
    result[1].Domain.Should().Be("media.example.com");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/domains/custom");
  }

  /// <summary>Verifies that AttachCustomDomainAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task AttachCustomDomainAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName      = "test-bucket";
    var hostname        = "r2.example.com";
    var zoneId          = "test-zone-id";
    var expectedResult  = new CustomDomainResponse(hostname, null, "pending_validation");
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.AttachCustomDomainAsync(bucketName, hostname, zoneId);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/domains/custom");
    capturedJsonBody.Should().NotBeNull();
    var content = JsonSerializer.Deserialize<AttachCustomDomainRequest>(capturedJsonBody!, _serializerOptions);
    content.Should().BeEquivalentTo(new AttachCustomDomainRequest(hostname, true, zoneId));
  }

  /// <summary>Verifies that GetCustomDomainStatusAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetCustomDomainStatusAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var hostname   = "r2.example.com";
    var responseBody =
      $@"{{
          ""success"": true,
          ""errors"": [],
          ""messages"": [],
          ""result"": {{
            ""domain"": ""{hostname}"",
            ""status"": {{
              ""ownership"": ""active"",
              ""ssl"": ""active""
            }},
            ""edgeHostname"": ""{hostname}.cdn.cloudflare.net""
          }}
        }}";

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(responseBody, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetCustomDomainStatusAsync(bucketName, hostname);

    // Assert
    result.Should().BeEquivalentTo(new CustomDomainResponse(hostname, $"{hostname}.cdn.cloudflare.net", "active"));
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/domains/custom/{hostname}");
  }

  /// <summary>Verifies that UpdateCustomDomainAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task UpdateCustomDomainAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var hostname   = "r2.example.com";
    var request    = new UpdateCustomDomainRequest(true, "1.2");
    // CustomDomainResponse uses a custom converter that expects camelCase property names
    var responseBody =
      $@"{{
          ""success"": true,
          ""errors"": [],
          ""messages"": [],
          ""result"": {{
            ""domain"": ""{hostname}"",
            ""status"": ""active"",
            ""edgeHostname"": ""{hostname}.cdn.cloudflare.net""
          }}
        }}";

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(responseBody, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.UpdateCustomDomainAsync(bucketName, hostname, request);

    // Assert
    result.Domain.Should().Be(hostname);
    result.Status.Should().Be("active");
    result.EdgeHostname.Should().Be($"{hostname}.cdn.cloudflare.net");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/domains/custom/{hostname}");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<UpdateCustomDomainRequest>();
    content.Should().BeEquivalentTo(request);
  }

  /// <summary>Verifies that DetachCustomDomainAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DetachCustomDomainAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName      = "test-bucket";
    var hostname        = "r2.example.com";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.DetachCustomDomainAsync(bucketName, hostname);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/domains/custom/{hostname}");
  }

  #endregion


  #region Managed Domain (r2.dev) Operations

  /// <summary>Verifies that GetManagedDomainAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetManagedDomainAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName      = "test-bucket";
    var expectedResult  = new ManagedDomainResponse("bucket-id-123", "test-bucket.12345.r2.dev", true);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetManagedDomainAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/domains/managed");
  }

  /// <summary>Verifies that EnableManagedDomainAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task EnableManagedDomainAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName      = "test-bucket";
    var expectedResult  = new ManagedDomainResponse("bucket-id-123", "test-bucket.12345.r2.dev", true);
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.EnableManagedDomainAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/domains/managed");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<SetManagedDomainRequest>();
    content.Should().BeEquivalentTo(new SetManagedDomainRequest(true));
  }

  /// <summary>Verifies that DisableManagedDomainAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task DisableManagedDomainAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName      = "test-bucket";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.DisableManagedDomainAsync(bucketName);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/domains/managed");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<SetManagedDomainRequest>();
    content.Should().BeEquivalentTo(new SetManagedDomainRequest(false));
  }

  #endregion


  #region Bucket Lock Operations

  /// <summary>Verifies that GetLockAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetLockAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var lockRule = new BucketLockRule(
      "rule-1",
      true,
      "important/",
      BucketLockCondition.ForDays(30)
    );
    var expectedResult  = new BucketLockPolicy(new[] { lockRule });
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetLockAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/lock");
  }

  /// <summary>Verifies that SetLockAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task SetLockAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var lockPolicy = new BucketLockPolicy(
      new[]
      {
        new BucketLockRule("rule-1", true, "legal/", BucketLockCondition.ForDays(365)),
        new BucketLockRule("rule-2", true, "compliance/", BucketLockCondition.Indefinitely())
      }
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(lockPolicy);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.SetLockAsync(bucketName, lockPolicy);

    // Assert
    result.Should().BeEquivalentTo(lockPolicy);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/lock");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<BucketLockPolicy>();
    content.Should().BeEquivalentTo(lockPolicy);
  }

  /// <summary>Verifies that DeleteLockAsync sends a PUT request with an empty rules array.</summary>
  [Fact]
  public async Task DeleteLockAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var emptyPolicy = new BucketLockPolicy(Array.Empty<BucketLockRule>());
    var successResponse = HttpFixtures.CreateSuccessResponse(emptyPolicy);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.DeleteLockAsync(bucketName);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/lock");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<BucketLockPolicy>();
    content.Should().NotBeNull();
    content!.Rules.Should().BeEmpty();
  }

  #endregion


  #region Sippy (Incremental Migration) Operations

  /// <summary>Verifies that GetSippyAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetSippyAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var expectedResult = new SippyConfig(
      true,
      new SippySourceInfo(SippyProvider.Aws, "source-bucket", "https://s3.us-east-1.amazonaws.com/source-bucket", "us-east-1"),
      new SippyDestination(SippyProvider.R2, "test-bucket", TestAccountId)
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.GetSippyAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/sippy");
  }

  /// <summary>Verifies that EnableSippyAsync with AWS source sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task EnableSippyAsync_WithAwsSource_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var awsSource = SippyAwsSource.Create("source-bucket", "us-east-1", "AKIAEXAMPLE", "secret-key");
    var request = new EnableSippyFromAwsRequest(awsSource);
    var expectedResult = new SippyConfig(
      true,
      new SippySourceInfo(SippyProvider.Aws, "source-bucket", "https://s3.us-east-1.amazonaws.com/source-bucket", "us-east-1"),
      new SippyDestination(SippyProvider.R2, "test-bucket", TestAccountId)
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.EnableSippyAsync(bucketName, request);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/sippy");
  }

  /// <summary>Verifies that EnableSippyAsync with GCS source sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task EnableSippyAsync_WithGcsSource_SendsCorrectRequest()
  {
    // Arrange
    var bucketName = "test-bucket";
    var gcsSource = SippyGcsSource.Create("gcs-source-bucket", "service@project.iam.gserviceaccount.com", "-----BEGIN PRIVATE KEY-----...");
    var request = new EnableSippyFromGcsRequest(gcsSource);
    var expectedResult = new SippyConfig(
      true,
      new SippySourceInfo(SippyProvider.Gcs, "gcs-source-bucket", "https://storage.googleapis.com/gcs-source-bucket"),
      new SippyDestination(SippyProvider.R2, "test-bucket", TestAccountId)
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.EnableSippyAsync(bucketName, request);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/sippy");
  }

  /// <summary>Verifies that DisableSippyAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DisableSippyAsync_SendsCorrectRequest()
  {
    // Arrange
    var bucketName      = "test-bucket";
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    await sut.DisableSippyAsync(bucketName);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/sippy");
  }

  #endregion


  #region Temporary Credentials Operations

  /// <summary>Verifies that CreateTempCredentialsAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateTempCredentialsAsync_SendsCorrectRequest()
  {
    // Arrange
    var request = new CreateTempCredentialsRequest(
      "test-bucket",
      "parent-access-key-id",
      TempCredentialPermission.ObjectReadWrite,
      3600
    );
    var expectedResult = new TempCredentials(
      "temp-access-key-id",
      "temp-secret-access-key",
      "session-token"
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.CreateTempCredentialsAsync(request);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/temp-access-credentials");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<CreateTempCredentialsRequest>();
    content.Should().BeEquivalentTo(request);
  }

  /// <summary>Verifies that CreateTempCredentialsAsync with prefixes sends a correctly formatted request.</summary>
  [Fact]
  public async Task CreateTempCredentialsAsync_WithPrefixes_SendsCorrectRequest()
  {
    // Arrange
    var request = new CreateTempCredentialsRequest(
      "test-bucket",
      "parent-access-key-id",
      TempCredentialPermission.ObjectReadOnly,
      1800,
      Prefixes: new[] { "uploads/", "public/" }
    );
    var expectedResult = new TempCredentials(
      "temp-access-key-id",
      "temp-secret-access-key",
      "session-token"
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.CreateTempCredentialsAsync(request);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("prefixes").GetArrayLength().Should().Be(2);
  }

  /// <summary>Verifies that CreateTempCredentialsAsync with objects sends a correctly formatted request.</summary>
  [Fact]
  public async Task CreateTempCredentialsAsync_WithObjects_SendsCorrectRequest()
  {
    // Arrange
    var request = new CreateTempCredentialsRequest(
      "test-bucket",
      "parent-access-key-id",
      TempCredentialPermission.ObjectWriteOnly,
      900,
      Objects: new[] { "file1.txt", "file2.pdf", "folder/file3.json" }
    );
    var expectedResult = new TempCredentials(
      "temp-access-key-id",
      "temp-secret-access-key",
      "session-token"
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);

    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var sut = CreateSut(successResponse, callback: (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    // Act
    var result = await sut.CreateTempCredentialsAsync(request);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("objects").GetArrayLength().Should().Be(3);
  }

  #endregion


  #region Error Handling

  /// <summary>Verifies that API errors are properly propagated as CloudflareApiException.</summary>
  [Fact]
  public async Task CreateAsync_WhenApiReturnsError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10000, "Authentication error");
    var sut           = CreateSut(errorResponse);

    // Act
    var action = async () => await sut.CreateAsync("test-bucket");

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareApiException>();
    ex.Which.Message.Should().Contain("10000");
    ex.Which.Errors.Should().ContainSingle().Which.Should().BeEquivalentTo(new ApiError(10000, "Authentication error"));
  }

  /// <summary>Verifies that DeleteAsync propagates API errors correctly.</summary>
  [Fact]
  public async Task DeleteAsync_WhenApiReturnsError_ThrowsCloudflareApiException()
  {
    // Arrange
    var errorResponse = HttpFixtures.CreateErrorResponse(10006, "Bucket not empty");
    var sut           = CreateSut(errorResponse);

    // Act
    var action = async () => await sut.DeleteAsync("non-empty-bucket");

    // Assert
    var ex = await action.Should().ThrowAsync<CloudflareApiException>();
    ex.Which.Errors.Should().ContainSingle().Which.Code.Should().Be(10006);
  }

  #endregion
}
