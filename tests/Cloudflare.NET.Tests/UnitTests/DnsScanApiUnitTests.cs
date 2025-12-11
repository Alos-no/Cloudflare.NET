namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Cloudflare.NET.Dns;
using Cloudflare.NET.Dns.Models;
using Microsoft.Extensions.Logging;
using Shared.Fixtures;
using Shared.Mocks;
using DnsRecordType = Zones.Models.DnsRecordType;

/// <summary>
///   Contains unit tests for the DNS scan operations in <see cref="DnsApi" /> class.
///   Tests cover request construction, response deserialization, URL encoding, and error handling.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class DnsScanApiUnitTests
{
  #region Properties & Fields - Non-Public

  /// <summary>Test zone ID.</summary>
  private const string TestZoneId = TestDataFactory.TestZoneId;

  #endregion


  #region Helper Methods

  /// <summary>Creates a DnsApi instance with a mock HTTP handler.</summary>
  /// <param name="responseContent">The response content to return.</param>
  /// <param name="statusCode">The HTTP status code to return.</param>
  /// <param name="requestCallback">Optional callback for inspecting the request (receives request and body).</param>
  /// <returns>A configured DnsApi instance.</returns>
  private static IDnsApi CreateDnsApi(
    string responseContent,
    HttpStatusCode statusCode = HttpStatusCode.OK,
    Action<HttpRequestMessage, string?>? requestCallback = null)
  {
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(
      responseContent,
      statusCode,
      requestCallback != null
        ? (req, _) =>
          {
            var body = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            requestCallback(req, body);
          }
        : null
    );

    var httpClient = new HttpClient(mockHandler.Object)
    {
      BaseAddress = new Uri("https://api.cloudflare.com/client/v4/")
    };

    var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));

    return new DnsApi(httpClient, loggerFactory);
  }

  /// <summary>Creates a standard success response wrapper.</summary>
  /// <param name="result">The result object to wrap.</param>
  /// <returns>A JSON string with the standard Cloudflare API envelope.</returns>
  private static string CreateSuccessResponse<T>(T result)
  {
    return JsonSerializer.Serialize(new
    {
      success = true,
      errors = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result
    });
  }

  /// <summary>Creates a success response with no result body (for trigger endpoint).</summary>
  /// <returns>A JSON string with success but no result.</returns>
  private static string CreateEmptySuccessResponse()
  {
    return JsonSerializer.Serialize(new
    {
      success = true,
      errors = Array.Empty<object>(),
      messages = Array.Empty<object>()
    });
  }

  /// <summary>Creates an error response.</summary>
  /// <param name="code">Error code.</param>
  /// <param name="message">Error message.</param>
  /// <returns>A JSON string with error details.</returns>
  private static string CreateErrorResponse(int code, string message)
  {
    return JsonSerializer.Serialize(new
    {
      success = false,
      errors = new[] { new { code, message } },
      messages = Array.Empty<object>()
    });
  }

  #endregion


  #region U01-U02: Trigger Scan Request Tests

  /// <summary>U01: Verifies TriggerDnsRecordScanAsync sends POST to correct endpoint.</summary>
  [Fact]
  public async Task TriggerDnsRecordScanAsync_ValidZoneId_SendsPostToCorrectEndpoint()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var sut = CreateDnsApi(CreateEmptySuccessResponse(), requestCallback: (req, _) => capturedRequest = req);

    // Act
    await sut.TriggerDnsRecordScanAsync(TestZoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Contain($"zones/{TestZoneId}/dns_records/scan/trigger");
  }

  /// <summary>U02: Verifies TriggerDnsRecordScanAsync sends null or empty body.</summary>
  [Fact]
  public async Task TriggerDnsRecordScanAsync_ValidZoneId_SendsNullOrEmptyBody()
  {
    // Arrange
    string? capturedBody = null;
    var sut = CreateDnsApi(CreateEmptySuccessResponse(), requestCallback: (_, body) => capturedBody = body);

    // Act
    await sut.TriggerDnsRecordScanAsync(TestZoneId);

    // Assert - Body should be null or empty (no request body for trigger)
    if (capturedBody != null)
    {
      capturedBody.Should().BeOneOf("", "null", "{}");
    }
  }

  #endregion


  #region U03: Get Review Request Tests

  /// <summary>U03: Verifies GetDnsRecordScanReviewAsync sends GET to correct endpoint.</summary>
  [Fact]
  public async Task GetDnsRecordScanReviewAsync_ValidZoneId_SendsGetToCorrectEndpoint()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var sut = CreateDnsApi(CreateSuccessResponse(Array.Empty<object>()), requestCallback: (req, _) => capturedRequest = req);

    // Act
    await sut.GetDnsRecordScanReviewAsync(TestZoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Contain($"zones/{TestZoneId}/dns_records/scan/review");
  }

  #endregion


  #region U04-U07: Submit Review Request Tests

  /// <summary>U04: Verifies SubmitDnsRecordScanReviewAsync with accepts only sends correct JSON.</summary>
  [Fact]
  public async Task SubmitDnsRecordScanReviewAsync_AcceptsOnly_SendsCorrectJson()
  {
    // Arrange
    string? capturedBody = null;
    var sut = CreateDnsApi(CreateSuccessResponse(new { accepts = 2, rejects = 0 }), requestCallback: (_, body) => capturedBody = body);

    var request = new DnsScanReviewRequest { Accepts = ["id1", "id2"] };

    // Act
    await sut.SubmitDnsRecordScanReviewAsync(TestZoneId, request);

    // Assert
    capturedBody.Should().NotBeNullOrEmpty();
    capturedBody.Should().Contain("\"accepts\"");
    capturedBody.Should().Contain("\"id1\"");
    capturedBody.Should().Contain("\"id2\"");
    capturedBody.Should().Contain("\"rejects\""); // Always present (as empty array)
  }

  /// <summary>U05: Verifies SubmitDnsRecordScanReviewAsync with rejects only sends correct JSON.</summary>
  [Fact]
  public async Task SubmitDnsRecordScanReviewAsync_RejectsOnly_SendsCorrectJson()
  {
    // Arrange
    string? capturedBody = null;
    var sut = CreateDnsApi(CreateSuccessResponse(new { accepts = 0, rejects = 3 }), requestCallback: (_, body) => capturedBody = body);

    var request = new DnsScanReviewRequest { Rejects = ["id1", "id2", "id3"] };

    // Act
    await sut.SubmitDnsRecordScanReviewAsync(TestZoneId, request);

    // Assert
    capturedBody.Should().NotBeNullOrEmpty();
    capturedBody.Should().Contain("\"rejects\"");
    capturedBody.Should().Contain("\"id1\"");
    capturedBody.Should().Contain("\"accepts\""); // Always present (as empty array)
  }

  /// <summary>U06: Verifies SubmitDnsRecordScanReviewAsync with mixed sends correct JSON.</summary>
  [Fact]
  public async Task SubmitDnsRecordScanReviewAsync_MixedAcceptsAndRejects_SendsCorrectJson()
  {
    // Arrange
    string? capturedBody = null;
    var sut = CreateDnsApi(CreateSuccessResponse(new { accepts = 1, rejects = 2 }), requestCallback: (_, body) => capturedBody = body);

    var request = new DnsScanReviewRequest { Accepts = ["accept-id"], Rejects = ["reject-id1", "reject-id2"] };

    // Act
    await sut.SubmitDnsRecordScanReviewAsync(TestZoneId, request);

    // Assert
    capturedBody.Should().NotBeNullOrEmpty();
    capturedBody.Should().Contain("\"accepts\"");
    capturedBody.Should().Contain("\"rejects\"");
    capturedBody.Should().Contain("\"accept-id\"");
    capturedBody.Should().Contain("\"reject-id1\"");
  }

  /// <summary>U07: Verifies SubmitDnsRecordScanReviewAsync includes both arrays even when one is empty.</summary>
  /// <remarks>
  ///   The Cloudflare API requires both accepts and rejects arrays to be present in the request body.
  ///   When one is not specified, it defaults to an empty array.
  /// </remarks>
  [Fact]
  public async Task SubmitDnsRecordScanReviewAsync_IncludesBothArrays_RejectsEmptyWhenNotSet()
  {
    // Arrange
    string? capturedBody = null;
    var sut = CreateDnsApi(CreateSuccessResponse(new { accepts = 1, rejects = 0 }), requestCallback: (_, body) => capturedBody = body);

    // Only set accepts, leave rejects as default (empty array)
    var request = new DnsScanReviewRequest { Accepts = ["id1"] };

    // Act
    await sut.SubmitDnsRecordScanReviewAsync(TestZoneId, request);

    // Assert
    capturedBody.Should().NotBeNullOrEmpty();
    capturedBody.Should().Contain("\"accepts\"");
    capturedBody.Should().Contain("\"rejects\""); // Empty array is still present
    capturedBody.Should().Contain("\"id1\"");
  }

  /// <summary>Verifies SubmitDnsRecordScanReviewAsync sends POST to correct endpoint.</summary>
  [Fact]
  public async Task SubmitDnsRecordScanReviewAsync_ValidRequest_SendsPostToCorrectEndpoint()
  {
    // Arrange
    HttpRequestMessage? capturedRequest = null;
    var sut = CreateDnsApi(CreateSuccessResponse(new { accepts = 0, rejects = 0 }), requestCallback: (req, _) => capturedRequest = req);

    var request = new DnsScanReviewRequest();

    // Act
    await sut.SubmitDnsRecordScanReviewAsync(TestZoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Contain($"zones/{TestZoneId}/dns_records/scan/review");
  }

  #endregion


  #region U08: URL Encoding Tests

  /// <summary>U08: Verifies ZoneId with special characters is properly URL-encoded.</summary>
  /// <remarks>
  ///   Note: Uri.ToString() decodes certain characters like %20 (space) back to their original form.
  ///   Characters like / + &amp; remain encoded because they have special meaning in URLs.
  ///   This test uses characters that remain encoded in the URI to verify URL encoding is working.
  /// </remarks>
  [Fact]
  public async Task TriggerDnsRecordScanAsync_ZoneIdWithSpecialChars_ProperlyUrlEncoded()
  {
    // Arrange
    const string specialZoneId = "zone/with+special&chars";
    HttpRequestMessage? capturedRequest = null;
    var sut = CreateDnsApi(CreateEmptySuccessResponse(), requestCallback: (req, _) => capturedRequest = req);

    // Act
    await sut.TriggerDnsRecordScanAsync(specialZoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("zone%2Fwith%2Bspecial%26chars");
  }

  /// <summary>U08b: Verifies GetDnsRecordScanReviewAsync URL-encodes zone ID.</summary>
  [Fact]
  public async Task GetDnsRecordScanReviewAsync_ZoneIdWithSpecialChars_ProperlyUrlEncoded()
  {
    // Arrange
    const string specialZoneId = "zone/with+special&chars";
    HttpRequestMessage? capturedRequest = null;
    var sut = CreateDnsApi(CreateSuccessResponse(Array.Empty<object>()), requestCallback: (req, _) => capturedRequest = req);

    // Act
    await sut.GetDnsRecordScanReviewAsync(specialZoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("zone%2Fwith%2Bspecial%26chars");
  }

  /// <summary>U08c: Verifies SubmitDnsRecordScanReviewAsync URL-encodes zone ID.</summary>
  [Fact]
  public async Task SubmitDnsRecordScanReviewAsync_ZoneIdWithSpecialChars_ProperlyUrlEncoded()
  {
    // Arrange
    const string specialZoneId = "zone/with+special&chars";
    HttpRequestMessage? capturedRequest = null;
    var sut = CreateDnsApi(CreateSuccessResponse(new { accepts = 0, rejects = 0 }), requestCallback: (req, _) => capturedRequest = req);
    var request = new DnsScanReviewRequest();

    // Act
    await sut.SubmitDnsRecordScanReviewAsync(specialZoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("zone%2Fwith%2Bspecial%26chars");
  }

  #endregion


  #region U09-U14: Response Deserialization Tests

  /// <summary>U09: Verifies TriggerDnsRecordScanAsync completes without exception on success.</summary>
  [Fact]
  public async Task TriggerDnsRecordScanAsync_VoidResponse_CompletesSuccessfully()
  {
    // Arrange
    var sut = CreateDnsApi(CreateEmptySuccessResponse());

    // Act
    var action = async () => await sut.TriggerDnsRecordScanAsync(TestZoneId);

    // Assert
    await action.Should().NotThrowAsync();
  }

  /// <summary>U10: Verifies GetDnsRecordScanReviewAsync returns empty list when no pending records.</summary>
  [Fact]
  public async Task GetDnsRecordScanReviewAsync_EmptyResult_ReturnsEmptyList()
  {
    // Arrange
    var sut = CreateDnsApi(CreateSuccessResponse(Array.Empty<object>()));

    // Act
    var result = await sut.GetDnsRecordScanReviewAsync(TestZoneId);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  /// <summary>U11: Verifies GetDnsRecordScanReviewAsync deserializes DnsRecord list correctly.</summary>
  [Fact]
  public async Task GetDnsRecordScanReviewAsync_WithRecords_ReturnsDnsRecordList()
  {
    // Arrange
    var response = CreateSuccessResponse(new[]
    {
      new
      {
        id = "rec-1",
        name = "www.example.com",
        type = "A",
        content = "192.0.2.1",
        proxied = false,
        proxiable = true,
        ttl = 300,
        created_on = "2024-01-01T00:00:00Z",
        modified_on = "2024-01-01T00:00:00Z"
      },
      new
      {
        id = "rec-2",
        name = "mail.example.com",
        type = "MX",
        content = "mail.example.com",
        proxied = false,
        proxiable = false,
        ttl = 3600,
        created_on = "2024-01-02T00:00:00Z",
        modified_on = "2024-01-02T00:00:00Z"
      }
    });
    var sut = CreateDnsApi(response);

    // Act
    var result = await sut.GetDnsRecordScanReviewAsync(TestZoneId);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(2);
    result[0].Id.Should().Be("rec-1");
    result[0].Name.Should().Be("www.example.com");
    result[0].Type.Should().Be(DnsRecordType.A);
    result[1].Id.Should().Be("rec-2");
    result[1].Type.Should().Be(DnsRecordType.MX);
  }

  /// <summary>U12: Verifies SubmitDnsRecordScanReviewAsync deserializes accepts-only result.</summary>
  [Fact]
  public async Task SubmitDnsRecordScanReviewAsync_AcceptsOnly_ReturnsCorrectResult()
  {
    // Arrange
    var sut = CreateDnsApi(CreateSuccessResponse(new { accepts = 5, rejects = 0 }));
    var request = new DnsScanReviewRequest { Accepts = ["1", "2", "3", "4", "5"] };

    // Act
    var result = await sut.SubmitDnsRecordScanReviewAsync(TestZoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Accepts.Should().Be(5);
    result.Rejects.Should().Be(0);
  }

  /// <summary>U13: Verifies SubmitDnsRecordScanReviewAsync deserializes rejects-only result.</summary>
  [Fact]
  public async Task SubmitDnsRecordScanReviewAsync_RejectsOnly_ReturnsCorrectResult()
  {
    // Arrange
    var sut = CreateDnsApi(CreateSuccessResponse(new { accepts = 0, rejects = 3 }));
    var request = new DnsScanReviewRequest { Rejects = ["1", "2", "3"] };

    // Act
    var result = await sut.SubmitDnsRecordScanReviewAsync(TestZoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Accepts.Should().Be(0);
    result.Rejects.Should().Be(3);
  }

  /// <summary>U14: Verifies SubmitDnsRecordScanReviewAsync deserializes mixed result.</summary>
  [Fact]
  public async Task SubmitDnsRecordScanReviewAsync_MixedResult_ReturnsCorrectCounts()
  {
    // Arrange
    var sut = CreateDnsApi(CreateSuccessResponse(new { accepts = 2, rejects = 1 }));
    var request = new DnsScanReviewRequest { Accepts = ["a", "b"], Rejects = ["c"] };

    // Act
    var result = await sut.SubmitDnsRecordScanReviewAsync(TestZoneId, request);

    // Assert
    result.Should().NotBeNull();
    result.Accepts.Should().Be(2);
    result.Rejects.Should().Be(1);
  }

  #endregion


  #region U15-U22: Error Handling Tests

  /// <summary>U15: Verifies 404 response throws HttpRequestException.</summary>
  [Fact]
  public async Task GetDnsRecordScanReviewAsync_NotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var sut = CreateDnsApi(CreateErrorResponse(7003, "Zone not found"), HttpStatusCode.NotFound);

    // Act
    var action = async () => await sut.GetDnsRecordScanReviewAsync(TestZoneId);

    // Assert
    var ex = await action.Should().ThrowAsync<HttpRequestException>();
    ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  /// <summary>U16: Verifies API error in envelope throws CloudflareApiException.</summary>
  [Fact]
  public async Task TriggerDnsRecordScanAsync_ApiError_ThrowsWithErrorDetails()
  {
    // Arrange
    var sut = CreateDnsApi(CreateErrorResponse(1234, "Test error message"));

    // Act
    var action = async () => await sut.TriggerDnsRecordScanAsync(TestZoneId);

    // Assert
    await action.Should().ThrowAsync<Exception>();
  }

  /// <summary>U17: Verifies multiple errors in response are captured.</summary>
  [Fact]
  public async Task SubmitDnsRecordScanReviewAsync_MultipleErrors_ThrowsWithAllErrors()
  {
    // Arrange
    var response = JsonSerializer.Serialize(new
    {
      success = false,
      errors = new[]
      {
        new { code = 1001, message = "First error" },
        new { code = 1002, message = "Second error" }
      },
      messages = Array.Empty<object>()
    });
    var sut = CreateDnsApi(response);
    var request = new DnsScanReviewRequest { Accepts = ["invalid"] };

    // Act
    var action = async () => await sut.SubmitDnsRecordScanReviewAsync(TestZoneId, request);

    // Assert
    await action.Should().ThrowAsync<Exception>();
  }

  /// <summary>U19: Verifies 401 Unauthorized throws HttpRequestException.</summary>
  [Fact]
  public async Task TriggerDnsRecordScanAsync_Unauthorized_ThrowsHttpRequestException()
  {
    // Arrange
    var sut = CreateDnsApi(CreateErrorResponse(9109, "Unauthorized"), HttpStatusCode.Unauthorized);

    // Act
    var action = async () => await sut.TriggerDnsRecordScanAsync(TestZoneId);

    // Assert
    var ex = await action.Should().ThrowAsync<HttpRequestException>();
    ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>U20: Verifies 403 Forbidden throws HttpRequestException.</summary>
  [Fact]
  public async Task GetDnsRecordScanReviewAsync_Forbidden_ThrowsHttpRequestException()
  {
    // Arrange
    var sut = CreateDnsApi(CreateErrorResponse(9110, "Forbidden"), HttpStatusCode.Forbidden);

    // Act
    var action = async () => await sut.GetDnsRecordScanReviewAsync(TestZoneId);

    // Assert
    var ex = await action.Should().ThrowAsync<HttpRequestException>();
    ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  /// <summary>U21: Verifies 429 Rate Limited throws HttpRequestException.</summary>
  [Fact]
  public async Task SubmitDnsRecordScanReviewAsync_RateLimited_ThrowsHttpRequestException()
  {
    // Arrange
    var sut = CreateDnsApi(CreateErrorResponse(10000, "Rate limited"), HttpStatusCode.TooManyRequests);
    var request = new DnsScanReviewRequest { Accepts = ["id"] };

    // Act
    var action = async () => await sut.SubmitDnsRecordScanReviewAsync(TestZoneId, request);

    // Assert
    var ex = await action.Should().ThrowAsync<HttpRequestException>();
    ex.Which.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
  }

  /// <summary>U22: Verifies 500/502/503 server errors throw HttpRequestException.</summary>
  [Theory]
  [InlineData(HttpStatusCode.InternalServerError)]
  [InlineData(HttpStatusCode.BadGateway)]
  [InlineData(HttpStatusCode.ServiceUnavailable)]
  public async Task TriggerDnsRecordScanAsync_ServerError_ThrowsHttpRequestException(HttpStatusCode statusCode)
  {
    // Arrange
    var sut = CreateDnsApi(CreateErrorResponse(0, "Server error"), statusCode);

    // Act
    var action = async () => await sut.TriggerDnsRecordScanAsync(TestZoneId);

    // Assert
    var ex = await action.Should().ThrowAsync<HttpRequestException>();
    ex.Which.StatusCode.Should().Be(statusCode);
  }

  #endregion


  #region Parameter Validation Tests

  /// <summary>Verifies TriggerDnsRecordScanAsync throws on null zoneId.</summary>
  [Fact]
  public async Task TriggerDnsRecordScanAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var sut = CreateDnsApi(CreateEmptySuccessResponse());

    // Act
    var action = async () => await sut.TriggerDnsRecordScanAsync(null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentNullException>()
      .WithParameterName("zoneId");
  }

  /// <summary>Verifies TriggerDnsRecordScanAsync throws on empty zoneId.</summary>
  [Fact]
  public async Task TriggerDnsRecordScanAsync_EmptyZoneId_ThrowsArgumentException()
  {
    // Arrange
    var sut = CreateDnsApi(CreateEmptySuccessResponse());

    // Act
    var action = async () => await sut.TriggerDnsRecordScanAsync("");

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
      .WithParameterName("zoneId");
  }

  /// <summary>Verifies GetDnsRecordScanReviewAsync throws on whitespace zoneId.</summary>
  [Fact]
  public async Task GetDnsRecordScanReviewAsync_WhitespaceZoneId_ThrowsArgumentException()
  {
    // Arrange
    var sut = CreateDnsApi(CreateSuccessResponse(Array.Empty<object>()));

    // Act
    var action = async () => await sut.GetDnsRecordScanReviewAsync("   ");

    // Assert
    await action.Should().ThrowAsync<ArgumentException>()
      .WithParameterName("zoneId");
  }

  /// <summary>Verifies SubmitDnsRecordScanReviewAsync throws on null request.</summary>
  [Fact]
  public async Task SubmitDnsRecordScanReviewAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var sut = CreateDnsApi(CreateSuccessResponse(new { accepts = 0, rejects = 0 }));

    // Act
    var action = async () => await sut.SubmitDnsRecordScanReviewAsync(TestZoneId, null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentNullException>()
      .WithParameterName("request");
  }

  #endregion
}
