namespace Cloudflare.NET.Tests.UnitTests;

using System.Net;
using System.Text.Json;
using Core.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Shared.Fixtures;
using Workers;
using Workers.Models;
using Xunit.Abstractions;

/// <summary>
///   Contains unit tests for the <see cref="WorkersApi" /> class.
///   These tests verify HTTP request construction, response deserialization,
///   error handling, and parameter validation.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class WorkersApiUnitTests
{
  #region Properties & Fields - Non-Public

  /// <summary>Logger factory for test API instances.</summary>
  private readonly ILoggerFactory _loggerFactory;

  /// <summary>The test zone ID used in tests.</summary>
  private const string TestZoneId = "test-zone-id";

  /// <summary>The test route ID used in tests.</summary>
  private const string TestRouteId = "test-route-id";

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="WorkersApiUnitTests" /> class.</summary>
  /// <param name="output">The test output helper.</param>
  public WorkersApiUnitTests(ITestOutputHelper output)
  {
    var loggerProvider = new XunitTestOutputLoggerProvider { Current = output };
    _loggerFactory = new LoggerFactory([loggerProvider]);
  }

  #endregion


  #region Request Construction Tests (U01-U06)

  /// <summary>U01: Verifies that ListRoutesAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task ListRoutesAsync_SendsCorrectRequest()
  {
    // Arrange
    var successResponse = CreateListResponse(Array.Empty<WorkerRoute>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act
    await sut.ListRoutesAsync(TestZoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/zones/{TestZoneId}/workers/routes");
  }

  /// <summary>U02: Verifies that GetRouteAsync sends a GET request to the correct endpoint.</summary>
  [Fact]
  public async Task GetRouteAsync_SendsCorrectRequest()
  {
    // Arrange
    var route = CreateTestRoute();
    var successResponse = HttpFixtures.CreateSuccessResponse(route);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act
    await sut.GetRouteAsync(TestZoneId, TestRouteId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Get);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/zones/{TestZoneId}/workers/routes/{TestRouteId}");
  }

  /// <summary>U03: Verifies that CreateRouteAsync sends a POST request with script.</summary>
  [Fact]
  public async Task CreateRouteAsync_WithScript_SendsCorrectRequest()
  {
    // Arrange
    var request = new CreateWorkerRouteRequest("example.com/*", "my-worker");
    var route = CreateTestRoute();
    var successResponse = HttpFixtures.CreateSuccessResponse(route);
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateRouteAsync(TestZoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Post);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/zones/{TestZoneId}/workers/routes");
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("\"pattern\"");
    capturedBody.Should().Contain("example.com/*");
    capturedBody.Should().Contain("\"script\"");
    capturedBody.Should().Contain("my-worker");
  }

  /// <summary>U04: Verifies that CreateRouteAsync sends a POST request without script field when null.</summary>
  [Fact]
  public async Task CreateRouteAsync_WithoutScript_OmitsScriptField()
  {
    // Arrange
    var request = new CreateWorkerRouteRequest("example.com/*");
    var route = CreateTestRoute("route-id", "example.com/*", null);
    var successResponse = HttpFixtures.CreateSuccessResponse(route);
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act
    await sut.CreateRouteAsync(TestZoneId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("\"pattern\"");
    capturedBody.Should().NotContain("\"script\""); // Script field should be omitted when null
  }

  /// <summary>U05: Verifies that UpdateRouteAsync sends a PUT request with correct body.</summary>
  [Fact]
  public async Task UpdateRouteAsync_SendsCorrectRequest()
  {
    // Arrange
    var request = new UpdateWorkerRouteRequest("api.example.com/*", "api-worker");
    var route = CreateTestRoute();
    var successResponse = HttpFixtures.CreateSuccessResponse(route);
    HttpRequestMessage? capturedRequest = null;
    string? capturedBody = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) =>
    {
      capturedRequest = req;
      capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
    });

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act
    await sut.UpdateRouteAsync(TestZoneId, TestRouteId, request);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Put);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/zones/{TestZoneId}/workers/routes/{TestRouteId}");
    capturedBody.Should().NotBeNull();
    capturedBody.Should().Contain("\"pattern\"");
    capturedBody.Should().Contain("api.example.com/*");
    capturedBody.Should().Contain("\"script\"");
    capturedBody.Should().Contain("api-worker");
  }

  /// <summary>U06: Verifies that DeleteRouteAsync sends a DELETE request to the correct endpoint.</summary>
  [Fact]
  public async Task DeleteRouteAsync_SendsCorrectRequest()
  {
    // Arrange
    var successResponse = HttpFixtures.CreateSuccessResponse<object?>(null);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act
    await sut.DeleteRouteAsync(TestZoneId, TestRouteId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.Method.Should().Be(HttpMethod.Delete);
    capturedRequest.RequestUri!.AbsolutePath.Should().Be($"/client/v4/zones/{TestZoneId}/workers/routes/{TestRouteId}");
  }

  #endregion


  #region Response Deserialization Tests (U07-U10)

  /// <summary>U07: Verifies WorkerRoute with script is deserialized correctly.</summary>
  [Fact]
  public async Task GetRouteAsync_WithScript_DeserializesCorrectly()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""route-123"",
        ""pattern"": ""api.example.com/*"",
        ""script"": ""api-handler""
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetRouteAsync(TestZoneId, "route-123");

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be("route-123");
    result.Pattern.Should().Be("api.example.com/*");
    result.Script.Should().Be("api-handler");
  }

  /// <summary>U08: Verifies WorkerRoute without script is deserialized correctly.</summary>
  [Fact]
  public async Task GetRouteAsync_WithoutScript_DeserializesCorrectly()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": {
        ""id"": ""route-456"",
        ""pattern"": ""*.example.com/*"",
        ""script"": null
      }
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.GetRouteAsync(TestZoneId, "route-456");

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be("route-456");
    result.Pattern.Should().Be("*.example.com/*");
    result.Script.Should().BeNull();
  }

  /// <summary>U09: Verifies route list is deserialized correctly.</summary>
  [Fact]
  public async Task ListRoutesAsync_DeserializesListCorrectly()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": [
        { ""id"": ""route-1"", ""pattern"": ""api.example.com/*"", ""script"": ""api-handler"" },
        { ""id"": ""route-2"", ""pattern"": ""static.example.com/*"", ""script"": ""static-handler"" },
        { ""id"": ""route-3"", ""pattern"": ""disabled.example.com/*"", ""script"": null }
      ]
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListRoutesAsync(TestZoneId);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(3);
    result[0].Id.Should().Be("route-1");
    result[0].Pattern.Should().Be("api.example.com/*");
    result[0].Script.Should().Be("api-handler");
    result[1].Id.Should().Be("route-2");
    result[2].Script.Should().BeNull();
  }

  /// <summary>U10: Verifies empty route list is handled correctly.</summary>
  [Fact]
  public async Task ListRoutesAsync_EmptyList_ReturnsEmptyList()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": true,
      ""errors"": [],
      ""messages"": [],
      ""result"": []
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act
    var result = await sut.ListRoutesAsync(TestZoneId);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
  }

  #endregion


  #region Error Handling Tests (U11-U14)

  /// <summary>U11: Verifies that API error envelope throws CloudflareApiException.</summary>
  [Fact]
  public async Task CreateRouteAsync_WhenApiError_ThrowsCloudflareApiException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10016, ""message"": ""workers.api.error.route_exists"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act & Assert
    var request = new CreateWorkerRouteRequest("example.com/*", "my-worker");
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.CreateRouteAsync(TestZoneId, request));
    exception.Errors.Should().HaveCount(1);
    exception.Errors[0].Code.Should().Be(10016);
  }

  /// <summary>U12: Verifies that invalid pattern throws CloudflareApiException.</summary>
  [Fact]
  public async Task CreateRouteAsync_InvalidPattern_ThrowsCloudflareApiException()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [{ ""code"": 10015, ""message"": ""workers.api.error.invalid_route_pattern"" }],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act & Assert
    var request = new CreateWorkerRouteRequest("invalid-pattern");
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.CreateRouteAsync(TestZoneId, request));
    exception.Errors.Should().HaveCount(1);
    exception.Errors[0].Code.Should().Be(10015);
  }

  /// <summary>U13: Verifies multiple errors in response are captured.</summary>
  [Fact]
  public async Task CreateRouteAsync_WhenMultipleErrors_CapturesAllErrors()
  {
    // Arrange
    var jsonResponse = @"{
      ""success"": false,
      ""errors"": [
        { ""code"": 10015, ""message"": ""Invalid pattern"" },
        { ""code"": 10016, ""message"": ""Route exists"" }
      ],
      ""messages"": [],
      ""result"": null
    }";
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(jsonResponse, HttpStatusCode.OK);
    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act & Assert
    var request = new CreateWorkerRouteRequest("example.com/*");
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(() => sut.CreateRouteAsync(TestZoneId, request));
    exception.Errors.Should().HaveCount(2);
    exception.Errors.Select(e => e.Code).Should().Contain(new[] { 10015, 10016 });
  }

  #endregion


  #region URL Encoding Tests (U15-U16)

  /// <summary>U15: Verifies that ListRoutesAsync properly URL-encodes the zone ID.</summary>
  [Fact]
  public async Task ListRoutesAsync_WithSpecialChars_UrlEncodesZoneId()
  {
    // Arrange
    var zoneId = "zone+with/special";
    var successResponse = CreateListResponse(Array.Empty<WorkerRoute>());
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act
    await sut.ListRoutesAsync(zoneId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("zone%2Bwith%2Fspecial");
  }

  /// <summary>U16: Verifies that GetRouteAsync properly URL-encodes the route ID.</summary>
  [Fact]
  public async Task GetRouteAsync_WithSpecialChars_UrlEncodesRouteId()
  {
    // Arrange
    var routeId = "route+with/special";
    var route = CreateTestRoute(routeId);
    var successResponse = HttpFixtures.CreateSuccessResponse(route);
    HttpRequestMessage? capturedRequest = null;
    var mockHandler = HttpFixtures.GetMockHttpMessageHandler(successResponse, HttpStatusCode.OK, (req, _) => capturedRequest = req);

    var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act
    await sut.GetRouteAsync(TestZoneId, routeId);

    // Assert
    capturedRequest.Should().NotBeNull();
    capturedRequest!.RequestUri!.AbsolutePath.Should().Contain("route%2Bwith%2Fspecial");
  }

  #endregion


  #region Parameter Validation Tests (U23-U30)

  /// <summary>U23: Verifies that ListRoutesAsync throws on null zoneId.</summary>
  [Fact]
  public async Task ListRoutesAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.ListRoutesAsync(null!));
  }

  /// <summary>U24: Verifies that ListRoutesAsync throws on whitespace zoneId.</summary>
  [Fact]
  public async Task ListRoutesAsync_WhitespaceZoneId_ThrowsArgumentException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(() => sut.ListRoutesAsync("   "));
  }

  /// <summary>U25: Verifies that GetRouteAsync throws on null routeId.</summary>
  [Fact]
  public async Task GetRouteAsync_NullRouteId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.GetRouteAsync(TestZoneId, null!));
  }

  /// <summary>U26: Verifies that CreateRouteAsync throws on null request.</summary>
  [Fact]
  public async Task CreateRouteAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.CreateRouteAsync(TestZoneId, null!));
  }

  /// <summary>U27: Verifies that UpdateRouteAsync throws on null routeId.</summary>
  [Fact]
  public async Task UpdateRouteAsync_NullRouteId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);
    var request = new UpdateWorkerRouteRequest("example.com/*");

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateRouteAsync(TestZoneId, null!, request));
  }

  /// <summary>U28: Verifies that UpdateRouteAsync throws on null request.</summary>
  [Fact]
  public async Task UpdateRouteAsync_NullRequest_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.UpdateRouteAsync(TestZoneId, TestRouteId, null!));
  }

  /// <summary>U29: Verifies that DeleteRouteAsync throws on null zoneId.</summary>
  [Fact]
  public async Task DeleteRouteAsync_NullZoneId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.DeleteRouteAsync(null!, TestRouteId));
  }

  /// <summary>U30: Verifies that DeleteRouteAsync throws on null routeId.</summary>
  [Fact]
  public async Task DeleteRouteAsync_NullRouteId_ThrowsArgumentNullException()
  {
    // Arrange
    var httpClient = new HttpClient { BaseAddress = new Uri("https://api.cloudflare.com/client/v4/") };
    var sut = new WorkersApi(httpClient, _loggerFactory);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentNullException>(() => sut.DeleteRouteAsync(TestZoneId, null!));
  }

  #endregion


  #region Test Helpers

  /// <summary>Creates a test WorkerRoute for use in tests.</summary>
  private static WorkerRoute CreateTestRoute(string? id = null, string? pattern = null, string? script = "my-worker") =>
    new(id ?? TestRouteId, pattern ?? "example.com/*", script);

  /// <summary>Creates a list response JSON for routes.</summary>
  private static string CreateListResponse(IEnumerable<WorkerRoute> routes)
  {
    return JsonSerializer.Serialize(
      new
      {
        success = true,
        errors = Array.Empty<object>(),
        messages = Array.Empty<object>(),
        result = routes.ToList()
      },
      new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
  }

  #endregion
}
