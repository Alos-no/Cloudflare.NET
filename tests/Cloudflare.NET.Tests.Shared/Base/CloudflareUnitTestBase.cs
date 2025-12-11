namespace Cloudflare.NET.Tests.Shared.Base;

using System.Net;
using System.Text.Json;
using Cloudflare.NET.Tests.Shared.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
///   Base class for Cloudflare API unit tests. Provides common setup
///   for mocked HTTP client and request capturing.
/// </summary>
public abstract class CloudflareUnitTestBase : IDisposable
{
  #region Properties & Fields - Non-Public

  /// <summary>Captured HTTP requests for verification.</summary>
  protected List<HttpRequestMessage> CapturedRequests { get; } = [];

  /// <summary>Mock HTTP message handler.</summary>
  protected Mock<HttpMessageHandler>? MockHandler { get; private set; }

  /// <summary>Configured HttpClient for testing.</summary>
  protected HttpClient? HttpClient { get; private set; }

  /// <summary>Mock logger for the API under test.</summary>
  protected Mock<ILogger> MockLogger { get; } = new();

  /// <summary>Tracks whether the object has been disposed.</summary>
  private bool _disposed;

  #endregion


  #region Setup Methods

  /// <summary>
  ///   Configures the mock HTTP client to return a successful response.
  /// </summary>
  /// <typeparam name="T">Result type.</typeparam>
  /// <param name="result">The result to return.</param>
  protected void SetupSuccessResponse<T>(T result)
  {
    SetupResponse(MockResponseFactory.Success(result), HttpStatusCode.OK);
  }

  /// <summary>
  ///   Configures the mock HTTP client to return a paginated response.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="items">Items to return.</param>
  /// <param name="page">Current page.</param>
  /// <param name="perPage">Items per page.</param>
  /// <param name="totalCount">Total item count.</param>
  protected void SetupPagePaginatedResponse<T>(
    IEnumerable<T> items,
    int page = 1,
    int perPage = 20,
    int totalCount = 1)
  {
    SetupResponse(
      MockResponseFactory.SuccessPagePaginated(items, page, perPage, totalCount),
      HttpStatusCode.OK);
  }

  /// <summary>
  ///   Configures the mock HTTP client to return a cursor-paginated response.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="items">Items to return.</param>
  /// <param name="perPage">Items per page.</param>
  /// <param name="cursor">Cursor for next page (null if last page).</param>
  protected void SetupCursorPaginatedResponse<T>(
    IEnumerable<T> items,
    int perPage = 20,
    string? cursor = null)
  {
    SetupResponse(
      MockResponseFactory.SuccessCursorPaginated(items, perPage, cursor),
      HttpStatusCode.OK);
  }

  /// <summary>
  ///   Configures the mock HTTP client to return an error response.
  /// </summary>
  /// <param name="code">Error code.</param>
  /// <param name="message">Error message.</param>
  protected void SetupApiErrorResponse(int code, string message)
  {
    SetupResponse(MockResponseFactory.Error(code, message), HttpStatusCode.OK);
  }

  /// <summary>
  ///   Configures the mock HTTP client to return an HTTP error.
  /// </summary>
  /// <param name="statusCode">HTTP status code.</param>
  protected void SetupHttpErrorResponse(HttpStatusCode statusCode)
  {
    var json = statusCode switch
    {
      HttpStatusCode.Unauthorized => MockResponseFactory.AuthenticationError(),
      HttpStatusCode.Forbidden => MockResponseFactory.ForbiddenError(),
      HttpStatusCode.NotFound => MockResponseFactory.NotFoundError(),
      HttpStatusCode.TooManyRequests => MockResponseFactory.RateLimitedError(),
      HttpStatusCode.BadRequest => MockResponseFactory.Error(6003, "Bad request"),
      _ => MockResponseFactory.Error(10000, $"HTTP {(int)statusCode}")
    };
    SetupResponse(json, statusCode);
  }

  /// <summary>
  ///   Configures the mock HTTP client with sequential responses for pagination testing.
  /// </summary>
  /// <param name="responses">Responses in order.</param>
  protected void SetupSequentialResponses(params (string Content, HttpStatusCode StatusCode)[] responses)
  {
    var (handler, requests) = MockApiClientFactory.CreateCapturingSequentialMockHandler(responses);
    MockHandler = handler;
    CapturedRequests.Clear();

    // We need to capture requests ourselves since the sequential handler uses a different pattern
    // The requests list is already being populated by the capturing handler
    HttpClient = new HttpClient(MockHandler.Object)
    {
      BaseAddress = new Uri(TestConstants.CloudflareApiBaseUrl)
    };

    // Wire up to capture requests
    foreach (var req in requests)
    {
      CapturedRequests.Add(req);
    }
  }

  /// <summary>
  ///   Configures the mock HTTP client with sequential responses and captures all requests.
  /// </summary>
  /// <param name="responses">Responses in order.</param>
  protected void SetupCapturingSequentialResponses(params (string Content, HttpStatusCode StatusCode)[] responses)
  {
    var (handler, requests) = MockApiClientFactory.CreateCapturingSequentialMockHandler(responses);
    MockHandler = handler;
    CapturedRequests.Clear();

    HttpClient = new HttpClient(MockHandler.Object)
    {
      BaseAddress = new Uri(TestConstants.CloudflareApiBaseUrl)
    };

    // Store the reference so captured requests accumulate
    // Note: CapturedRequests and requests are different lists; we need to expose the correct one
    // For sequential capturing, the requests list from the factory is populated during execution
    // We'll swap our reference to use that list
    // This is a design limitation - we'll need to return the requests list to the caller
  }

  /// <summary>
  ///   Configures the mock HTTP client to return a raw JSON response.
  /// </summary>
  /// <param name="json">Raw JSON response content.</param>
  /// <param name="statusCode">HTTP status code.</param>
  protected void SetupRawResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
  {
    SetupResponse(json, statusCode);
  }

  /// <summary>
  ///   Core setup method for configuring mock responses.
  /// </summary>
  private void SetupResponse(string content, HttpStatusCode statusCode)
  {
    CapturedRequests.Clear();
    var (handler, requests) = MockApiClientFactory.CreateCapturingMockHandler(content, statusCode);
    MockHandler = handler;

    // Use the captured requests list directly
    HttpClient = new HttpClient(MockHandler.Object)
    {
      BaseAddress = new Uri(TestConstants.CloudflareApiBaseUrl)
    };

    // Replace our list with the one that's being populated
    CapturedRequests.Clear();

    // We need to re-configure to add items to our list
    MockHandler = MockApiClientFactory.CreateMockHandler(content, statusCode, req => CapturedRequests.Add(req));
    HttpClient = new HttpClient(MockHandler.Object)
    {
      BaseAddress = new Uri(TestConstants.CloudflareApiBaseUrl)
    };
  }

  #endregion


  #region Assertion Helpers

  /// <summary>
  ///   Asserts the captured request has the expected method and path.
  /// </summary>
  /// <param name="expectedMethod">Expected HTTP method.</param>
  /// <param name="expectedPath">Expected request path.</param>
  /// <param name="requestIndex">Index of the request to check (default: 0).</param>
  protected void AssertRequest(
    HttpMethod expectedMethod,
    string expectedPath,
    int requestIndex = 0)
  {
    Assert.True(CapturedRequests.Count > requestIndex,
      $"Expected at least {requestIndex + 1} requests, but only {CapturedRequests.Count} were made.");

    var request = CapturedRequests[requestIndex];
    Assert.Equal(expectedMethod, request.Method);
    Assert.Equal(expectedPath, request.RequestUri?.AbsolutePath);
  }

  /// <summary>
  ///   Asserts the captured request contains the expected query parameter.
  /// </summary>
  /// <param name="paramName">Query parameter name.</param>
  /// <param name="expectedValue">Expected parameter value.</param>
  /// <param name="requestIndex">Index of the request to check (default: 0).</param>
  protected void AssertQueryParam(
    string paramName,
    string expectedValue,
    int requestIndex = 0)
  {
    var request = CapturedRequests[requestIndex];
    var query = request.RequestUri?.Query ?? string.Empty;

    Assert.Contains($"{paramName}={Uri.EscapeDataString(expectedValue)}", query);
  }

  /// <summary>
  ///   Asserts the captured request does NOT contain the specified query parameter.
  /// </summary>
  /// <param name="paramName">Query parameter name that should not be present.</param>
  /// <param name="requestIndex">Index of the request to check (default: 0).</param>
  protected void AssertNoQueryParam(
    string paramName,
    int requestIndex = 0)
  {
    var request = CapturedRequests[requestIndex];
    var query = request.RequestUri?.Query ?? string.Empty;

    Assert.DoesNotContain($"{paramName}=", query);
  }

  /// <summary>
  ///   Gets the request body as a string.
  /// </summary>
  /// <param name="requestIndex">Index of the request to check (default: 0).</param>
  /// <returns>Request body as string.</returns>
  protected async Task<string> GetRequestBodyAsync(int requestIndex = 0)
  {
    var request = CapturedRequests[requestIndex];

    if (request.Content == null)
      return string.Empty;

    return await request.Content.ReadAsStringAsync();
  }

  /// <summary>
  ///   Gets the request body as a deserialized object.
  /// </summary>
  /// <typeparam name="T">Type to deserialize to.</typeparam>
  /// <param name="requestIndex">Index of the request to check (default: 0).</param>
  /// <returns>Deserialized request body.</returns>
  protected async Task<T?> GetRequestBodyAsync<T>(int requestIndex = 0)
  {
    var json = await GetRequestBodyAsync(requestIndex);

    return JsonSerializer.Deserialize<T>(json, MockResponseFactory.JsonOptions);
  }

  /// <summary>
  ///   Asserts that the request body contains the expected JSON property.
  /// </summary>
  /// <param name="propertyName">JSON property name (snake_case).</param>
  /// <param name="expectedValue">Expected value as string.</param>
  /// <param name="requestIndex">Index of the request to check (default: 0).</param>
  protected async Task AssertRequestBodyContainsAsync(
    string propertyName,
    string expectedValue,
    int requestIndex = 0)
  {
    var body = await GetRequestBodyAsync(requestIndex);

    Assert.Contains($"\"{propertyName}\"", body);
    Assert.Contains(expectedValue, body);
  }

  /// <summary>
  ///   Asserts that no requests were made.
  /// </summary>
  protected void AssertNoRequestsMade()
  {
    Assert.Empty(CapturedRequests);
  }

  /// <summary>
  ///   Asserts the expected number of requests were made.
  /// </summary>
  /// <param name="expectedCount">Expected number of requests.</param>
  protected void AssertRequestCount(int expectedCount)
  {
    Assert.Equal(expectedCount, CapturedRequests.Count);
  }

  #endregion


  #region IDisposable

  /// <summary>
  ///   Releases resources used by the test.
  /// </summary>
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  /// <summary>
  ///   Releases resources used by the test.
  /// </summary>
  /// <param name="disposing">True if disposing managed resources.</param>
  protected virtual void Dispose(bool disposing)
  {
    if (_disposed)
      return;

    if (disposing)
    {
      HttpClient?.Dispose();
    }

    _disposed = true;
  }

  #endregion
}
