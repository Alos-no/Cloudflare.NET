namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using NET.Security.Firewall.Models;
using Shared.Fixtures;
using Xunit.Abstractions;
using Zones.CustomHostnames;
using Zones.CustomHostnames.Models;

/// <summary>Contains unit tests for the <see cref="CustomHostnamesApi" /> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class CustomHostnamesApiUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly ILoggerFactory _loggerFactory;

  private readonly JsonSerializerOptions _serializerOptions =
    new()
    {
      PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

  #endregion

  #region Constructors

  public CustomHostnamesApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion

  #region Methods

  #region Tests - Get

  /// <summary>Verifies that GetAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId           = "test-zone-id";
    var customHostnameId = "ch-12345";
    var hostname         = "app.customer.com";

    var                 expectedResult  = CreateSampleCustomHostname(hostname, customHostnameId);
    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new CustomHostnamesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetAsync(zoneId, customHostnameId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(customHostnameId);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/custom_hostnames/{customHostnameId}");
  }

  #endregion


  #region Tests - Update

  /// <summary>Verifies that UpdateAsync sends a correctly formatted PATCH request.</summary>
  [Fact]
  public async Task UpdateAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId           = "test-zone-id";
    var customHostnameId = "ch-12345";
    var hostname         = "app.customer.com";
    var request = new UpdateCustomHostnameRequest(
      new SslConfiguration(DcvMethod.Txt, CertificateType.Dv));

    var                 expectedResult  = CreateSampleCustomHostname(hostname, customHostnameId);
    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new CustomHostnamesApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateAsync(zoneId, customHostnameId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/custom_hostnames/{customHostnameId}");

    var content = await capturedRequest.Content!.ReadAsStringAsync();
    content.Should().Contain("\"method\":\"txt\"");
  }

  #endregion


  #region Tests - Delete

  /// <summary>Verifies that DeleteAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DeleteAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId           = "test-zone-id";
    var customHostnameId = "ch-12345";

    var                 successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new CustomHostnamesApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteAsync(zoneId, customHostnameId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/custom_hostnames/{customHostnameId}");
  }

  #endregion

  /// <summary>Creates a sample CustomHostname object for testing.</summary>
  private static CustomHostname CreateSampleCustomHostname(string hostname, string id = "ch-test-id") =>
    new(
      id,
      hostname,
      CustomHostnameStatus.Pending,
      new SslResponse(
        "ssl-123",
        SslStatus.PendingValidation,
        DcvMethod.Http,
        CertificateType.Dv),
      new OwnershipVerification("txt", $"_cf-custom-hostname.{hostname}", "verification-token"),
      null,
      null,
      null,
      null,
      null,
      DateTimeOffset.UtcNow);

  #endregion


  #region Tests - Create

  /// <summary>Verifies that CreateAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId   = "test-zone-id";
    var hostname = "app.customer.com";
    var request = new CreateCustomHostnameRequest(
      hostname,
      new SslConfiguration(DcvMethod.Http, CertificateType.Dv));

    var                 expectedResult  = CreateSampleCustomHostname(hostname);
    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new CustomHostnamesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.CreateAsync(zoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Hostname.Should().Be(hostname);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/custom_hostnames");

    var content = await capturedRequest.Content!.ReadAsStringAsync();
    content.Should().Contain("\"hostname\":\"app.customer.com\"");
    content.Should().Contain("\"method\":\"http\"");
    content.Should().Contain("\"type\":\"dv\"");
  }

  /// <summary>Verifies that CreateAsync correctly serializes custom origin settings.</summary>
  [Fact]
  public async Task CreateAsync_WithCustomOrigin_SendsCorrectRequest()
  {
    // Arrange
    var zoneId   = "test-zone-id";
    var hostname = "app.customer.com";
    var request = new CreateCustomHostnameRequest(
      hostname,
      new SslConfiguration(DcvMethod.Http, CertificateType.Dv),
      CustomOriginServer: "origin.myservice.com",
      CustomOriginSni: CustomHostnameConstants.Sni.UseRequestHostHeader);

    var                 expectedResult  = CreateSampleCustomHostname(hostname);
    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new CustomHostnamesApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateAsync(zoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    var content = await capturedRequest!.Content!.ReadAsStringAsync();
    content.Should().Contain("\"custom_origin_server\":\"origin.myservice.com\"");
    content.Should().Contain("\"custom_origin_sni\":\":request_host_header:\"");
  }

  #endregion


  #region Tests - List

  /// <summary>Verifies that ListAsync sends a correctly formatted GET request with no filters.</summary>
  [Fact]
  public async Task ListAsync_WithNoFilters_SendsCorrectRequest()
  {
    // Arrange
    var zoneId = "test-zone-id";

    var                 successResponse = HttpFixtures.CreateSuccessResponse(Array.Empty<CustomHostname>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new CustomHostnamesApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAsync(zoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/custom_hostnames");
  }

  /// <summary>Verifies that ListAsync correctly constructs the query string with all filters.</summary>
  [Fact]
  public async Task ListAsync_WithFilters_ConstructsCorrectQueryString()
  {
    // Arrange
    var zoneId = "test-zone-id";
    var filters = new ListCustomHostnamesFilters(
      "ch-filter-id",
      "customer.com",
      SslStatus.Active,
      "hostname",
      ListOrderDirection.Ascending,
      2,
      25);

    var                 successResponse = HttpFixtures.CreateSuccessResponse(Array.Empty<CustomHostname>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new CustomHostnamesApi(httpClient, _loggerFactory);

    // Act
    await sut.ListAsync(zoneId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.ToString();
    uri.Should().Contain("id=ch-filter-id");
    uri.Should().Contain("hostname=customer.com");
    uri.Should().Contain("ssl=active");
    uri.Should().Contain("order=hostname");
    uri.Should().Contain("direction=asc");
    uri.Should().Contain("page=2");
    uri.Should().Contain("per_page=25");
  }

  /// <summary>Verifies that ListAllAsync handles pagination correctly.</summary>
  [Fact]
  public async Task ListAllAsync_ShouldHandlePaginationCorrectly()
  {
    // Arrange
    var zoneId    = "test-zone-id";
    var hostname1 = CreateSampleCustomHostname("app1.customer.com", "ch-1");
    var hostname2 = CreateSampleCustomHostname("app2.customer.com", "ch-2");

    // First page response.
    var responsePage1 =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new[] { hostname1 },
          result_info = new { page = 1, per_page = 1, count = 1, total_pages = 2, total_count = 2 }
        },
        _serializerOptions);

    // Second page response.
    var responsePage2 =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new[] { hostname2 },
          result_info = new { page = 2, per_page = 1, count = 1, total_pages = 2, total_count = 2 }
        },
        _serializerOptions);

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
    var sut        = new CustomHostnamesApi(httpClient, _loggerFactory);

    // Act
    var allHostnames = new List<CustomHostname>();
    await foreach (var hostname in sut.ListAllAsync(zoneId, new ListCustomHostnamesFilters(PerPage: 1)))
      allHostnames.Add(hostname);

    // Assert
    capturedRequests.Should().HaveCount(2);
    capturedRequests[0].RequestUri!.Query.Should().Contain("page=1");
    capturedRequests[1].RequestUri!.Query.Should().Contain("page=2");
    allHostnames.Should().HaveCount(2);
    allHostnames.Select(h => h.Id).Should().ContainInOrder("ch-1", "ch-2");
  }

  #endregion


  #region Tests - Fallback Origin

  /// <summary>Verifies that GetFallbackOriginAsync sends a correctly formatted GET request.</summary>
  [Fact]
  public async Task GetFallbackOriginAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId = "test-zone-id";

    var                 expectedResult  = new FallbackOrigin("fallback.myservice.com", "active");
    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new CustomHostnamesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetFallbackOriginAsync(zoneId);

    // Assert
    result.Should().NotBeNull();
    result.Origin.Should().Be("fallback.myservice.com");
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/custom_hostnames/fallback_origin");
  }

  /// <summary>Verifies that UpdateFallbackOriginAsync sends a correctly formatted PUT request.</summary>
  [Fact]
  public async Task UpdateFallbackOriginAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId  = "test-zone-id";
    var request = new UpdateFallbackOriginRequest("new-fallback.myservice.com");

    var                 expectedResult  = new FallbackOrigin("new-fallback.myservice.com", "initializing");
    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new CustomHostnamesApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateFallbackOriginAsync(zoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/custom_hostnames/fallback_origin");

    var content = await capturedRequest.Content!.ReadAsStringAsync();
    content.Should().Contain("\"origin\":\"new-fallback.myservice.com\"");
  }

  /// <summary>Verifies that DeleteFallbackOriginAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DeleteFallbackOriginAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId = "test-zone-id";

    var                 successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new CustomHostnamesApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteFallbackOriginAsync(zoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/custom_hostnames/fallback_origin");
  }

  #endregion
}
