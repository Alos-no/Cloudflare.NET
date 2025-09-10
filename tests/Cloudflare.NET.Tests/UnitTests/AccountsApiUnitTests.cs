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
    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

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
    var content = await capturedRequest.Content!.ReadFromJsonAsync<CreateBucketRequest>();
    content.Should().NotBeNull();
    content.Name.Should().Be(bucketName);
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

  #endregion
}
