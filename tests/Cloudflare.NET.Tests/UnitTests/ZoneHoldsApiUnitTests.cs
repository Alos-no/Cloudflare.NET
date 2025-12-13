namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Shared.Fixtures;
using Shared.Helpers;
using Shared.Mocks;
using Xunit.Abstractions;
using Zones;
using Zones.Models;

/// <summary>
///   Contains unit tests for the Zone Hold operations of <see cref="ZonesApi" />. These tests verify request
///   construction, URL encoding, response deserialization, and parameter validation for zone hold functionality.
/// </summary>
/// <remarks>
///   This test class focuses on Zone Hold operations including:
///   <list type="bullet">
///     <item><description>GetZoneHoldAsync - Get zone hold status</description></item>
///     <item><description>CreateZoneHoldAsync - Create a zone hold</description></item>
///     <item><description>UpdateZoneHoldAsync - Update zone hold settings</description></item>
///     <item><description>RemoveZoneHoldAsync - Remove a zone hold</description></item>
///   </list>
///   For Zone CRUD unit tests, see <see cref="ZonesApiUnitTests" />.
///   For Zone Settings unit tests, see <see cref="ZoneSettingsApiUnitTests" />.
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class ZoneHoldsApiUnitTests
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

  public ZoneHoldsApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Zone Hold Tests - Request Construction

  /// <summary>U01: Verifies that GetZoneHoldAsync sends correct GET request.</summary>
  [Fact]
  public async Task GetZoneHoldAsync_SendsCorrectRequest()
  {
    // Arrange
    const string zoneId = "zone-123";
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneHold());
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.GetZoneHoldAsync(zoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Contain($"zones/{zoneId}/hold");
  }

  /// <summary>U02: Verifies that CreateZoneHoldAsync sends POST request without query params by default.</summary>
  [Fact]
  public async Task CreateZoneHoldAsync_Default_SendsCorrectRequest()
  {
    // Arrange
    const string zoneId = "zone-123";
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneHold(hold: true));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateZoneHoldAsync(zoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    var uri = capturedRequest.RequestUri!.ToString();
    uri.Should().Contain($"zones/{zoneId}/hold");
    uri.Should().NotContain("include_subdomains");
  }

  /// <summary>U03: Verifies that CreateZoneHoldAsync with includeSubdomains=true sends correct query param.</summary>
  [Fact]
  public async Task CreateZoneHoldAsync_WithSubdomains_SendsCorrectRequest()
  {
    // Arrange
    const string zoneId = "zone-123";
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneHold(hold: true));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateZoneHoldAsync(zoneId, includeSubdomains: true);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    var uri = capturedRequest.RequestUri!.ToString();
    uri.Should().Contain($"zones/{zoneId}/hold?include_subdomains=true");
  }

  /// <summary>U04: Verifies that UpdateZoneHoldAsync with HoldAfter only sends correct body.</summary>
  [Fact]
  public async Task UpdateZoneHoldAsync_HoldAfterOnly_SendsCorrectBody()
  {
    // Arrange
    const string zoneId   = "zone-123";
    var          holdAfter = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    var          request   = new UpdateZoneHoldRequest(HoldAfter: holdAfter);

    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneHold(hold: true, holdAfter: holdAfter.ToString("O")));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateZoneHoldAsync(zoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
    var content = await capturedRequest.Content!.ReadAsStringAsync();
    content.Should().Contain("hold_after");
    content.Should().NotContain("include_subdomains");
  }

  /// <summary>U05: Verifies that UpdateZoneHoldAsync with IncludeSubdomains only sends correct body.</summary>
  [Fact]
  public async Task UpdateZoneHoldAsync_SubdomainsOnly_SendsCorrectBody()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          request = new UpdateZoneHoldRequest(IncludeSubdomains: true);

    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneHold(hold: true));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateZoneHoldAsync(zoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    var content = await capturedRequest.Content!.ReadAsStringAsync();
    content.Should().Contain("include_subdomains");
    content.Should().NotContain("hold_after");
  }

  /// <summary>U06: Verifies that UpdateZoneHoldAsync with both fields sends correct body.</summary>
  [Fact]
  public async Task UpdateZoneHoldAsync_BothFields_SendsCorrectBody()
  {
    // Arrange
    const string zoneId    = "zone-123";
    var          holdAfter = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    var          request   = new UpdateZoneHoldRequest(HoldAfter: holdAfter, IncludeSubdomains: true);

    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneHold(hold: true));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateZoneHoldAsync(zoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    var content = await capturedRequest.Content!.ReadAsStringAsync();
    content.Should().Contain("hold_after");
    content.Should().Contain("include_subdomains");
  }

  /// <summary>U07: Verifies that UpdateZoneHoldRequest with null values omits those fields in JSON.</summary>
  [Fact]
  public void UpdateZoneHoldRequest_NullValues_OmittedInJson()
  {
    // Arrange
    var request = new UpdateZoneHoldRequest(); // All defaults (nulls)

    // Act
    var json = JsonSerializer.Serialize(request, _serializerOptions);

    // Assert
    json.Should().Be("{}");
  }

  /// <summary>U08: Verifies that RemoveZoneHoldAsync sends correct DELETE request.</summary>
  [Fact]
  public async Task RemoveZoneHoldAsync_SendsCorrectRequest()
  {
    // Arrange
    const string zoneId = "zone-123";
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneHold(hold: false));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.RemoveZoneHoldAsync(zoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should().Contain($"zones/{zoneId}/hold");
  }

  #endregion


  #region Zone Hold Tests - URL Encoding

  /// <summary>U09: Verifies that special characters in zoneId are URL encoded for zone hold operations.</summary>
  [Fact]
  public async Task GetZoneHoldAsync_SpecialCharactersInZoneId_UrlEncodesCorrectly()
  {
    // Arrange
    const string zoneId = "zone/with+special";
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneHold());
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.GetZoneHoldAsync(zoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.ToString();
    uri.Should().Contain("zone%2Fwith%2Bspecial");
    uri.Should().NotContain("zone/with+special");
  }

  #endregion


  #region Zone Hold Tests - Response Deserialization

  /// <summary>U10: Verifies that ZoneHold with active hold deserializes correctly.</summary>
  [Fact]
  public async Task GetZoneHoldAsync_ActiveHold_DeserializesCorrectly()
  {
    // Arrange
    const string zoneId          = "zone-123";
    const string holdAfterString = "2024-01-01T00:00:00Z";
    var response = new
    {
      success  = true,
      errors   = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result = new
      {
        hold               = true,
        hold_after         = holdAfterString,
        include_subdomains = true
      }
    };
    var successResponse = JsonSerializer.Serialize(response, _serializerOptions);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneHoldAsync(zoneId);

    // Assert
    result.Should().NotBeNull();
    result.Hold.Should().BeTrue();
    result.HoldAfter.Should().NotBeNull();
    result.HoldAfter!.Value.Year.Should().Be(2024);
    result.IncludeSubdomains.Should().BeTrue();
  }

  /// <summary>U11: Verifies that ZoneHold with inactive hold deserializes correctly.</summary>
  [Fact]
  public async Task GetZoneHoldAsync_InactiveHold_DeserializesCorrectly()
  {
    // Arrange
    const string zoneId = "zone-123";
    var response = new
    {
      success  = true,
      errors   = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result = new
      {
        hold               = false,
        hold_after         = (string?)null,
        include_subdomains = (bool?)null
      }
    };
    var successResponse = JsonSerializer.Serialize(response, _serializerOptions);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneHoldAsync(zoneId);

    // Assert
    result.Should().NotBeNull();
    result.Hold.Should().BeFalse();
    result.HoldAfter.Should().BeNull();
    result.IncludeSubdomains.Should().BeNull();
  }

  /// <summary>U12: Verifies that ZoneHold with future hold_after deserializes correctly.</summary>
  [Fact]
  public async Task GetZoneHoldAsync_FutureHold_DeserializesCorrectly()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          futureDate = DateTime.UtcNow.AddDays(30);
    var response = new
    {
      success  = true,
      errors   = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result = new
      {
        hold       = true,
        hold_after = futureDate.ToString("O")
      }
    };
    var successResponse = JsonSerializer.Serialize(response, _serializerOptions);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneHoldAsync(zoneId);

    // Assert
    result.Should().NotBeNull();
    result.HoldAfter.Should().NotBeNull();
    result.HoldAfter!.Value.Should().BeAfter(DateTime.UtcNow);
  }

  /// <summary>U13: Verifies that include_subdomains as boolean deserializes correctly.</summary>
  /// <remarks>
  ///   The Cloudflare API returns include_subdomains as a boolean value.
  /// </remarks>
  [Fact]
  public async Task GetZoneHoldAsync_IncludeSubdomainsTrue_DeserializesCorrectly()
  {
    // Arrange
    const string zoneId = "zone-123";
    var response = new
    {
      success  = true,
      errors   = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result = new
      {
        hold               = true,
        include_subdomains = true
      }
    };
    var successResponse = JsonSerializer.Serialize(response, _serializerOptions);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneHoldAsync(zoneId);

    // Assert
    result.Should().NotBeNull();
    result.IncludeSubdomains.Should().BeTrue();
  }

  /// <summary>U15: Verifies that ZoneHold with only hold field deserializes correctly.</summary>
  [Fact]
  public async Task GetZoneHoldAsync_MissingOptionalFields_DeserializesCorrectly()
  {
    // Arrange
    const string zoneId   = "zone-123";
    const string jsonBody = """{"success":true,"errors":[],"messages":[],"result":{"hold":true}}""";
    var          mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonBody, HttpStatusCode.OK);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneHoldAsync(zoneId);

    // Assert
    result.Should().NotBeNull();
    result.Hold.Should().BeTrue();
    result.HoldAfter.Should().BeNull();
    result.IncludeSubdomains.Should().BeNull();
  }

  #endregion


  #region Zone Hold Tests - Parameter Validation

  /// <summary>Verifies that GetZoneHoldAsync throws ArgumentNullException for null zoneId.</summary>
  [Fact]
  public async Task GetZoneHoldAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut         = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync<ZoneHold>(
      () => sut.GetZoneHoldAsync(null!),
      "zoneId");
  }

  /// <summary>Verifies that GetZoneHoldAsync throws ArgumentException for empty zoneId.</summary>
  [Fact]
  public async Task GetZoneHoldAsync_EmptyZoneId_ThrowsArgumentException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut         = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentEmptyAsync<ZoneHold>(
      () => sut.GetZoneHoldAsync(""),
      "zoneId");
  }

  /// <summary>Verifies that CreateZoneHoldAsync throws ArgumentNullException for null zoneId.</summary>
  [Fact]
  public async Task CreateZoneHoldAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut         = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync<ZoneHold>(
      () => sut.CreateZoneHoldAsync(null!),
      "zoneId");
  }

  /// <summary>Verifies that UpdateZoneHoldAsync throws ArgumentNullException for null zoneId.</summary>
  [Fact]
  public async Task UpdateZoneHoldAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut         = new ZonesApi(httpClient, _loggerFactory);
    var request     = new UpdateZoneHoldRequest();

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync<ZoneHold>(
      () => sut.UpdateZoneHoldAsync(null!, request),
      "zoneId");
  }

  /// <summary>Verifies that UpdateZoneHoldAsync throws ArgumentNullException for null request.</summary>
  [Fact]
  public async Task UpdateZoneHoldAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut         = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync<ZoneHold>(
      () => sut.UpdateZoneHoldAsync("zone-123", null!),
      "request");
  }

  /// <summary>Verifies that RemoveZoneHoldAsync throws ArgumentNullException for null zoneId.</summary>
  [Fact]
  public async Task RemoveZoneHoldAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut         = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync<ZoneHold>(
      () => sut.RemoveZoneHoldAsync(null!),
      "zoneId");
  }

  #endregion
}
