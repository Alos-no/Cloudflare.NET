namespace Cloudflare.NET.Tests.Shared.Fixtures;

using System.Net;
using System.Text.Json;
using Moq.Protected;

/// <summary>
///   Provides helper methods and fixtures for creating mocked HTTP responses and handlers
///   for unit tests.
/// </summary>
public static class HttpFixtures
{
  #region Methods

  /// <summary>Creates a successful Cloudflare API JSON response string.</summary>
  /// <param name="result">The result object to embed in the response.</param>
  /// <returns>A JSON string representing a successful API response.</returns>
  public static string CreateSuccessResponse<T>(T result)
  {
    var response = new
    {
      success  = true,
      errors   = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result
    };
    return JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
  }

  /// <summary>Creates a failed Cloudflare API JSON response string.</summary>
  /// <param name="code">The error code.</param>
  /// <param name="message">The error message.</param>
  /// <returns>A JSON string representing a failed API response.</returns>
  public static string CreateErrorResponse(int code = 10000, string message = "Authentication error")
  {
    var response = new
    {
      success = false,
      errors = new[]
      {
        new { code, message }
      },
      messages = Array.Empty<object>(),
      result   = (object?)null
    };
    return JsonSerializer.Serialize(response);
  }

  /// <summary>
  ///   Creates a mock <see cref="HttpMessageHandler" /> that is set up to return a specific
  ///   response.
  /// </summary>
  /// <param name="responseContent">The string content to return in the response.</param>
  /// <param name="statusCode">The HTTP status code to return.</param>
  /// <param name="callback">An optional callback to inspect the request message.</param>
  /// <returns>A configured mock of <see cref="HttpMessageHandler" />.</returns>
  public static Mock<HttpMessageHandler> GetMockHttpMessageHandler(
    string                                         responseContent,
    HttpStatusCode                                 statusCode,
    Action<HttpRequestMessage, CancellationToken>? callback)
  {
    var mockHandler = new Mock<HttpMessageHandler>();
    var response = new HttpResponseMessage
    {
      StatusCode = statusCode,
      Content    = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
    };

    var setup = mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
                );

    if (callback is not null)
      setup.Callback(callback);

    setup.ReturnsAsync(response);

    return mockHandler;
  }


  /// <summary>
  ///   Creates a mock <see cref="HttpMessageHandler" /> that is set up to return a specific
  ///   response.
  /// </summary>
  /// <param name="responseContent">The string content to return in the response.</param>
  /// <param name="statusCode">The HTTP status code to return.</param>
  /// <returns>A configured mock of <see cref="HttpMessageHandler" />.</returns>
  public static Mock<HttpMessageHandler> GetMockHttpMessageHandler(string responseContent, HttpStatusCode statusCode) =>
    GetMockHttpMessageHandler(responseContent, statusCode, null);

  #endregion
}
