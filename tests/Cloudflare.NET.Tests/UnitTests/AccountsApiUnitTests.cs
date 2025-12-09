namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Accounts;
using Accounts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq.Protected;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>Contains unit tests for the <see cref="AccountsApi" /> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class AccountsApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;
  private readonly JsonSerializerOptions _serializerOptions =
    new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

  #endregion

  #region Constructors

  public AccountsApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion

  #region Methods

  /// <summary>Verifies that CreateR2BucketAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateR2BucketAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";

    var expectedResult =
      new R2Bucket(bucketName, DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(), "wnam", "eu", "Standard");
    var                 successResponse  = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.CreateR2BucketAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets");

    // Verify JSON body contains only the bucket name
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("name").GetString().Should().Be(bucketName);
  }

  /// <summary>Verifies that CreateR2BucketAsync with location hint sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateR2BucketAsync_WithLocationHint_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "eu-bucket";

    var expectedResult = new R2Bucket(
      bucketName,
      DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(),
      R2LocationHint.WestEurope,
      null,
      R2StorageClass.Standard
    );
    var                 successResponse  = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.CreateR2BucketAsync(bucketName, R2LocationHint.WestEurope);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets");

    // Verify JSON contains the location hint
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("name").GetString().Should().Be(bucketName);
    doc.RootElement.GetProperty("locationHint").GetString().Should().Be("weur");
  }

  /// <summary>Verifies that CreateR2BucketAsync with all options sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateR2BucketAsync_WithAllOptions_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "gdpr-bucket";

    var expectedResult = new R2Bucket(
      bucketName,
      DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(),
      R2LocationHint.WestEurope,
      R2Jurisdiction.EuropeanUnion,
      R2StorageClass.InfrequentAccess
    );
    var                 successResponse  = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.CreateR2BucketAsync(
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

  /// <summary>Verifies that CreateR2BucketAsync without jurisdiction does NOT send the header.</summary>
  [Fact]
  public async Task CreateR2BucketAsync_WithoutJurisdiction_DoesNotSendHeader()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "simple-bucket";

    var expectedResult = new R2Bucket(
      bucketName,
      DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(),
      R2LocationHint.EastNorthAmerica,
      null,
      R2StorageClass.Standard
    );
    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.CreateR2BucketAsync(
      bucketName,
      locationHint: R2LocationHint.EastNorthAmerica
    );

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();

    // Verify jurisdiction header is NOT present when jurisdiction is null
    capturedRequest!.Headers.TryGetValues("cf-r2-jurisdiction", out _).Should().BeFalse(
      "jurisdiction header should not be sent when jurisdiction is null");
  }

  /// <summary>Verifies that CreateR2BucketAsync handles custom/unknown extensible enum values.</summary>
  [Fact]
  public async Task CreateR2BucketAsync_WithCustomExtensibleEnumValues_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "future-bucket";

    // Simulate using future values that the API might support before the SDK is updated
    R2LocationHint customLocation    = "mars-colony-alpha";
    R2Jurisdiction customJurisdiction = "space-treaty-2050";
    R2StorageClass customStorageClass = "CryogenicStorage";

    var expectedResult = new R2Bucket(
      bucketName,
      DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(),
      customLocation,
      customJurisdiction,
      customStorageClass
    );
    var                 successResponse  = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.CreateR2BucketAsync(
      bucketName,
      locationHint: customLocation,
      jurisdiction: customJurisdiction,
      storageClass: customStorageClass
    );

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();

    // Verify custom location hint and storage class are serialized correctly in body
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("name").GetString().Should().Be(bucketName);
    doc.RootElement.GetProperty("locationHint").GetString().Should().Be("mars-colony-alpha");
    doc.RootElement.GetProperty("storageClass").GetString().Should().Be("CryogenicStorage");

    // Verify custom jurisdiction is passed as HTTP header
    capturedRequest!.Headers.TryGetValues("cf-r2-jurisdiction", out var jurisdictionValues).Should().BeTrue();
    jurisdictionValues.Should().ContainSingle().Which.Should().Be("space-treaty-2050");
  }

  /// <summary>Verifies that CreateR2BucketAsync with only location hint (no jurisdiction) sends correct request.</summary>
  [Fact]
  public async Task CreateR2BucketAsync_WithOnlyLocationHint_OmitsStorageClassFromBody()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "location-only-bucket";

    var expectedResult = new R2Bucket(
      bucketName,
      DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(),
      R2LocationHint.AsiaPacific,
      null,
      null
    );
    var                 successResponse  = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.CreateR2BucketAsync(
      bucketName,
      locationHint: R2LocationHint.AsiaPacific
    );

    // Assert
    result.Should().BeEquivalentTo(expectedResult);

    // Verify JSON body contains only name and locationHint (null values omitted)
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("name").GetString().Should().Be(bucketName);
    doc.RootElement.GetProperty("locationHint").GetString().Should().Be("apac");
    doc.RootElement.TryGetProperty("storageClass", out _).Should().BeFalse(
      "storageClass should be omitted when null");

    // Verify no jurisdiction header
    capturedRequest!.Headers.TryGetValues("cf-r2-jurisdiction", out _).Should().BeFalse();
  }

  /// <summary>Verifies that CreateR2BucketAsync with only storage class sends correct request.</summary>
  [Fact]
  public async Task CreateR2BucketAsync_WithOnlyStorageClass_OmitsLocationHintFromBody()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "storage-only-bucket";

    var expectedResult = new R2Bucket(
      bucketName,
      DateTime.Parse("2024-01-01T00:00:00Z").ToUniversalTime(),
      null,
      null,
      R2StorageClass.InfrequentAccess
    );
    var                 successResponse  = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest  = req;
      capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.CreateR2BucketAsync(
      bucketName,
      locationHint: null,
      jurisdiction: null,
      storageClass: R2StorageClass.InfrequentAccess
    );

    // Assert
    result.Should().BeEquivalentTo(expectedResult);

    // Verify JSON body contains only name and storageClass (null values omitted)
    capturedJsonBody.Should().NotBeNull();
    using var doc = JsonDocument.Parse(capturedJsonBody!);
    doc.RootElement.GetProperty("name").GetString().Should().Be(bucketName);
    doc.RootElement.GetProperty("storageClass").GetString().Should().Be("InfrequentAccess");
    doc.RootElement.TryGetProperty("locationHint", out _).Should().BeFalse(
      "locationHint should be omitted when null");

    // Verify no jurisdiction header
    capturedRequest!.Headers.TryGetValues("cf-r2-jurisdiction", out _).Should().BeFalse();
  }

  /// <summary>Verifies that DeleteR2BucketAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DeleteR2BucketAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket-to-delete";

    var                 successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.DeleteR2BucketAsync(bucketName);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}");
  }

  /// <summary>Verifies that ListR2BucketsAsync constructs the correct URI with no filters.</summary>
  [Fact]
  public async Task ListR2BucketsAsync_ShouldConstructCorrectRequestUri_WithNoFilters()
  {
    // Arrange
    var                 accountId       = "test-account-id";
    var                 successResponse = HttpFixtures.CreateSuccessResponse(new ListR2BucketsResponse(Array.Empty<R2Bucket>()));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.ListR2BucketsAsync();

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets");
  }

  /// <summary>Verifies that ListR2BucketsAsync constructs the correct URI with all filters applied.</summary>
  [Fact]
  public async Task ListR2BucketsAsync_ShouldConstructCorrectRequestUri_WithAllFilters()
  {
    // Arrange
    var                 accountId       = "test-account-id";
    var                 filters         = new ListR2BucketsFilters(50, "abc123def");
    var                 successResponse = HttpFixtures.CreateSuccessResponse(new ListR2BucketsResponse(Array.Empty<R2Bucket>()));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.ListR2BucketsAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should()
                    .Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets?per_page=50&cursor=abc123def");
  }

  /// <summary>Verifies that ListAllR2BucketsAsync handles pagination correctly.</summary>
  [Fact]
  public async Task ListAllR2BucketsAsync_ShouldHandlePaginationCorrectly()
  {
    // Arrange
    var accountId = "test-account-id";
    var bucket1   = new R2Bucket("bucket1", DateTime.UtcNow, "loc", "jur", "class");
    var bucket2   = new R2Bucket("bucket2", DateTime.UtcNow, "loc", "jur", "class");
    var cursor    = "next_page_cursor";

    // First page response with a cursor, matching the nested "buckets" structure.
    var responsePage1 =
      JsonSerializer.Serialize(
        new
        {
          success            = true,
          errors             = Array.Empty<object>(),
          messages           = Array.Empty<object>(),
          result             = new { buckets = new[] { bucket1 } },
          cursor_result_info = new { count   = 1, per_page = 1, cursor }
        },
        _serializerOptions);

    // Second page response without a cursor, matching the nested "buckets" structure.
    var responsePage2 =
      JsonSerializer.Serialize(
        new
        {
          success            = true,
          errors             = Array.Empty<object>(),
          messages           = Array.Empty<object>(),
          result             = new { buckets = new[] { bucket2 } },
          cursor_result_info = new { count   = 1, per_page = 1, cursor = (string?)null }
        },
        _serializerOptions);

    var capturedRequests = new List<HttpRequestMessage>();
    var mockHandler      = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage1) })
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(responsePage2) });

    // Capture each request
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
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var allBuckets = new List<R2Bucket>();
    await foreach (var bucket in sut.ListAllR2BucketsAsync())
      allBuckets.Add(bucket);

    // Assert
    capturedRequests.Should().HaveCount(2);
    capturedRequests[0].RequestUri!.Query.Should().NotContain("cursor");
    capturedRequests[1].RequestUri!.Query.Should().Contain($"cursor={cursor}");
    allBuckets.Should().HaveCount(2);
    allBuckets.Select(b => b.Name).Should().ContainInOrder("bucket1", "bucket2");
  }

  /// <summary>Verifies that AttachCustomDomainAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task AttachCustomDomainAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";
    var hostname   = "r2.example.com";
    var zoneId     = "test-zone-id";

    // The initial POST response does not include an EdgeHostname. It is null.
    var                 expectedResult  = new CustomDomainResponse(hostname, null, "pending_validation");
    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.AttachCustomDomainAsync(bucketName, hostname, zoneId);

    // Assert
    // This will now pass as the expected EdgeHostname is correctly null.
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}/domains/custom");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<AttachCustomDomainRequest>();
    content.Should().BeEquivalentTo(new AttachCustomDomainRequest(hostname, true, zoneId));
  }

  /// <summary>Verifies that DisableDevUrlAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task DisableDevUrlAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";

    var                 successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.DisableDevUrlAsync(bucketName);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}/domains/managed");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<SetManagedDomainRequest>();
    content.Should().BeEquivalentTo(new SetManagedDomainRequest(false));
  }

  /// <summary>Verifies that GetCustomDomainStatusAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetCustomDomainStatusAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";
    var hostname   = "r2.example.com";

    // The real API response for an existing domain is more complex, so we mimic that structure for an accurate test.
    // This specifically tests the CustomDomainResponseConverter's ability to parse the nested status object.
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
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(responseBody, HttpStatusCode.OK, (req, _) => capturedRequest = req);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.GetCustomDomainStatusAsync(bucketName, hostname);

    // Assert
    result.Should()
          .BeEquivalentTo(new CustomDomainResponse(hostname, $"{hostname}.cdn.cloudflare.net", "active"));
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}/domains/custom/{hostname}");
  }

  /// <summary>Verifies that DetachCustomDomainAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DetachCustomDomainAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";
    var hostname   = "r2.example.com";

    var                 successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.DetachCustomDomainAsync(bucketName, hostname);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}/domains/custom/{hostname}");
  }

  /// <summary>Verifies that GetBucketCorsAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetBucketCorsAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";

    var corsRule = new CorsRule(
      new CorsAllowed(
        new[] { "GET", "PUT" },
        new[] { "https://example.com" },
        new[] { "Content-Type" }
      ),
      "Allow Example",
      new[] { "ETag" },
      3600
    );

    var                 expectedResult  = new BucketCorsPolicy(new[] { corsRule });
    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.GetBucketCorsAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}/cors");
  }

  /// <summary>Verifies that SetBucketCorsAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task SetBucketCorsAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";

    var corsPolicy = new BucketCorsPolicy(
      new[]
      {
        new CorsRule(
          new CorsAllowed(
            new[] { "GET", "PUT", "POST" },
            new[] { "https://example.com", "https://app.example.com" },
            new[] { "Content-Type", "Authorization" }
          ),
          "Production CORS",
          new[] { "ETag", "Content-Length" },
          7200
        )
      }
    );

    var                 successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.SetBucketCorsAsync(bucketName, corsPolicy);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}/cors");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<BucketCorsPolicy>();
    content.Should().BeEquivalentTo(corsPolicy);
  }

  /// <summary>Verifies that DeleteBucketCorsAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DeleteBucketCorsAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";

    var                 successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.DeleteBucketCorsAsync(bucketName);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}/cors");
  }

  /// <summary>Verifies that GetBucketLifecycleAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetBucketLifecycleAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";

    var lifecycleRule = new LifecycleRule(
      "Delete old logs",
      true,
      new LifecycleRuleConditions("logs/"),
      DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(90)),
      AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(LifecycleCondition.AfterDays(7)),
      StorageClassTransitions: new[]
      {
        new StorageClassTransition(LifecycleCondition.AfterDays(30), R2StorageClass.InfrequentAccess)
      }
    );

    var                 expectedResult  = new BucketLifecyclePolicy(new[] { lifecycleRule });
    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    var result = await sut.GetBucketLifecycleAsync(bucketName);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}/lifecycle");
  }

  /// <summary>Verifies that SetBucketLifecycleAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task SetBucketLifecycleAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";

    var lifecyclePolicy = new BucketLifecyclePolicy(
      new[]
      {
        // Rule to delete objects after 90 days
        new LifecycleRule(
          "Delete old objects",
          true,
          new LifecycleRuleConditions("temp/"),
          DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(90))
        ),
        // Rule to abort incomplete multipart uploads after 7 days
        new LifecycleRule(
          "Cleanup multipart",
          true,
          AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(LifecycleCondition.AfterDays(7))
        ),
        // Rule to transition to Infrequent Access after 30 days
        new LifecycleRule(
          "Archive old data",
          true,
          new LifecycleRuleConditions("archive/"),
          StorageClassTransitions: new[]
          {
            new StorageClassTransition(LifecycleCondition.AfterDays(30), R2StorageClass.InfrequentAccess)
          }
        )
      }
    );

    var                 successResponse  = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
      {
        capturedRequest  = req;
        capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
      });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.SetBucketLifecycleAsync(bucketName, lifecyclePolicy);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}/lifecycle");
    capturedJsonBody.Should().NotBeNullOrEmpty();

    // The SDK normalizes null Conditions to empty LifecycleRuleConditions objects to satisfy the Cloudflare R2 API
    // requirement that the 'conditions' field be present in each rule, even if empty.
    var content = JsonSerializer.Deserialize<BucketLifecyclePolicy>(capturedJsonBody!);
    content.Should().NotBeNull();
    content!.Rules.Should().HaveCount(3);

    // First rule: has explicit conditions with prefix
    content.Rules[0].Id.Should().Be("Delete old objects");
    content.Rules[0].Conditions.Should().NotBeNull();
    content.Rules[0].Conditions!.Prefix.Should().Be("temp/");

    // Second rule: had null conditions, now normalized to empty LifecycleRuleConditions
    content.Rules[1].Id.Should().Be("Cleanup multipart");
    content.Rules[1].Conditions.Should().NotBeNull("SDK normalizes null conditions to empty conditions");
    content.Rules[1].Conditions!.Prefix.Should().BeNull();

    // Third rule: has explicit conditions with prefix
    content.Rules[2].Id.Should().Be("Archive old data");
    content.Rules[2].Conditions.Should().NotBeNull();
    content.Rules[2].Conditions!.Prefix.Should().Be("archive/");
  }

  /// <summary>Verifies that DeleteBucketLifecycleAsync sends a PUT request with an empty rules array.</summary>
  /// <remarks>
  ///   Cloudflare R2 does not have a dedicated DELETE endpoint for lifecycle policies. Instead, the policy is removed
  ///   by setting an empty rules array via PUT.
  /// </remarks>
  [Fact]
  public async Task DeleteBucketLifecycleAsync_SendsCorrectRequest()
  {
    // Arrange
    var accountId  = "test-account-id";
    var bucketName = "test-bucket";

    var                 successResponse  = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest  = null;
    string?             capturedJsonBody = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
      {
        capturedRequest  = req;
        capturedJsonBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
      });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var options    = Options.Create(new CloudflareApiOptions { AccountId = accountId });
    var sut        = new AccountsApi(httpClient, options, _loggerFactory);

    // Act
    await sut.DeleteBucketLifecycleAsync(bucketName);

    // Assert - Cloudflare R2 uses PUT with empty rules array to clear lifecycle policy
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/accounts/{accountId}/r2/buckets/{bucketName}/lifecycle");
    capturedJsonBody.Should().NotBeNullOrEmpty();
    var content = JsonSerializer.Deserialize<BucketLifecyclePolicy>(capturedJsonBody!);
    content.Should().NotBeNull();
    content!.Rules.Should().BeEmpty();
  }

  #endregion
}
