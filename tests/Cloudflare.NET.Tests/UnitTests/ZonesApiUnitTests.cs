namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using NET.Security.Firewall.Models;
using Shared.Fixtures;
using Xunit.Abstractions;
using Zones;
using Zones.Models;

/// <summary>Contains unit tests for the <see cref="ZonesApi" /> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class ZonesApiUnitTests
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

  public ZonesApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion

  #region Methods

  /// <summary>Verifies that CreateCnameRecordAsync sends a correctly formatted POST request.</summary>
  [Fact]
  public async Task CreateCnameRecordAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId      = "test-zone-id";
    var hostname    = "test.example.com";
    var cnameTarget = "target.example.com";

    var                 expectedResult  = new DnsRecord("dns-record-id", hostname, "CNAME");
    var                 successResponse = HttpFixtures.CreateSuccessResponse(expectedResult);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.CreateCnameRecordAsync(zoneId, hostname, cnameTarget);

    // Assert
    result.Should().BeEquivalentTo(expectedResult);
    capturedRequest.Should().NotBeNull();
    capturedRequest.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records");
    var content = await capturedRequest.Content!.ReadFromJsonAsync<CreateDnsRecordRequest>();
    content.Should().BeEquivalentTo(new CreateDnsRecordRequest("CNAME", hostname, cnameTarget, 300, true));
  }

  /// <summary>Verifies that DeleteDnsRecordAsync sends a correctly formatted DELETE request.</summary>
  [Fact]
  public async Task DeleteDnsRecordAsync_SendsCorrectRequest()
  {
    // Arrange
    var zoneId   = "test-zone-id";
    var recordId = "dns-record-to-delete-id";

    var                 successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteDnsRecordAsync(zoneId, recordId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records/{recordId}");
  }

  /// <summary>Verifies that FindDnsRecordByNameAsync sends a correctly formatted GET request and returns the first result.</summary>
  [Fact]
  public async Task FindDnsRecordByNameAsync_SendsCorrectRequestAndReturnsFirstRecord()
  {
    // Arrange
    var zoneId   = "test-zone-id";
    var hostname = "findme.example.com";

    var                 record1         = new DnsRecord("id-1", hostname, "A");
    var                 record2         = new DnsRecord("id-2", hostname, "AAAA");
    var                 successResponse = HttpFixtures.CreateSuccessResponse(new[] { record1, record2 });
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.FindDnsRecordByNameAsync(zoneId, hostname);

    // Assert
    result.Should().BeEquivalentTo(record1);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should()
                    .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records?name={hostname}");
  }

  /// <summary>Verifies that FindDnsRecordByNameAsync returns null when no record is found.</summary>
  [Fact]
  public async Task FindDnsRecordByNameAsync_WhenNoRecordExists_ReturnsNull()
  {
    // Arrange
    var zoneId   = "test-zone-id";
    var hostname = "findme.example.com";

    var                 successResponse = HttpFixtures.CreateSuccessResponse(Array.Empty<DnsRecord>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.FindDnsRecordByNameAsync(zoneId, hostname);

    // Assert
    result.Should().BeNull();
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should()
                    .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records?name={hostname}");
  }

  /// <summary>
  ///   Verifies that ListAllDnsRecordsAsync handles page-based pagination correctly. Cloudflare's page-based
  ///   pagination uses a `result_info` object with `page` and `total_pages` to control the loop. [4, 8, 17]
  /// </summary>
  [Fact]
  public async Task ListAllDnsRecordsAsync_ShouldHandlePaginationCorrectly()
  {
    // Arrange
    var zoneId  = "test-zone-id";
    var record1 = new DnsRecord("id-1", "a.example.com", "A");
    var record2 = new DnsRecord("id-2", "b.example.com", "A");

    // Mock first page response
    var responsePage1 =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new[] { record1 },
          result_info = new { page = 1, per_page = 1, count = 1, total_pages = 2, total_count = 2 }
        },
        _serializerOptions);

    // Mock second page response
    var responsePage2 =
      JsonSerializer.Serialize(
        new
        {
          success     = true,
          errors      = Array.Empty<object>(),
          messages    = Array.Empty<object>(),
          result      = new[] { record2 },
          result_info = new { page = 2, per_page = 1, count = 1, total_pages = 2, total_count = 2 }
        },
        _serializerOptions);

    var capturedRequests = new List<HttpRequestMessage>();
    var mockHandler      = new Mock<HttpMessageHandler>();

    // Setup sequential responses for the paginated calls.
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
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var allRecords = new List<DnsRecord>();
    await foreach (var record in sut.ListAllDnsRecordsAsync(zoneId, new ListDnsRecordsFilters { PerPage = 1 }))
      allRecords.Add(record);

    // Assert
    capturedRequests.Should().HaveCount(2);
    capturedRequests[0].RequestUri!.Query.Should().Contain("page=1");
    capturedRequests[1].RequestUri!.Query.Should().Contain("page=2");
    allRecords.Should().HaveCount(2);
    allRecords.Select(r => r.Id).Should().ContainInOrder("id-1", "id-2");
  }

  /// <summary>Verifies that ListDnsRecordsAsync constructs the correct request URI when all filters are applied.</summary>
  [Fact]
  public async Task ListDnsRecordsAsync_ShouldConstructCorrectRequestUri_WithAllFilters()
  {
    // Arrange
    var zoneId = "test-zone-id";
    var filters = new ListDnsRecordsFilters(
      "A",
      "test.com",
      Page: 2,
      Order: "type",
      Direction: ListOrderDirection.Descending,
      PerPage: 50,
      Content: "1.2.3.4",
      Proxied: true
    );
    var                 successResponse = HttpFixtures.CreateSuccessResponse(Array.Empty<DnsRecord>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);
    var expectedQuery =
      "type=A&name=test.com&content=1.2.3.4&proxied=true&page=2&per_page=50&order=type&direction=desc";

    // Act
    await sut.ListDnsRecordsAsync(zoneId, filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should()
                    .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records?{expectedQuery}");
  }

  /// <summary>Verifies that ExportDnsRecordsAsync calls the correct endpoint and returns the raw string body.</summary>
  [Fact]
  public async Task ExportDnsRecordsAsync_ShouldCallCorrectEndpointAndReturnString()
  {
    // Arrange
    var                 zoneId          = "test-zone-id";
    var                 bindContent     = "; BIND file content";
    HttpRequestMessage? capturedRequest = null;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
               .ReturnsAsync(
                 new HttpResponseMessage
                 {
                   StatusCode = HttpStatusCode.OK,
                   Content    = new StringContent(bindContent, System.Text.Encoding.UTF8, "text/plain")
                 });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ExportDnsRecordsAsync(zoneId);

    // Assert
    result.Should().Be(bindContent);
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records/export");
  }

  /// <summary>Verifies that ImportDnsRecordsAsync sends a multipart/form-data request with the correct parameters.</summary>
  [Fact]
  public async Task ImportDnsRecordsAsync_ShouldSendMultipartFormDataRequest()
  {
    // Arrange
    var       zoneId      = "test-zone-id";
    var       bindContent = "; BIND file content";
    using var stream      = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(bindContent));

    var                 successResponse = HttpFixtures.CreateSuccessResponse(new DnsImportResult(1, 0, 1));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.ImportDnsRecordsAsync(zoneId, stream, true, false);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records/import?proxied=true&overwrite_existing=false");
    capturedRequest.Content.Should().BeOfType<MultipartFormDataContent>();
  }

  /// <summary>Verifies that PurgeCacheAsync serializes the correct payload for purging everything.</summary>
  [Fact]
  public async Task PurgeCacheAsync_ShouldSerializeCorrectPayload_ForPurgeEverything()
  {
    // Arrange
    var zoneId  = "test-zone-id";
    var request = new PurgeCacheRequest(true);

    var                 successResponse = HttpFixtures.CreateSuccessResponse(new PurgeCacheResult(zoneId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.PurgeCacheAsync(zoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/purge_cache");
    var content = await capturedRequest.Content!.ReadAsStringAsync();
    content.Should().Be("{\"purge_everything\":true}");
  }

  /// <summary>Verifies that PurgeCacheAsync serializes the correct payload for purging specific files.</summary>
  [Fact]
  public async Task PurgeCacheAsync_ShouldSerializeCorrectPayload_ForFiles()
  {
    // Arrange
    var zoneId  = "test-zone-id";
    var request = new PurgeCacheRequest(Files: ["url1", "url2"]);

    var                 successResponse = HttpFixtures.CreateSuccessResponse(new PurgeCacheResult(zoneId));
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.PurgeCacheAsync(zoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    var content         = await capturedRequest.Content!.ReadAsStringAsync();
    var expectedPayload = new { files = new[] { "url1", "url2" } };
    var expectedJson    = JsonSerializer.Serialize(expectedPayload, _serializerOptions);
    content.Should().Be(expectedJson);
  }

  #endregion
}
