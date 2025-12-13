namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Cloudflare.NET.Core.Exceptions;
using Cloudflare.NET.Security.Firewall.Models;
using Cloudflare.NET.Turnstile;
using Cloudflare.NET.Turnstile.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Shared.Fixtures;
using Xunit.Abstractions;


/// <summary>
///   Contains unit tests for <see cref="TurnstileApi" />.
///   <para>
///     Tests verify request construction, URL encoding, response deserialization,
///     pagination behavior, and error handling for all Turnstile API operations.
///   </para>
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class TurnstileApiUnitTests
{
  #region Properties & Fields - Non-Public

  /// <summary>The logger factory for creating loggers in tests.</summary>
  private readonly ILoggerFactory _loggerFactory;

  /// <summary>The test account ID used in tests.</summary>
  private const string TestAccountId = "test-account-id";

  /// <summary>The test sitekey used in tests.</summary>
  private const string TestSitekey = "0x4AAAAAAA_test_sitekey";

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="TurnstileApiUnitTests" /> class.</summary>
  /// <param name="output">The xUnit test output helper.</param>
  public TurnstileApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Request Construction Tests (U01-U10)

  /// <summary>U01: Verifies ListWidgetsAsync sends correct request without filters.</summary>
  [Fact]
  public async Task ListWidgetsAsync_NoFilters_SendsCorrectRequest()
  {
    // Arrange
    var responseJson = HttpFixtures.CreatePaginatedResponse(Array.Empty<TurnstileWidget>(), 1, 25, 0);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK, (req, _) => capturedRequest = req);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    await sut.ListWidgetsAsync(TestAccountId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{TestAccountId}/challenges/widgets");
    capturedRequest.RequestUri.Query.Should().BeEmpty();
  }

  /// <summary>U02: Verifies ListWidgetsAsync sends correct query parameters with pagination filters.</summary>
  [Fact]
  public async Task ListWidgetsAsync_WithPaginationFilters_IncludesQueryParameters()
  {
    // Arrange
    var responseJson = HttpFixtures.CreatePaginatedResponse(Array.Empty<TurnstileWidget>(), 2, 10, 0);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK, (req, _) => capturedRequest = req);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);
    var filters = new ListTurnstileWidgetsFilters(Page: 2, PerPage: 10);

    // Act
    await sut.ListWidgetsAsync(TestAccountId, filters);

    // Assert
    var query = capturedRequest!.RequestUri!.Query;
    query.Should().Contain("page=2");
    query.Should().Contain("per_page=10");
  }

  /// <summary>U03: Verifies ListWidgetsAsync sends correct order and direction parameters.</summary>
  [Fact]
  public async Task ListWidgetsAsync_WithOrderFilters_IncludesOrderParameters()
  {
    // Arrange
    var responseJson = HttpFixtures.CreatePaginatedResponse(Array.Empty<TurnstileWidget>(), 1, 25, 0);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK, (req, _) => capturedRequest = req);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);
    var filters = new ListTurnstileWidgetsFilters(Order: TurnstileOrderField.CreatedOn, Direction: ListOrderDirection.Descending);

    // Act
    await sut.ListWidgetsAsync(TestAccountId, filters);

    // Assert
    var query = capturedRequest!.RequestUri!.Query;
    query.Should().Contain("order=created_on");
    query.Should().Contain("direction=desc");
  }

  /// <summary>U04: Verifies GetWidgetAsync sends correct GET request.</summary>
  [Fact]
  public async Task GetWidgetAsync_SendsCorrectRequest()
  {
    // Arrange
    var widget = CreateTestWidget();
    var responseJson = HttpFixtures.CreateSuccessResponse(widget);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK, (req, _) => capturedRequest = req);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    await sut.GetWidgetAsync(TestAccountId, TestSitekey);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{TestAccountId}/challenges/widgets/{TestSitekey}");
  }

  /// <summary>U05: Verifies CreateWidgetAsync sends correct POST request with basic fields.</summary>
  [Fact]
  public async Task CreateWidgetAsync_BasicRequest_SendsCorrectRequest()
  {
    // Arrange
    var widget = CreateTestWidget(secret: "test-secret");
    var responseJson = HttpFixtures.CreateSuccessResponse(widget);
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);
    var request = new CreateTurnstileWidgetRequest(
      Name: "Test Widget",
      Domains: ["example.com"],
      Mode: WidgetMode.Managed);

    // Act
    await sut.CreateWidgetAsync(TestAccountId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{TestAccountId}/challenges/widgets");
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("\"name\"");
    capturedBody.Should().Contain("Test Widget");
    capturedBody.Should().Contain("\"domains\"");
    capturedBody.Should().Contain("example.com");
    capturedBody.Should().Contain("\"mode\"");
    capturedBody.Should().Contain("managed");
  }

  /// <summary>U06: Verifies CreateWidgetAsync includes all optional fields when provided.</summary>
  [Fact]
  public async Task CreateWidgetAsync_AllFields_IncludesAllFields()
  {
    // Arrange
    var widget = CreateTestWidget();
    var responseJson = HttpFixtures.CreateSuccessResponse(widget);
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);
    var request = new CreateTurnstileWidgetRequest(
      Name: "Test Widget",
      Domains: ["example.com", "api.example.com"],
      Mode: WidgetMode.Invisible,
      BotFightMode: true,
      ClearanceLevel: ClearanceLevel.Managed,
      EphemeralId: true,
      Offlabel: true,
      Region: "world");

    // Act
    await sut.CreateWidgetAsync(TestAccountId, request);

    // Assert
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("\"bot_fight_mode\":true");
    capturedBody.Should().Contain("\"clearance_level\":\"managed\"");
    capturedBody.Should().Contain("\"ephemeral_id\":true");
    capturedBody.Should().Contain("\"offlabel\":true");
    capturedBody.Should().Contain("\"region\":\"world\"");
  }

  /// <summary>U07: Verifies UpdateWidgetAsync sends correct PUT request.</summary>
  [Fact]
  public async Task UpdateWidgetAsync_SendsCorrectRequest()
  {
    // Arrange
    var widget = CreateTestWidget();
    var responseJson = HttpFixtures.CreateSuccessResponse(widget);
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);
    var request = new UpdateTurnstileWidgetRequest(
      Name: "Updated Widget",
      Domains: ["updated.example.com"],
      Mode: WidgetMode.NonInteractive);

    // Act
    await sut.UpdateWidgetAsync(TestAccountId, TestSitekey, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{TestAccountId}/challenges/widgets/{TestSitekey}");
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("Updated Widget");
    capturedBody.Should().Contain("updated.example.com");
    capturedBody.Should().Contain("non-interactive");
  }

  /// <summary>U08: Verifies DeleteWidgetAsync sends correct DELETE request.</summary>
  [Fact]
  public async Task DeleteWidgetAsync_SendsCorrectRequest()
  {
    // Arrange
    var responseJson = HttpFixtures.CreateSuccessResponse(new { });
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK, (req, _) => capturedRequest = req);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteWidgetAsync(TestAccountId, TestSitekey);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{TestAccountId}/challenges/widgets/{TestSitekey}");
  }

  /// <summary>U09: Verifies RotateSecretAsync sends correct request with grace period.</summary>
  [Fact]
  public async Task RotateSecretAsync_GracePeriod_SendsCorrectRequest()
  {
    // Arrange
    var result = new RotateWidgetSecretResult("new-secret");
    var responseJson = HttpFixtures.CreateSuccessResponse(result);
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    await sut.RotateSecretAsync(TestAccountId, TestSitekey, invalidateImmediately: false);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/accounts/{TestAccountId}/challenges/widgets/{TestSitekey}/rotate_secret");
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("\"invalidate_immediately\":false");
  }

  /// <summary>U10: Verifies RotateSecretAsync sends correct request with immediate invalidation.</summary>
  [Fact]
  public async Task RotateSecretAsync_Immediate_SendsCorrectRequest()
  {
    // Arrange
    var result = new RotateWidgetSecretResult("new-secret");
    var responseJson = HttpFixtures.CreateSuccessResponse(result);
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    await sut.RotateSecretAsync(TestAccountId, TestSitekey, invalidateImmediately: true);

    // Assert
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("\"invalidate_immediately\":true");
  }

  #endregion


  #region Response Deserialization Tests (U11-U19)

  /// <summary>U11: Verifies TurnstileWidget full model deserializes correctly.</summary>
  [Fact]
  public async Task ListWidgetsAsync_FullModel_DeserializesCorrectly()
  {
    // Arrange
    // Use raw JSON to ensure proper serialization of extensible enums.
    var responseJson = """
      {
        "success": true,
        "errors": [],
        "messages": [],
        "result": [{
          "sitekey": "0x4AAAAAAAA...",
          "name": "My Widget",
          "mode": "managed",
          "domains": ["example.com", "www.example.com"],
          "created_on": "2024-01-15T12:00:00Z",
          "modified_on": "2024-01-20T14:30:00Z",
          "bot_fight_mode": true,
          "clearance_level": "interactive",
          "ephemeral_id": true,
          "offlabel": true,
          "region": "world"
        }],
        "result_info": { "page": 1, "per_page": 25, "count": 1, "total_count": 1, "total_pages": 1 }
      }
      """;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListWidgetsAsync(TestAccountId);

    // Assert
    result.Items.Should().HaveCount(1);
    var actual = result.Items[0];
    actual.Sitekey.Should().Be("0x4AAAAAAAA...");
    actual.Name.Should().Be("My Widget");
    actual.Mode.Should().Be(WidgetMode.Managed);
    actual.Domains.Should().BeEquivalentTo(["example.com", "www.example.com"]);
    actual.BotFightMode.Should().BeTrue();
    actual.ClearanceLevel.Should().Be(ClearanceLevel.Interactive);
    actual.EphemeralId.Should().BeTrue();
    actual.Offlabel.Should().BeTrue();
    actual.Region.Should().Be("world");
  }

  /// <summary>U12: Verifies TurnstileWidget with secret deserializes correctly on create.</summary>
  [Fact]
  public async Task CreateWidgetAsync_WithSecret_DeserializesSecret()
  {
    // Arrange
    var widget = CreateTestWidget(secret: "0x4AAAAAAA...secret");
    var responseJson = HttpFixtures.CreateSuccessResponse(widget);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);
    var request = new CreateTurnstileWidgetRequest("Test", ["example.com"], WidgetMode.Managed);

    // Act
    var result = await sut.CreateWidgetAsync(TestAccountId, request);

    // Assert
    result.Secret.Should().Be("0x4AAAAAAA...secret");
  }

  /// <summary>U13: Verifies TurnstileWidget without secret deserializes correctly on get.</summary>
  [Fact]
  public async Task GetWidgetAsync_WithoutSecret_DeserializesNullSecret()
  {
    // Arrange
    var widget = CreateTestWidget(secret: null);
    var responseJson = HttpFixtures.CreateSuccessResponse(widget);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetWidgetAsync(TestAccountId, TestSitekey);

    // Assert
    result.Secret.Should().BeNull();
  }

  /// <summary>U14: Verifies WidgetMode.Invisible deserializes correctly.</summary>
  [Fact]
  public async Task GetWidgetAsync_InvisibleMode_DeserializesCorrectly()
  {
    // Arrange
    var widget = CreateTestWidget(mode: WidgetMode.Invisible);
    var responseJson = HttpFixtures.CreateSuccessResponse(widget);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetWidgetAsync(TestAccountId, TestSitekey);

    // Assert
    result.Mode.Should().Be(WidgetMode.Invisible);
  }

  /// <summary>U15: Verifies WidgetMode.Managed deserializes correctly.</summary>
  [Fact]
  public async Task GetWidgetAsync_ManagedMode_DeserializesCorrectly()
  {
    // Arrange
    var widget = CreateTestWidget(mode: WidgetMode.Managed);
    var responseJson = HttpFixtures.CreateSuccessResponse(widget);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetWidgetAsync(TestAccountId, TestSitekey);

    // Assert
    result.Mode.Should().Be(WidgetMode.Managed);
  }

  /// <summary>U16: Verifies WidgetMode.NonInteractive deserializes correctly.</summary>
  [Fact]
  public async Task GetWidgetAsync_NonInteractiveMode_DeserializesCorrectly()
  {
    // Arrange
    var widget = CreateTestWidget(mode: WidgetMode.NonInteractive);
    var responseJson = HttpFixtures.CreateSuccessResponse(widget);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetWidgetAsync(TestAccountId, TestSitekey);

    // Assert
    result.Mode.Should().Be(WidgetMode.NonInteractive);
  }

  /// <summary>U17: Verifies unknown WidgetMode value is preserved as extensible enum.</summary>
  [Fact]
  public async Task GetWidgetAsync_UnknownMode_PreservesValue()
  {
    // Arrange
    var widget = CreateTestWidget(mode: new WidgetMode("future-mode"));
    var responseJson = HttpFixtures.CreateSuccessResponse(widget);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetWidgetAsync(TestAccountId, TestSitekey);

    // Assert
    result.Mode.Value.Should().Be("future-mode");
  }

  /// <summary>U18: Verifies all ClearanceLevel values deserialize correctly.</summary>
  [Theory]
  [InlineData("no_clearance")]
  [InlineData("jschallenge")]
  [InlineData("managed")]
  [InlineData("interactive")]
  public async Task GetWidgetAsync_ClearanceLevel_DeserializesCorrectly(string clearanceValue)
  {
    // Arrange
    var widget = CreateTestWidget(clearanceLevel: new ClearanceLevel(clearanceValue));
    var responseJson = HttpFixtures.CreateSuccessResponse(widget);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetWidgetAsync(TestAccountId, TestSitekey);

    // Assert
    result.ClearanceLevel!.Value.Value.Should().Be(clearanceValue);
  }

  /// <summary>U19: Verifies RotateWidgetSecretResult deserializes correctly.</summary>
  [Fact]
  public async Task RotateSecretAsync_DeserializesSecret()
  {
    // Arrange
    var result = new RotateWidgetSecretResult("new-secret-value");
    var responseJson = HttpFixtures.CreateSuccessResponse(result);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var actual = await sut.RotateSecretAsync(TestAccountId, TestSitekey);

    // Assert
    actual.Secret.Should().Be("new-secret-value");
  }

  #endregion


  #region Pagination Tests (U20-U21)

  /// <summary>U20: Verifies ListAllWidgetsAsync handles single page correctly.</summary>
  [Fact]
  public async Task ListAllWidgetsAsync_SinglePage_YieldsAllItems()
  {
    // Arrange
    var widgets = new[] { CreateTestWidget("widget1"), CreateTestWidget("widget2") };
    var responseJson = HttpFixtures.CreatePaginatedResponse(widgets, 1, 25, 2);
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var results = new List<TurnstileWidget>();
    await foreach (var widget in sut.ListAllWidgetsAsync(TestAccountId))
    {
      results.Add(widget);
    }

    // Assert
    results.Should().HaveCount(2);
    results.Select(w => w.Sitekey).Should().Contain(["widget1", "widget2"]);
  }

  /// <summary>U21: Verifies ListAllWidgetsAsync handles multiple pages correctly.</summary>
  [Fact]
  public async Task ListAllWidgetsAsync_MultiplePages_YieldsAllItems()
  {
    // Arrange
    var page1Widgets = new[] { CreateTestWidget("widget1") };
    var page2Widgets = new[] { CreateTestWidget("widget2") };
    var page3Widgets = new[] { CreateTestWidget("widget3") };
    var responses = new[]
    {
      HttpFixtures.CreatePaginatedResponse(page1Widgets, 1, 1, 3),
      HttpFixtures.CreatePaginatedResponse(page2Widgets, 2, 1, 3),
      HttpFixtures.CreatePaginatedResponse(page3Widgets, 3, 1, 3)
    };
    var callIndex = 0;
    var mockHandler = new Mock<HttpMessageHandler>();
    mockHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(responses[Math.Min(callIndex++, responses.Length - 1)], System.Text.Encoding.UTF8, "application/json")
      });
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var results = new List<TurnstileWidget>();
    await foreach (var widget in sut.ListAllWidgetsAsync(TestAccountId))
    {
      results.Add(widget);
    }

    // Assert
    results.Should().HaveCount(3);
    results.Select(w => w.Sitekey).Should().ContainInOrder("widget1", "widget2", "widget3");
  }

  #endregion


  #region Error Handling Tests (U22-U24)

  /// <summary>U22: Verifies CreateWidgetAsync throws CloudflareApiException on API error.</summary>
  [Fact]
  public async Task CreateWidgetAsync_ApiError_ThrowsCloudflareApiException()
  {
    // Arrange - 2xx status with success=false throws CloudflareApiException.
    var responseJson = HttpFixtures.CreateErrorResponse(1001, "Invalid widget configuration");
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);
    var request = new CreateTurnstileWidgetRequest("Test", ["example.com"], WidgetMode.Managed);

    // Act
    var action = async () => await sut.CreateWidgetAsync(TestAccountId, request);

    // Assert
    var exception = await action.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().ContainSingle(e => e.Code == 1001);
  }

  /// <summary>U23: Verifies API returns multiple errors they are all captured.</summary>
  [Fact]
  public async Task CreateWidgetAsync_MultipleErrors_CapturesAllErrors()
  {
    // Arrange - 2xx status with success=false and multiple errors.
    var responseJson = JsonSerializer.Serialize(new
    {
      success = false,
      errors = new[]
      {
        new { code = 1001, message = "Error 1" },
        new { code = 1002, message = "Error 2" }
      },
      messages = Array.Empty<object>(),
      result = (object?)null
    });
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);
    var request = new CreateTurnstileWidgetRequest("Test", ["example.com"], WidgetMode.Managed);

    // Act
    var action = async () => await sut.CreateWidgetAsync(TestAccountId, request);

    // Assert
    var exception = await action.Should().ThrowAsync<CloudflareApiException>();
    exception.Which.Errors.Should().HaveCount(2);
    exception.Which.Errors.Should().Contain(e => e.Code == 1001);
    exception.Which.Errors.Should().Contain(e => e.Code == 1002);
  }

  #endregion


  #region URL Encoding Tests (U25-U26)

  /// <summary>U25: Verifies ListWidgetsAsync URL encodes accountId with special characters.</summary>
  [Fact]
  public async Task ListWidgetsAsync_SpecialCharsInAccountId_UrlEncodesCorrectly()
  {
    // Arrange
    var responseJson = HttpFixtures.CreatePaginatedResponse(Array.Empty<TurnstileWidget>(), 1, 25, 0);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK, (req, _) => capturedRequest = req);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    await sut.ListWidgetsAsync("test/account+id");

    // Assert
    var path = capturedRequest!.RequestUri!.AbsolutePath;
    path.Should().Contain("test%2Faccount%2Bid");
    path.Should().NotContain("test/account+id");
  }

  /// <summary>U26: Verifies GetWidgetAsync URL encodes sitekey with special characters.</summary>
  [Fact]
  public async Task GetWidgetAsync_SpecialCharsInSitekey_UrlEncodesCorrectly()
  {
    // Arrange
    var widget = CreateTestWidget();
    var responseJson = HttpFixtures.CreateSuccessResponse(widget);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK, (req, _) => capturedRequest = req);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    await sut.GetWidgetAsync(TestAccountId, "site/key+value");

    // Assert
    var path = capturedRequest!.RequestUri!.AbsolutePath;
    path.Should().Contain("site%2Fkey%2Bvalue");
    path.Should().NotContain("site/key+value");
  }

  #endregion


  #region Parameter Validation Tests (U33-U40)

  /// <summary>U33: Verifies ListWidgetsAsync throws ArgumentException for null accountId.</summary>
  [Fact]
  public async Task ListWidgetsAsync_NullAccountId_ThrowsArgumentNullException()
  {
    // Arrange
    var responseJson = HttpFixtures.CreateSuccessResponse(new { });
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.ListWidgetsAsync(null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentNullException>();
  }

  /// <summary>U34: Verifies ListWidgetsAsync throws ArgumentException for whitespace accountId.</summary>
  [Fact]
  public async Task ListWidgetsAsync_WhitespaceAccountId_ThrowsArgumentException()
  {
    // Arrange
    var responseJson = HttpFixtures.CreateSuccessResponse(new { });
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.ListWidgetsAsync("   ");

    // Assert
    await action.Should().ThrowAsync<ArgumentException>();
  }

  /// <summary>U35: Verifies GetWidgetAsync throws ArgumentNullException for null sitekey.</summary>
  [Fact]
  public async Task GetWidgetAsync_NullSitekey_ThrowsArgumentNullException()
  {
    // Arrange
    var responseJson = HttpFixtures.CreateSuccessResponse(new { });
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.GetWidgetAsync(TestAccountId, null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentNullException>();
  }

  /// <summary>U36: Verifies CreateWidgetAsync throws ArgumentNullException for null request.</summary>
  [Fact]
  public async Task CreateWidgetAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var responseJson = HttpFixtures.CreateSuccessResponse(new { });
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.CreateWidgetAsync(TestAccountId, null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentNullException>();
  }

  /// <summary>U37: Verifies UpdateWidgetAsync throws ArgumentNullException for null sitekey.</summary>
  [Fact]
  public async Task UpdateWidgetAsync_NullSitekey_ThrowsArgumentNullException()
  {
    // Arrange
    var responseJson = HttpFixtures.CreateSuccessResponse(new { });
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);
    var request = new UpdateTurnstileWidgetRequest("Test", ["example.com"], WidgetMode.Managed);

    // Act
    var action = async () => await sut.UpdateWidgetAsync(TestAccountId, null!, request);

    // Assert
    await action.Should().ThrowAsync<ArgumentNullException>();
  }

  /// <summary>U38: Verifies UpdateWidgetAsync throws ArgumentNullException for null request.</summary>
  [Fact]
  public async Task UpdateWidgetAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var responseJson = HttpFixtures.CreateSuccessResponse(new { });
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.UpdateWidgetAsync(TestAccountId, TestSitekey, null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentNullException>();
  }

  /// <summary>U39: Verifies DeleteWidgetAsync throws ArgumentNullException for null sitekey.</summary>
  [Fact]
  public async Task DeleteWidgetAsync_NullSitekey_ThrowsArgumentNullException()
  {
    // Arrange
    var responseJson = HttpFixtures.CreateSuccessResponse(new { });
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.DeleteWidgetAsync(TestAccountId, null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentNullException>();
  }

  /// <summary>U40: Verifies RotateSecretAsync throws ArgumentNullException for null sitekey.</summary>
  [Fact]
  public async Task RotateSecretAsync_NullSitekey_ThrowsArgumentNullException()
  {
    // Arrange
    var responseJson = HttpFixtures.CreateSuccessResponse(new { });
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(responseJson, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new TurnstileApi(httpClient, _loggerFactory);

    // Act
    var action = async () => await sut.RotateSecretAsync(TestAccountId, null!);

    // Assert
    await action.Should().ThrowAsync<ArgumentNullException>();
  }

  #endregion


  #region Helpers

  /// <summary>Creates a test TurnstileWidget for use in unit tests.</summary>
  private static TurnstileWidget CreateTestWidget(
    string? sitekey = null,
    string? name = null,
    WidgetMode? mode = null,
    bool botFightMode = false,
    ClearanceLevel? clearanceLevel = null,
    bool ephemeralId = false,
    bool offlabel = false,
    string? region = null,
    string? secret = null)
  {
    return new TurnstileWidget(
      Sitekey: sitekey ?? TestSitekey,
      Name: name ?? "Test Widget",
      Mode: mode ?? WidgetMode.Managed,
      Domains: ["example.com", "www.example.com"],
      CreatedOn: new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc),
      ModifiedOn: new DateTime(2024, 1, 20, 14, 30, 0, DateTimeKind.Utc),
      BotFightMode: botFightMode,
      ClearanceLevel: clearanceLevel,
      EphemeralId: ephemeralId,
      Offlabel: offlabel,
      Region: region,
      Secret: secret);
  }

  #endregion
}
