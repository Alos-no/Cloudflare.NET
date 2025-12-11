namespace Cloudflare.NET.Tests.Shared.Mocks;

using System.Net;
using System.Text;
using Moq;
using Moq.Protected;

/// <summary>
///   Factory for creating mock API clients with pre-configured HTTP handlers.
///   Simplifies test setup by hiding the HttpClient configuration boilerplate.
/// </summary>
public static class MockApiClientFactory
{
  #region Methods

  /// <summary>
  ///   Creates an HttpClient configured with a mock handler that returns the specified response.
  /// </summary>
  /// <param name="responseContent">JSON response content.</param>
  /// <param name="statusCode">HTTP status code.</param>
  /// <param name="requestCallback">Optional callback to inspect the request.</param>
  /// <returns>Configured HttpClient.</returns>
  public static HttpClient CreateMockHttpClient(
    string responseContent,
    HttpStatusCode statusCode = HttpStatusCode.OK,
    Action<HttpRequestMessage>? requestCallback = null)
  {
    var mockHandler = CreateMockHandler(responseContent, statusCode, requestCallback);

    return new HttpClient(mockHandler.Object)
    {
      BaseAddress = new Uri(TestConstants.CloudflareApiBaseUrl)
    };
  }

  /// <summary>
  ///   Creates a mock HttpMessageHandler that returns the specified response.
  /// </summary>
  /// <param name="responseContent">JSON response content.</param>
  /// <param name="statusCode">HTTP status code.</param>
  /// <param name="requestCallback">Optional callback to inspect the request.</param>
  /// <returns>Mock handler.</returns>
  public static Mock<HttpMessageHandler> CreateMockHandler(
    string responseContent,
    HttpStatusCode statusCode = HttpStatusCode.OK,
    Action<HttpRequestMessage>? requestCallback = null)
  {
    var mockHandler = new Mock<HttpMessageHandler>();
    var response = new HttpResponseMessage(statusCode)
    {
      Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
    };

    var setup = mockHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>());

    if (requestCallback != null)
    {
      setup.Callback<HttpRequestMessage, CancellationToken>((req, _) => requestCallback(req));
    }

    setup.ReturnsAsync(response);

    return mockHandler;
  }

  /// <summary>
  ///   Creates a mock handler that returns different responses based on request sequence.
  /// </summary>
  /// <param name="responses">Responses in order (each call returns the next response).</param>
  /// <returns>Mock handler configured for sequential responses.</returns>
  public static Mock<HttpMessageHandler> CreateSequentialMockHandler(
    params (string Content, HttpStatusCode StatusCode)[] responses)
  {
    var mockHandler = new Mock<HttpMessageHandler>();
    var callIndex = 0;

    mockHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync(() =>
      {
        var (content, status) = responses[Math.Min(callIndex++, responses.Length - 1)];

        return new HttpResponseMessage(status)
        {
          Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
      });

    return mockHandler;
  }

  /// <summary>
  ///   Creates a mock handler that captures all requests for later verification.
  /// </summary>
  /// <param name="responseContent">JSON response content for all requests.</param>
  /// <param name="statusCode">HTTP status code.</param>
  /// <returns>Tuple of (Mock handler, List of captured requests).</returns>
  public static (Mock<HttpMessageHandler> Handler, List<HttpRequestMessage> Requests) CreateCapturingMockHandler(
    string responseContent,
    HttpStatusCode statusCode = HttpStatusCode.OK)
  {
    var requests = new List<HttpRequestMessage>();
    var handler = CreateMockHandler(responseContent, statusCode, req => requests.Add(req));

    return (handler, requests);
  }

  /// <summary>
  ///   Creates a mock handler that returns different responses based on request URL or method.
  /// </summary>
  /// <param name="responseSelector">Function to select response based on request.</param>
  /// <returns>Mock handler configured with custom response selection.</returns>
  public static Mock<HttpMessageHandler> CreateConditionalMockHandler(
    Func<HttpRequestMessage, (string Content, HttpStatusCode StatusCode)> responseSelector)
  {
    var mockHandler = new Mock<HttpMessageHandler>();

    mockHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
      {
        var (content, status) = responseSelector(request);

        return new HttpResponseMessage(status)
        {
          Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
      });

    return mockHandler;
  }

  /// <summary>
  ///   Creates a mock handler that captures requests and returns sequential responses.
  /// </summary>
  /// <param name="responses">Responses in order.</param>
  /// <returns>Tuple of (Mock handler, List of captured requests).</returns>
  public static (Mock<HttpMessageHandler> Handler, List<HttpRequestMessage> Requests) CreateCapturingSequentialMockHandler(
    params (string Content, HttpStatusCode StatusCode)[] responses)
  {
    var mockHandler = new Mock<HttpMessageHandler>();
    var requests = new List<HttpRequestMessage>();
    var callIndex = 0;

    mockHandler
      .Protected()
      .Setup<Task<HttpResponseMessage>>(
        "SendAsync",
        ItExpr.IsAny<HttpRequestMessage>(),
        ItExpr.IsAny<CancellationToken>())
      .Callback<HttpRequestMessage, CancellationToken>((req, _) => requests.Add(req))
      .ReturnsAsync(() =>
      {
        var (content, status) = responses[Math.Min(callIndex++, responses.Length - 1)];

        return new HttpResponseMessage(status)
        {
          Content = new StringContent(content, Encoding.UTF8, "application/json")
        };
      });

    return (mockHandler, requests);
  }

  #endregion
}
