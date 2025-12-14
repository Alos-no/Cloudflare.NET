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
///     <item><description>Core bucket CRUD operations (Create, Get, List, Delete)</description></item>
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

  /// <summary>Verifies that ListAsync constructs the correct URI with all filters applied.</summary>
  [Fact]
  public async Task ListAsync_ShouldConstructCorrectRequestUri_WithAllFilters()
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

    HttpRequestMessage? capturedRequest = null;
    var sut = CreateSut(successResponse, callback: (req, _) => capturedRequest = req);

    // Act
    var result = await sut.AttachCustomDomainAsync(bucketName, hostname, zoneId);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{TestAccountId}/r2/buckets/{bucketName}/domains/custom");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<AttachCustomDomainRequest>();
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
