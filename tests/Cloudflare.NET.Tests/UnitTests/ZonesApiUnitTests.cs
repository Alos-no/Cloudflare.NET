namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using NET.Security.Firewall.Models;
using Shared.Fixtures;
using Shared.Helpers;
using Shared.Mocks;
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


  #region Zone CRUD - Request Construction Tests

  /// <summary>U01: Verifies that ListZonesAsync sends correct GET request with no filters.</summary>
  [Fact]
  public async Task ListZonesAsync_NoFilters_SendsCorrectRequest()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone();
    var successResponse = HttpFixtures.CreatePaginatedResponse([zone], 1, 20, 1);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.ListZonesAsync();

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/zones");
  }

  /// <summary>U02: Verifies that ListZonesAsync sends correct GET request with all filters.</summary>
  [Fact]
  public async Task ListZonesAsync_AllFilters_SendsCorrectRequest()
  {
    // Arrange
    var zone    = TestDataFactory.CreateZone();
    var filters = new ListZonesFilters(
      Name: "example.com",
      Status: ZoneStatus.Active,
      AccountId: TestDataFactory.TestAccountId,
      AccountName: "Test Account",
      Page: 2,
      PerPage: 50,
      Order: ZoneOrderField.Name,
      Direction: ListOrderDirection.Ascending,
      Match: FilterMatch.All
    );
    var successResponse = HttpFixtures.CreatePaginatedResponse([zone], 2, 50, 10);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.ListZonesAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    var uri = capturedRequest.RequestUri!.ToString();
    uri.Should().Contain("name=example.com");
    uri.Should().Contain("status=active");
    uri.Should().Contain($"account.id={TestDataFactory.TestAccountId}");
    // Uri.ToString() decodes the query string, so check for both encoded and decoded forms
    uri.Should().Match("*account.name=Test*Account*");
    uri.Should().Contain("page=2");
    uri.Should().Contain("per_page=50");
    uri.Should().Contain("order=name");
    uri.Should().Contain("direction=asc");
    uri.Should().Contain("match=all");
  }

  /// <summary>U03: Verifies that ListZonesAsync sends correct GET request with status filter only.</summary>
  [Fact]
  public async Task ListZonesAsync_StatusFilter_SendsCorrectRequest()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone(status: "pending");
    var filters         = new ListZonesFilters(Status: ZoneStatus.Pending);
    var successResponse = HttpFixtures.CreatePaginatedResponse([zone], 1, 20, 1);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.ListZonesAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("status=pending");
  }

  /// <summary>U04: Verifies that ListZonesAsync sends correct GET request with account filter.</summary>
  [Fact]
  public async Task ListZonesAsync_AccountFilter_SendsCorrectRequest()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone();
    var filters         = new ListZonesFilters(AccountId: TestDataFactory.TestAccountId);
    var successResponse = HttpFixtures.CreatePaginatedResponse([zone], 1, 20, 1);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.ListZonesAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain($"account.id={TestDataFactory.TestAccountId}");
  }

  /// <summary>U05: Verifies that ListZonesAsync sends correct GET request with order by plan.id.</summary>
  [Fact]
  public async Task ListZonesAsync_OrderByPlan_SendsCorrectRequest()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone();
    var filters         = new ListZonesFilters(Order: ZoneOrderField.PlanId, Direction: ListOrderDirection.Descending);
    var successResponse = HttpFixtures.CreatePaginatedResponse([zone], 1, 20, 1);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.ListZonesAsync(filters);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.ToString().Should().Contain("order=plan.id");
    capturedRequest.RequestUri.ToString().Should().Contain("direction=desc");
  }

  /// <summary>U06: Verifies that CreateZoneAsync sends correct POST request.</summary>
  [Fact]
  public async Task CreateZoneAsync_SendsCorrectRequest()
  {
    // Arrange
    var zone    = TestDataFactory.CreateZone();
    var request = new CreateZoneRequest(
      Name: "newzone.com",
      Type: ZoneType.Full,
      Account: new ZoneAccountReference(TestDataFactory.TestAccountId)
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(zone);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateZoneAsync(request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.ToString().Should().Be("https://api.cloudflare.com/client/v4/zones");
    var content = await capturedRequest.Content!.ReadAsStringAsync();
    content.Should().Contain("\"name\":\"newzone.com\"");
    content.Should().Contain("\"type\":\"full\"");
    content.Should().Contain($"\"id\":\"{TestDataFactory.TestAccountId}\"");
    content.Should().NotContain("jump_start"); // null should be omitted
  }

  /// <summary>U07: Verifies that CreateZoneAsync with jump_start sends correct POST request.</summary>
  [Fact]
  public async Task CreateZoneAsync_WithJumpStart_SendsCorrectRequest()
  {
    // Arrange
    var zone    = TestDataFactory.CreateZone();
    var request = new CreateZoneRequest(
      Name: "newzone.com",
      Type: ZoneType.Full,
      Account: new ZoneAccountReference(TestDataFactory.TestAccountId),
      JumpStart: true
    );
    var successResponse = HttpFixtures.CreateSuccessResponse(zone);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateZoneAsync(request);

    // Assert
    capturedRequest.Should().NotBeNull();
    var content = await capturedRequest!.Content!.ReadAsStringAsync();
    content.Should().Contain("\"jump_start\":true");
  }

  /// <summary>U08: Verifies that EditZoneAsync with paused sends correct PATCH request.</summary>
  [Fact]
  public async Task EditZoneAsync_Paused_SendsCorrectRequest()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone();
    var request         = new EditZoneRequest(Paused: true);
    var successResponse = HttpFixtures.CreateSuccessResponse(zone);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.EditZoneAsync(TestDataFactory.TestZoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{TestDataFactory.TestZoneId}");
    var content = await capturedRequest.Content!.ReadAsStringAsync();
    content.Should().Be("{\"paused\":true}");
  }

  /// <summary>U09: Verifies that EditZoneAsync with type sends correct PATCH request.</summary>
  [Fact]
  public async Task EditZoneAsync_Type_SendsCorrectRequest()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone(type: "partial");
    var request         = new EditZoneRequest(Type: ZoneType.Partial);
    var successResponse = HttpFixtures.CreateSuccessResponse(zone);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.EditZoneAsync(TestDataFactory.TestZoneId, request);

    // Assert
    var content = await capturedRequest!.Content!.ReadAsStringAsync();
    content.Should().Be("{\"type\":\"partial\"}");
  }

  /// <summary>U10: Verifies that EditZoneAsync with vanity nameservers sends correct PATCH request.</summary>
  [Fact]
  public async Task EditZoneAsync_VanityNameServers_SendsCorrectRequest()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone();
    var request         = new EditZoneRequest(VanityNameServers: ["ns1.custom.com", "ns2.custom.com"]);
    var successResponse = HttpFixtures.CreateSuccessResponse(zone);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.EditZoneAsync(TestDataFactory.TestZoneId, request);

    // Assert
    var content = await capturedRequest!.Content!.ReadAsStringAsync();
    content.Should().Contain("\"vanity_name_servers\":[\"ns1.custom.com\",\"ns2.custom.com\"]");
  }

  /// <summary>U11: Verifies that EditZoneAsync with plan sends correct PATCH request.</summary>
  [Fact]
  public async Task EditZoneAsync_Plan_SendsCorrectRequest()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone();
    var request         = new EditZoneRequest(Plan: new ZonePlanReference("pro_plan_id"));
    var successResponse = HttpFixtures.CreateSuccessResponse(zone);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.EditZoneAsync(TestDataFactory.TestZoneId, request);

    // Assert
    var content = await capturedRequest!.Content!.ReadAsStringAsync();
    content.Should().Contain("\"plan\":{\"id\":\"pro_plan_id\"}");
  }

  /// <summary>U12: Verifies that DeleteZoneAsync sends correct DELETE request.</summary>
  [Fact]
  public async Task DeleteZoneAsync_SendsCorrectRequest()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteZoneAsync(TestDataFactory.TestZoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{TestDataFactory.TestZoneId}");
  }

  /// <summary>U13: Verifies that TriggerActivationCheckAsync sends correct PUT request.</summary>
  [Fact]
  public async Task TriggerActivationCheckAsync_SendsCorrectRequest()
  {
    // Arrange
    var result          = new { id = TestDataFactory.TestZoneId };
    var successResponse = HttpFixtures.CreateSuccessResponse(result);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.TriggerActivationCheckAsync(TestDataFactory.TestZoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.ToString().Should()
                   .Be($"https://api.cloudflare.com/client/v4/zones/{TestDataFactory.TestZoneId}/activation_check");
  }

  /// <summary>U14: Verifies that GetZoneDetailsAsync sends correct GET request.</summary>
  [Fact]
  public async Task GetZoneDetailsAsync_SendsCorrectRequest()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone();
    var successResponse = HttpFixtures.CreateSuccessResponse(zone);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.GetZoneDetailsAsync(TestDataFactory.TestZoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.ToString().Should().Be($"https://api.cloudflare.com/client/v4/zones/{TestDataFactory.TestZoneId}");
  }

  #endregion


  #region Zone CRUD - Parameter Validation Tests

  /// <summary>U15: Verifies that GetZoneDetailsAsync throws ArgumentNullException for null zoneId.</summary>
  [Fact]
  public async Task GetZoneDetailsAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZone());
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, null);
    var httpClient      = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut             = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync(
      () => sut.GetZoneDetailsAsync(null!),
      "zoneId");
  }

  /// <summary>U16: Verifies that GetZoneDetailsAsync throws ArgumentException for empty zoneId.</summary>
  [Fact]
  public async Task GetZoneDetailsAsync_EmptyZoneId_ThrowsArgumentException()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZone());
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, null);
    var httpClient      = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut             = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentEmptyAsync(
      () => sut.GetZoneDetailsAsync(""),
      "zoneId");
  }

  /// <summary>U17: Verifies that EditZoneAsync throws ArgumentNullException for null zoneId.</summary>
  [Fact]
  public async Task EditZoneAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZone());
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, null);
    var httpClient      = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut             = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync(
      () => sut.EditZoneAsync(null!, new EditZoneRequest(Paused: true)),
      "zoneId");
  }

  /// <summary>U18: Verifies that EditZoneAsync throws ArgumentNullException for null request.</summary>
  [Fact]
  public async Task EditZoneAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZone());
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, null);
    var httpClient      = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut             = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync(
      () => sut.EditZoneAsync(TestDataFactory.TestZoneId, null!),
      "request");
  }

  /// <summary>U19: Verifies that DeleteZoneAsync throws ArgumentNullException for null zoneId.</summary>
  [Fact]
  public async Task DeleteZoneAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, null);
    var httpClient      = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut             = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync(
      () => sut.DeleteZoneAsync(null!),
      "zoneId");
  }

  /// <summary>U20: Verifies that TriggerActivationCheckAsync throws ArgumentNullException for null zoneId.</summary>
  [Fact]
  public async Task TriggerActivationCheckAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse(new { id = TestDataFactory.TestZoneId });
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, null);
    var httpClient      = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut             = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync(
      () => sut.TriggerActivationCheckAsync(null!),
      "zoneId");
  }

  /// <summary>U21: Verifies that CreateZoneAsync throws ArgumentNullException for null request.</summary>
  [Fact]
  public async Task CreateZoneAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZone());
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, null);
    var httpClient      = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut             = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync(
      () => sut.CreateZoneAsync(null!),
      "request");
  }

  #endregion


  #region Zone CRUD - URL Encoding Tests

  /// <summary>U22: Verifies that special characters in zoneId are properly URL-encoded.</summary>
  [Fact]
  public async Task GetZoneDetailsAsync_SpecialCharactersInZoneId_UrlEncodesCorrectly()
  {
    // Arrange
    var zoneIdWithSpecialChars = "zone+id/with&special=chars";
    var zone                   = TestDataFactory.CreateZone();
    var successResponse        = HttpFixtures.CreateSuccessResponse(zone);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.GetZoneDetailsAsync(zoneIdWithSpecialChars);

    // Assert
    capturedRequest.Should().NotBeNull();
    var uri = capturedRequest!.RequestUri!.ToString();
    // The zoneId should be URL-encoded
    uri.Should().Contain("zone%2Bid%2Fwith%26special%3Dchars");
  }

  #endregion


  #region Zone CRUD - Response Deserialization Tests

  /// <summary>U23: Verifies that GetZoneDetailsAsync correctly deserializes a full Zone model.</summary>
  [Fact]
  public async Task GetZoneDetailsAsync_DeserializesFullZoneModel()
  {
    // Arrange
    var rawJson = """
                  {
                    "success": true,
                    "errors": [],
                    "messages": [],
                    "result": {
                      "id": "023e105f4ecef8ad9ca31a8372d0c353",
                      "name": "example.com",
                      "status": "active",
                      "account": { "id": "01a7362d577a6c3019a474fd6f485823", "name": "Test Account" },
                      "owner": { "id": "7c5dae5552338874e5053f2534d2767a", "name": "Test User", "type": "user" },
                      "plan": {
                        "id": "0feeeeeeeeeeeeeeeeeeeeeeeeeeeeee",
                        "name": "Free Website",
                        "price": 0,
                        "currency": "USD",
                        "frequency": "monthly",
                        "is_subscribed": true,
                        "can_subscribe": true,
                        "legacy_id": "free",
                        "legacy_discount": false,
                        "externally_managed": false
                      },
                      "meta": {
                        "step": 4,
                        "custom_certificate_quota": 1,
                        "page_rule_quota": 3,
                        "phishing_detected": false,
                        "cdn_only": false,
                        "dns_only": false,
                        "foundation_dns": false
                      },
                      "name_servers": ["ns1.example.com", "ns2.example.com"],
                      "original_name_servers": ["ns1.original.com"],
                      "original_registrar": "Registrar Inc",
                      "original_dnshost": "DNS Host Inc",
                      "type": "full",
                      "development_mode": 0,
                      "paused": false,
                      "permissions": ["#zone:read", "#zone:edit"],
                      "vanity_name_servers": ["ns1.custom.com"],
                      "cname_suffix": null,
                      "verification_key": "abc123",
                      "tenant": { "id": "tenant1", "name": "My Tenant" },
                      "tenant_unit": { "id": "unit1" },
                      "activated_on": "2024-01-15T10:30:00Z",
                      "created_on": "2024-01-01T00:00:00Z",
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
    var result = await sut.GetZoneDetailsAsync("023e105f4ecef8ad9ca31a8372d0c353");

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be("023e105f4ecef8ad9ca31a8372d0c353");
    result.Name.Should().Be("example.com");
    result.Status.Should().Be(ZoneStatus.Active);
    result.Type.Should().Be(ZoneType.Full);
    result.Paused.Should().BeFalse();
    result.DevelopmentMode.Should().Be(0);

    // Account
    result.Account.Should().NotBeNull();
    result.Account.Id.Should().Be("01a7362d577a6c3019a474fd6f485823");
    result.Account.Name.Should().Be("Test Account");

    // Owner
    result.Owner.Should().NotBeNull();
    result.Owner.Id.Should().Be("7c5dae5552338874e5053f2534d2767a");
    result.Owner.Name.Should().Be("Test User");
    result.Owner.Type.Should().Be("user");

    // Plan
    result.Plan.Should().NotBeNull();
    result.Plan.Id.Should().Be("0feeeeeeeeeeeeeeeeeeeeeeeeeeeeee");
    result.Plan.Name.Should().Be("Free Website");
    result.Plan.Price.Should().Be(0);
    result.Plan.Currency.Should().Be("USD");
    result.Plan.Frequency.Should().Be("monthly");
    result.Plan.IsSubscribed.Should().BeTrue();
    result.Plan.CanSubscribe.Should().BeTrue();
    result.Plan.LegacyId.Should().Be("free");
    result.Plan.LegacyDiscount.Should().BeFalse();
    result.Plan.ExternallyManaged.Should().BeFalse();

    // Meta
    result.Meta.Should().NotBeNull();
    result.Meta!.Step.Should().Be(4);
    result.Meta.CustomCertificateQuota.Should().Be(1);
    result.Meta.PageRuleQuota.Should().Be(3);
    result.Meta.PhishingDetected.Should().BeFalse();
    result.Meta.CdnOnly.Should().BeFalse();
    result.Meta.DnsOnly.Should().BeFalse();
    result.Meta.FoundationDns.Should().BeFalse();

    // Name servers
    result.NameServers.Should().BeEquivalentTo(["ns1.example.com", "ns2.example.com"]);
    result.OriginalNameServers.Should().BeEquivalentTo(["ns1.original.com"]);
    result.VanityNameServers.Should().BeEquivalentTo(["ns1.custom.com"]);

    // Other fields
    result.OriginalRegistrar.Should().Be("Registrar Inc");
    result.OriginalDnsHost.Should().Be("DNS Host Inc");
    result.VerificationKey.Should().Be("abc123");
    result.Permissions.Should().BeEquivalentTo(["#zone:read", "#zone:edit"]);

    // Tenant
    result.Tenant.Should().NotBeNull();
    result.Tenant!.Id.Should().Be("tenant1");
    result.Tenant.Name.Should().Be("My Tenant");
    result.TenantUnit.Should().NotBeNull();
    result.TenantUnit!.Id.Should().Be("unit1");

    // Timestamps
    result.ActivatedOn.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
    result.CreatedOn.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    result.ModifiedOn.Should().Be(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
  }

  /// <summary>U24: Verifies that Zone with optional fields null deserializes correctly.</summary>
  [Fact]
  public async Task GetZoneDetailsAsync_OptionalFieldsNull_DeserializesCorrectly()
  {
    // Arrange
    var rawJson = """
                  {
                    "success": true,
                    "errors": [],
                    "messages": [],
                    "result": {
                      "id": "023e105f4ecef8ad9ca31a8372d0c353",
                      "name": "example.com",
                      "status": "pending",
                      "account": { "id": "01a7362d577a6c3019a474fd6f485823", "name": "Test Account" },
                      "owner": { "id": null, "name": null, "type": null },
                      "plan": {
                        "id": "free",
                        "name": "Free",
                        "price": 0,
                        "currency": "USD",
                        "frequency": null,
                        "is_subscribed": true,
                        "can_subscribe": true,
                        "legacy_id": null,
                        "legacy_discount": false,
                        "externally_managed": false
                      },
                      "meta": null,
                      "name_servers": ["ns1.example.com"],
                      "original_name_servers": null,
                      "original_registrar": null,
                      "original_dnshost": null,
                      "type": "full",
                      "development_mode": 0,
                      "paused": false,
                      "permissions": null,
                      "vanity_name_servers": null,
                      "cname_suffix": null,
                      "verification_key": null,
                      "tenant": null,
                      "tenant_unit": null,
                      "activated_on": null,
                      "created_on": "2024-01-01T00:00:00Z",
                      "modified_on": "2024-01-01T00:00:00Z"
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
    var result = await sut.GetZoneDetailsAsync("023e105f4ecef8ad9ca31a8372d0c353");

    // Assert
    result.Should().NotBeNull();
    result.Status.Should().Be(ZoneStatus.Pending);
    result.ActivatedOn.Should().BeNull();
    result.Meta.Should().BeNull();
    result.OriginalNameServers.Should().BeNull();
    result.OriginalRegistrar.Should().BeNull();
    result.OriginalDnsHost.Should().BeNull();
    result.Permissions.Should().BeNull();
    result.VanityNameServers.Should().BeNull();
    result.CnameSuffix.Should().BeNull();
    result.VerificationKey.Should().BeNull();
    result.Tenant.Should().BeNull();
    result.TenantUnit.Should().BeNull();
    result.Owner.Id.Should().BeNull();
    result.Owner.Name.Should().BeNull();
    result.Owner.Type.Should().BeNull();
    result.Plan.Frequency.Should().BeNull();
    result.Plan.LegacyId.Should().BeNull();
  }

  /// <summary>U25: Verifies that extensible enum ZoneType handles unknown values.</summary>
  [Fact]
  public async Task GetZoneDetailsAsync_UnknownZoneType_DeserializesAsCustomValue()
  {
    // Arrange
    var rawJson = """
                  {
                    "success": true,
                    "errors": [],
                    "messages": [],
                    "result": {
                      "id": "023e105f4ecef8ad9ca31a8372d0c353",
                      "name": "example.com",
                      "status": "active",
                      "account": { "id": "01a7362d577a6c3019a474fd6f485823", "name": "Test Account" },
                      "owner": { "id": "owner1" },
                      "plan": {
                        "id": "free",
                        "name": "Free",
                        "price": 0,
                        "currency": "USD",
                        "is_subscribed": true,
                        "can_subscribe": true,
                        "legacy_discount": false,
                        "externally_managed": false
                      },
                      "name_servers": [],
                      "type": "future_unknown_type",
                      "development_mode": 0,
                      "paused": false,
                      "created_on": "2024-01-01T00:00:00Z",
                      "modified_on": "2024-01-01T00:00:00Z"
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
    var result = await sut.GetZoneDetailsAsync("023e105f4ecef8ad9ca31a8372d0c353");

    // Assert
    result.Should().NotBeNull();
    result.Type.Value.Should().Be("future_unknown_type");
    // Extensible enum should not match known types
    result.Type.Should().NotBe(ZoneType.Full);
    result.Type.Should().NotBe(ZoneType.Partial);
    result.Type.Should().NotBe(ZoneType.Secondary);
  }

  /// <summary>U26: Verifies that TriggerActivationCheckAsync correctly deserializes the result.</summary>
  [Fact]
  public async Task TriggerActivationCheckAsync_DeserializesResult()
  {
    // Arrange
    var rawJson = """
                  {
                    "success": true,
                    "errors": [],
                    "messages": [],
                    "result": {
                      "id": "023e105f4ecef8ad9ca31a8372d0c353"
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
    var result = await sut.TriggerActivationCheckAsync("023e105f4ecef8ad9ca31a8372d0c353");

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be("023e105f4ecef8ad9ca31a8372d0c353");
  }

  #endregion


  #region Zone CRUD - Pagination Tests

  /// <summary>U27: Verifies that ListAllZonesAsync handles page-based pagination correctly.</summary>
  [Fact]
  public async Task ListAllZonesAsync_ShouldHandlePaginationCorrectly()
  {
    // Arrange
    var zone1 = TestDataFactory.CreateZone(id: "zone1", name: "zone1.example.com");
    var zone2 = TestDataFactory.CreateZone(id: "zone2", name: "zone2.example.com");

    var responsePage1 = JsonSerializer.Serialize(
      new
      {
        success     = true,
        errors      = Array.Empty<object>(),
        messages    = Array.Empty<object>(),
        result      = new[] { zone1 },
        result_info = new { page = 1, per_page = 1, count = 1, total_pages = 2, total_count = 2 }
      },
      _serializerOptions);

    var responsePage2 = JsonSerializer.Serialize(
      new
      {
        success     = true,
        errors      = Array.Empty<object>(),
        messages    = Array.Empty<object>(),
        result      = new[] { zone2 },
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
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var allZones = new List<Zone>();
    await foreach (var zone in sut.ListAllZonesAsync(new ListZonesFilters { PerPage = 1 }))
      allZones.Add(zone);

    // Assert
    capturedRequests.Should().HaveCount(2);
    capturedRequests[0].RequestUri!.Query.Should().Contain("page=1");
    capturedRequests[1].RequestUri!.Query.Should().Contain("page=2");
    allZones.Should().HaveCount(2);
    allZones.Select(z => z.Name).Should().ContainInOrder("zone1.example.com", "zone2.example.com");
  }

  /// <summary>U28: Verifies that ListZonesAsync returns correct pagination info.</summary>
  [Fact]
  public async Task ListZonesAsync_ReturnsPaginationInfo()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone();
    var successResponse = HttpFixtures.CreatePaginatedResponse([zone], 2, 50, 100);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, null);
    var httpClient      = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut             = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListZonesAsync(new ListZonesFilters { Page = 2, PerPage = 50 });

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().HaveCount(1);
    result.PageInfo.Should().NotBeNull();
    result.PageInfo!.Page.Should().Be(2);
    result.PageInfo.PerPage.Should().Be(50);
    result.PageInfo.TotalCount.Should().Be(100);
  }

  /// <summary>U29: Verifies that ListZonesAsync handles empty result.</summary>
  [Fact]
  public async Task ListZonesAsync_EmptyResult_ReturnsEmptyList()
  {
    // Arrange
    var successResponse = HttpFixtures.CreatePaginatedResponse(Array.Empty<object>(), 1, 20, 0);
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, null);
    var httpClient      = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut             = new ZonesApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListZonesAsync();

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().BeEmpty();
  }

  #endregion


  #region Zone CRUD - Convenience Method Tests

  /// <summary>U30: Verifies that SetZonePausedAsync sends correct request.</summary>
  [Fact]
  public async Task SetZonePausedAsync_SendsCorrectRequest()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone();
    var successResponse = HttpFixtures.CreateSuccessResponse(zone);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.SetZonePausedAsync(TestDataFactory.TestZoneId, true);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Patch);
    var content = await capturedRequest.Content!.ReadAsStringAsync();
    content.Should().Be("{\"paused\":true}");
  }

  /// <summary>U31: Verifies that SetZoneTypeAsync sends correct request.</summary>
  [Fact]
  public async Task SetZoneTypeAsync_SendsCorrectRequest()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone(type: "secondary");
    var successResponse = HttpFixtures.CreateSuccessResponse(zone);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.SetZoneTypeAsync(TestDataFactory.TestZoneId, ZoneType.Secondary);

    // Assert
    var content = await capturedRequest!.Content!.ReadAsStringAsync();
    content.Should().Be("{\"type\":\"secondary\"}");
  }

  /// <summary>U32: Verifies that SetVanityNameServersAsync sends correct request.</summary>
  [Fact]
  public async Task SetVanityNameServersAsync_SendsCorrectRequest()
  {
    // Arrange
    var zone            = TestDataFactory.CreateZone();
    var successResponse = HttpFixtures.CreateSuccessResponse(zone);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler =
      HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut        = new ZonesApi(httpClient, _loggerFactory);

    // Act
    await sut.SetVanityNameServersAsync(TestDataFactory.TestZoneId, ["ns1.custom.com", "ns2.custom.com"]);

    // Assert
    var content = await capturedRequest!.Content!.ReadAsStringAsync();
    content.Should().Contain("\"vanity_name_servers\":[\"ns1.custom.com\",\"ns2.custom.com\"]");
  }

  /// <summary>U33: Verifies that SetVanityNameServersAsync throws for null nameservers.</summary>
  [Fact]
  public async Task SetVanityNameServersAsync_NullNameservers_ThrowsArgumentNullException()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse(TestDataFactory.CreateZone());
    var mockHandler     = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, null);
    var httpClient      = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut             = new ZonesApi(httpClient, _loggerFactory);

    // Act & Assert
    await ParameterValidationTestHelpers.AssertThrowsArgumentNullAsync(
      () => sut.SetVanityNameServersAsync(TestDataFactory.TestZoneId, null!),
      "nameservers");
  }

  #endregion

}
