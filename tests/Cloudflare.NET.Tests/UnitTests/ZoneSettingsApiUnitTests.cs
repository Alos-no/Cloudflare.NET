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
///   Contains unit tests for the Zone Settings operations of <see cref="ZonesApi" />. These tests verify request
///   construction, URL encoding, response deserialization, JsonElement value access, parameter validation,
///   and error handling for zone settings functionality.
/// </summary>
/// <remarks>
///   This test class focuses on Zone Settings operations including:
///   <list type="bullet">
///     <item><description>GetZoneSettingAsync - Get a specific zone setting</description></item>
///     <item><description>SetZoneSettingAsync - Update a zone setting value</description></item>
///   </list>
///   For Zone CRUD unit tests, see <see cref="ZonesApiUnitTests" />.
///   For Zone Hold unit tests, see <see cref="ZoneHoldsApiUnitTests" />.
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class ZoneSettingsApiUnitTests
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

  public ZoneSettingsApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Zone Settings Tests - Request Construction

  /// <summary>U01: Verifies that GetZoneSettingAsync sends correct GET request.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_SendsCorrectRequest()
  {
    // Arrange
    const string zoneId    = "zone-123";
    const string settingId = "ssl";
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneSetting(id: settingId, value: "full"));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.GetZoneSettingAsync(zoneId, settingId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/settings/{settingId}");
  }

  /// <summary>U02: Verifies that GetZoneSettingAsync using constant sends correct request.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_UsingConstant_SendsCorrectRequest()
  {
    // Arrange
    const string zoneId = "zone-123";
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneSetting(id: ZoneSettingIds.MinTlsVersion, value: "1.2"));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.GetZoneSettingAsync(zoneId, ZoneSettingIds.MinTlsVersion);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("settings/min_tls_version");
  }

  /// <summary>U03: Verifies that SetZoneSettingAsync with string value sends correct PATCH request.</summary>
  [Fact]
  public async Task SetZoneSettingAsync_StringValue_SendsCorrectRequest()
  {
    // Arrange
    const string zoneId    = "zone-123";
    const string settingId = "min_tls_version";
    const string newValue  = "1.2";
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneSetting(id: settingId, value: newValue));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.SetZoneSettingAsync(zoneId, settingId, newValue);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{zoneId}/settings/{settingId}");
    var content = await capturedRequest.Content!.ReadAsStringAsync();
    content.Should().Contain("\"value\":\"1.2\"");
  }

  /// <summary>U04: Verifies that SetZoneSettingAsync with integer value sends correct body.</summary>
  [Fact]
  public async Task SetZoneSettingAsync_IntegerValue_SendsCorrectBody()
  {
    // Arrange
    const string zoneId    = "zone-123";
    const string settingId = "browser_cache_ttl";
    const int    newValue  = 14400;
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneSetting(id: settingId, value: newValue));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.SetZoneSettingAsync(zoneId, settingId, newValue);

    // Assert
    capturedRequest.Should().NotBeNull();
    var content = await capturedRequest!.Content!.ReadAsStringAsync();
    content.Should().Contain("\"value\":14400");
  }

  /// <summary>U05: Verifies that SetZoneSettingAsync with on/off string value sends correct body.</summary>
  [Fact]
  public async Task SetZoneSettingAsync_OnOffValue_SendsCorrectBody()
  {
    // Arrange
    const string zoneId    = "zone-123";
    const string settingId = "development_mode";
    const string newValue  = "on";
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneSetting(id: settingId, value: newValue));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.SetZoneSettingAsync(zoneId, settingId, newValue);

    // Assert
    var content = await capturedRequest!.Content!.ReadAsStringAsync();
    content.Should().Contain("\"value\":\"on\"");
  }

  /// <summary>U06: Verifies that SetZoneSettingAsync with complex object sends correct body.</summary>
  [Fact]
  public async Task SetZoneSettingAsync_ComplexObject_SendsCorrectBody()
  {
    // Arrange
    const string zoneId    = "zone-123";
    const string settingId = "mobile_redirect";
    var          newValue  = new { status = "on", mobile_subdomain = "m", strip_uri = true };
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneSetting(id: settingId, value: newValue));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.SetZoneSettingAsync(zoneId, settingId, newValue);

    // Assert
    var content = await capturedRequest!.Content!.ReadAsStringAsync();
    content.Should().Contain("\"value\":");
    content.Should().Contain("\"status\":\"on\"");
    content.Should().Contain("\"mobile_subdomain\":\"m\"");
    content.Should().Contain("\"strip_uri\":true");
  }

  #endregion


  #region Zone Settings Tests - URL Encoding

  /// <summary>U07: Verifies that special characters in zoneId are URL encoded for zone settings.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_SpecialCharactersInZoneId_UrlEncodesCorrectly()
  {
    // Arrange
    const string zoneId    = "zone/with+special";
    const string settingId = "ssl";
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneSetting(id: settingId, value: "full"));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.GetZoneSettingAsync(zoneId, settingId);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.ToString();
    uri.Should().Contain("zone%2Fwith%2Bspecial");
    uri.Should().NotContain("zone/with+special");
  }

  /// <summary>U08: Verifies that special characters in settingId are URL encoded.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_SpecialCharactersInSettingId_UrlEncodesCorrectly()
  {
    // Arrange
    const string zoneId    = "zone-123";
    const string settingId = "setting/with+special";
    HttpRequestMessage? capturedRequest = null;
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZoneSetting(id: settingId, value: "test"));
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.GetZoneSettingAsync(zoneId, settingId);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.ToString();
    uri.Should().Contain("setting%2Fwith%2Bspecial");
    uri.Should().NotContain("setting/with+special");
  }

  #endregion


  #region Zone Settings Tests - Response Deserialization

  /// <summary>U09: Verifies that ZoneSetting with string value deserializes correctly.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_StringValue_DeserializesCorrectly()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          rawJson = """
                           {
                             "success": true,
                             "errors": [],
                             "messages": [],
                             "result": {
                               "id": "ssl",
                               "value": "full",
                               "editable": true,
                               "modified_on": "2024-01-15T10:30:00Z"
                             }
                           }
                           """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(rawJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSettingAsync(zoneId, "ssl");

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be("ssl");
    result.Value.GetString().Should().Be("full");
    result.Editable.Should().BeTrue();
    result.ModifiedOn.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
  }

  /// <summary>U10: Verifies that ZoneSetting with integer value deserializes correctly.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_IntegerValue_DeserializesCorrectly()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          rawJson = """
                           {
                             "success": true,
                             "errors": [],
                             "messages": [],
                             "result": {
                               "id": "browser_cache_ttl",
                               "value": 14400,
                               "editable": true,
                               "modified_on": null
                             }
                           }
                           """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(rawJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSettingAsync(zoneId, "browser_cache_ttl");

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be("browser_cache_ttl");
    result.Value.GetInt32().Should().Be(14400);
    result.Editable.Should().BeTrue();
  }

  /// <summary>U11: Verifies that ZoneSetting with on/off toggle deserializes correctly.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_OnOffToggle_DeserializesCorrectly()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          rawJson = """
                           {
                             "success": true,
                             "errors": [],
                             "messages": [],
                             "result": {
                               "id": "always_use_https",
                               "value": "on",
                               "editable": true
                             }
                           }
                           """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(rawJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSettingAsync(zoneId, "always_use_https");

    // Assert
    result.Should().NotBeNull();
    result.Value.GetString().Should().Be("on");
  }

  /// <summary>U12: Verifies that ZoneSetting with complex object value deserializes correctly.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_ComplexObject_DeserializesCorrectly()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          rawJson = """
                           {
                             "success": true,
                             "errors": [],
                             "messages": [],
                             "result": {
                               "id": "mobile_redirect",
                               "value": {
                                 "status": "on",
                                 "mobile_subdomain": "m",
                                 "strip_uri": true
                               },
                               "editable": true
                             }
                           }
                           """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(rawJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSettingAsync(zoneId, "mobile_redirect");

    // Assert
    result.Should().NotBeNull();
    result.Value.ValueKind.Should().Be(JsonValueKind.Object);
  }

  /// <summary>U13: Verifies that ZoneSetting with editable=false deserializes correctly.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_NotEditable_DeserializesCorrectly()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          rawJson = """
                           {
                             "success": true,
                             "errors": [],
                             "messages": [],
                             "result": {
                               "id": "advanced_ddos",
                               "value": "off",
                               "editable": false
                             }
                           }
                           """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(rawJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSettingAsync(zoneId, "advanced_ddos");

    // Assert
    result.Should().NotBeNull();
    result.Editable.Should().BeFalse();
  }

  /// <summary>U14: Verifies that ZoneSetting modified_on datetime parses correctly.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_ModifiedOn_ParsesCorrectly()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          rawJson = """
                           {
                             "success": true,
                             "errors": [],
                             "messages": [],
                             "result": {
                               "id": "ssl",
                               "value": "full",
                               "editable": true,
                               "modified_on": "2024-06-15T14:30:45Z"
                             }
                           }
                           """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(rawJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSettingAsync(zoneId, "ssl");

    // Assert
    result.Should().NotBeNull();
    result.ModifiedOn.Should().NotBeNull();
    result.ModifiedOn!.Value.Year.Should().Be(2024);
    result.ModifiedOn.Value.Month.Should().Be(6);
    result.ModifiedOn.Value.Day.Should().Be(15);
    result.ModifiedOn.Value.Hour.Should().Be(14);
    result.ModifiedOn.Value.Minute.Should().Be(30);
    result.ModifiedOn.Value.Second.Should().Be(45);
  }

  /// <summary>U15: Verifies that ZoneSetting with null modified_on deserializes correctly.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_NullModifiedOn_DeserializesCorrectly()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          rawJson = """
                           {
                             "success": true,
                             "errors": [],
                             "messages": [],
                             "result": {
                               "id": "development_mode",
                               "value": "off",
                               "editable": true,
                               "modified_on": null
                             }
                           }
                           """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(rawJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSettingAsync(zoneId, "development_mode");

    // Assert
    result.Should().NotBeNull();
    result.ModifiedOn.Should().BeNull();
  }

  #endregion


  #region Zone Settings Tests - JsonElement Value Access

  /// <summary>U16: Verifies that GetString works on string value.</summary>
  [Fact]
  public async Task ZoneSetting_GetString_OnStringValue_ReturnsString()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          rawJson = """
                           {
                             "success": true,
                             "errors": [],
                             "messages": [],
                             "result": {
                               "id": "ssl",
                               "value": "strict",
                               "editable": true
                             }
                           }
                           """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(rawJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSettingAsync(zoneId, "ssl");

    // Assert
    var stringValue = result.Value.GetString();
    stringValue.Should().Be("strict");
  }

  /// <summary>U17: Verifies that GetInt32 works on integer value.</summary>
  [Fact]
  public async Task ZoneSetting_GetInt32_OnIntegerValue_ReturnsInt()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          rawJson = """
                           {
                             "success": true,
                             "errors": [],
                             "messages": [],
                             "result": {
                               "id": "browser_cache_ttl",
                               "value": 7200,
                               "editable": true
                             }
                           }
                           """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(rawJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSettingAsync(zoneId, "browser_cache_ttl");

    // Assert
    var intValue = result.Value.GetInt32();
    intValue.Should().Be(7200);
  }

  /// <summary>U18: Verifies that GetBoolean works on nested property in complex object.</summary>
  [Fact]
  public async Task ZoneSetting_GetBoolean_OnNestedProperty_ReturnsBoolean()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          rawJson = """
                           {
                             "success": true,
                             "errors": [],
                             "messages": [],
                             "result": {
                               "id": "mobile_redirect",
                               "value": {
                                 "status": "on",
                                 "enabled": true
                               },
                               "editable": true
                             }
                           }
                           """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(rawJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSettingAsync(zoneId, "mobile_redirect");

    // Assert
    result.Value.TryGetProperty("enabled", out var enabledElem).Should().BeTrue();
    enabledElem.GetBoolean().Should().BeTrue();
  }

  /// <summary>U19: Verifies that TryGetProperty works on complex object.</summary>
  [Fact]
  public async Task ZoneSetting_TryGetProperty_OnComplexObject_ReturnsTrue()
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          rawJson = """
                           {
                             "success": true,
                             "errors": [],
                             "messages": [],
                             "result": {
                               "id": "custom_setting",
                               "value": {
                                 "key1": "value1",
                                 "key2": 123
                               },
                               "editable": true
                             }
                           }
                           """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(rawJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSettingAsync(zoneId, "custom_setting");

    // Assert
    result.Value.TryGetProperty("key1", out var key1Elem).Should().BeTrue();
    key1Elem.GetString().Should().Be("value1");
    result.Value.TryGetProperty("key2", out var key2Elem).Should().BeTrue();
    key2Elem.GetInt32().Should().Be(123);
    result.Value.TryGetProperty("nonexistent", out _).Should().BeFalse();
  }

  /// <summary>U20: Verifies that ValueKind check works correctly for different value types.</summary>
  [Theory]
  [InlineData("""{"id":"ssl","value":"full","editable":true}""", JsonValueKind.String)]
  [InlineData("""{"id":"ttl","value":3600,"editable":true}""", JsonValueKind.Number)]
  [InlineData("""{"id":"complex","value":{"enabled":true},"editable":true}""", JsonValueKind.Object)]
  public async Task ZoneSetting_ValueKind_ReturnsCorrectKind(string resultJson, JsonValueKind expectedKind)
  {
    // Arrange
    const string zoneId  = "zone-123";
    var          rawJson = $@"{{""success"":true,""errors"":[],""messages"":[],""result"":{resultJson}}}";

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(rawJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetZoneSettingAsync(zoneId, "test");

    // Assert
    result.Value.ValueKind.Should().Be(expectedKind);
  }

  #endregion


  #region Zone Settings Tests - Parameter Validation

  /// <summary>Verifies that GetZoneSettingAsync throws ArgumentNullException for null zoneId.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut         = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync<ZoneSetting>(
      () => sut.GetZoneSettingAsync(null!, "ssl"),
      "zoneId");
  }

  /// <summary>Verifies that GetZoneSettingAsync throws ArgumentException for empty zoneId.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_EmptyZoneId_ThrowsArgumentException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut         = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentEmptyAsync<ZoneSetting>(
      () => sut.GetZoneSettingAsync("", "ssl"),
      "zoneId");
  }

  /// <summary>Verifies that GetZoneSettingAsync throws ArgumentNullException for null settingId.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_NullSettingId_ThrowsArgumentNullException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut         = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync<ZoneSetting>(
      () => sut.GetZoneSettingAsync("zone-123", null!),
      "settingId");
  }

  /// <summary>Verifies that GetZoneSettingAsync throws ArgumentException for empty settingId.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_EmptySettingId_ThrowsArgumentException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut         = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentEmptyAsync<ZoneSetting>(
      () => sut.GetZoneSettingAsync("zone-123", ""),
      "settingId");
  }

  /// <summary>Verifies that SetZoneSettingAsync throws ArgumentNullException for null zoneId.</summary>
  [Fact]
  public async Task SetZoneSettingAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut         = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync<ZoneSetting>(
      () => sut.SetZoneSettingAsync(null!, "ssl", "full"),
      "zoneId");
  }

  /// <summary>Verifies that SetZoneSettingAsync throws ArgumentNullException for null settingId.</summary>
  [Fact]
  public async Task SetZoneSettingAsync_NullSettingId_ThrowsArgumentNullException()
  {
    // Arrange
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler("{}", HttpStatusCode.OK);
    var httpClient  = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut         = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync<ZoneSetting>(
      () => sut.SetZoneSettingAsync("zone-123", null!, "full"),
      "settingId");
  }

  #endregion


  #region Zone Settings Tests - Error Handling

  /// <summary>U21: Verifies that API error response is handled correctly.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_ApiError_ThrowsCloudflareApiException()
  {
    // Arrange
    const string zoneId    = "zone-123";
    const string settingId = "ssl";
    var          errorJson = """
                             {
                               "success": false,
                               "errors": [{"code": 1001, "message": "Zone not found"}],
                               "messages": [],
                               "result": null
                             }
                             """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(errorJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await CloudflareApiTestHelpers.AssertApiErrorAsync<ZoneSetting>(
      () => sut.GetZoneSettingAsync(zoneId, settingId),
      1001);
  }

  /// <summary>U22: Verifies that multiple errors in response are handled correctly.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_MultipleErrors_ThrowsWithAllErrors()
  {
    // Arrange
    const string zoneId    = "zone-123";
    const string settingId = "ssl";
    var          errorJson = """
                             {
                               "success": false,
                               "errors": [
                                 {"code": 1001, "message": "Error 1"},
                                 {"code": 1002, "message": "Error 2"}
                               ],
                               "messages": [],
                               "result": null
                             }
                             """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(errorJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await CloudflareApiTestHelpers.AssertMultipleApiErrorsAsync<ZoneSetting>(
      () => sut.GetZoneSettingAsync(zoneId, settingId),
      2);
  }

  /// <summary>U23: Verifies that setting not editable error is handled correctly.</summary>
  [Fact]
  public async Task SetZoneSettingAsync_NotEditable_ThrowsCloudflareApiException()
  {
    // Arrange
    const string zoneId    = "zone-123";
    const string settingId = "advanced_ddos";
    var          errorJson = """
                             {
                               "success": false,
                               "errors": [{"code": 1004, "message": "Setting not editable for your zone plan"}],
                               "messages": [],
                               "result": null
                             }
                             """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent(errorJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    var act = () => sut.SetZoneSettingAsync(zoneId, settingId, "on");
    await act.Should().ThrowAsync<HttpRequestException>();
  }

  /// <summary>U25: Verifies that invalid value type error is handled correctly.</summary>
  [Fact]
  public async Task SetZoneSettingAsync_InvalidValue_ThrowsCloudflareApiException()
  {
    // Arrange
    const string zoneId    = "zone-123";
    const string settingId = "ssl";
    var          errorJson = """
                             {
                               "success": false,
                               "errors": [{"code": 1005, "message": "Invalid value for setting"}],
                               "messages": [],
                               "result": null
                             }
                             """;

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, Content = new StringContent(errorJson) });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    var act = () => sut.SetZoneSettingAsync(zoneId, settingId, "invalid_mode");
    await act.Should().ThrowAsync<HttpRequestException>();
  }

  /// <summary>U26: Verifies that HTTP 401 Unauthorized is handled correctly.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_Unauthorized_ThrowsHttpRequestException()
  {
    // Arrange
    const string zoneId    = "zone-123";
    const string settingId = "ssl";

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, Content = new StringContent("") });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await CloudflareApiTestHelpers.AssertUnauthorizedAsync<ZoneSetting>(
      () => sut.GetZoneSettingAsync(zoneId, settingId));
  }

  /// <summary>U27: Verifies that HTTP 403 Forbidden is handled correctly.</summary>
  [Fact]
  public async Task GetZoneSettingAsync_Forbidden_ThrowsHttpRequestException()
  {
    // Arrange
    const string zoneId    = "zone-123";
    const string settingId = "ssl";

    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden, Content = new StringContent("") });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await CloudflareApiTestHelpers.AssertForbiddenAsync<ZoneSetting>(
      () => sut.GetZoneSettingAsync(zoneId, settingId));
  }

  #endregion
}
