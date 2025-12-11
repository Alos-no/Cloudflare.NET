namespace Cloudflare.NET.Tests.Shared.Mocks;

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///   Factory for creating mock HTTP responses that match Cloudflare API patterns.
///   Use this instead of manually constructing JSON responses in each test.
/// </summary>
public static class MockResponseFactory
{
  #region Constants & Statics

  /// <summary>Standard JSON serializer options matching Cloudflare API conventions.</summary>
  public static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  #endregion


  #region Success Responses

  /// <summary>Creates a successful API response with a single result.</summary>
  /// <typeparam name="T">The result type.</typeparam>
  /// <param name="result">The result object.</param>
  /// <returns>JSON string representing the response.</returns>
  public static string Success<T>(T result) =>
    JsonSerializer.Serialize(new
    {
      success = true,
      errors = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result
    }, JsonOptions);

  /// <summary>Creates a successful API response with a list result and page pagination info.</summary>
  /// <typeparam name="T">The item type.</typeparam>
  /// <param name="items">The result items.</param>
  /// <param name="page">Current page number (1-based).</param>
  /// <param name="perPage">Items per page.</param>
  /// <param name="totalCount">Total items across all pages.</param>
  /// <returns>JSON string representing the paginated response.</returns>
  public static string SuccessPagePaginated<T>(
    IEnumerable<T> items,
    int page = 1,
    int perPage = 20,
    int totalCount = 1) =>
    JsonSerializer.Serialize(new
    {
      success = true,
      errors = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result = items,
      result_info = new
      {
        page,
        per_page = perPage,
        count = items.Count(),
        total_count = totalCount,
        total_pages = (int)Math.Ceiling((double)totalCount / perPage)
      }
    }, JsonOptions);

  /// <summary>Creates a successful API response with cursor-based pagination info.</summary>
  /// <typeparam name="T">The item type.</typeparam>
  /// <param name="items">The result items.</param>
  /// <param name="perPage">Items per page.</param>
  /// <param name="cursor">Cursor for next page (null if last page).</param>
  /// <returns>JSON string representing the cursor-paginated response.</returns>
  public static string SuccessCursorPaginated<T>(
    IEnumerable<T> items,
    int perPage = 20,
    string? cursor = null) =>
    JsonSerializer.Serialize(new
    {
      success = true,
      errors = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result = items,
      result_info = new
      {
        count = items.Count(),
        per_page = perPage,
        cursor
      }
    }, JsonOptions);

  /// <summary>Creates an empty successful response (for delete operations).</summary>
  /// <param name="deletedId">Optional ID of the deleted resource.</param>
  /// <returns>JSON string representing an empty success response.</returns>
  public static string SuccessEmpty(string? deletedId = "deleted-id") =>
    JsonSerializer.Serialize(new
    {
      success = true,
      errors = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result = new { id = deletedId }  // Cloudflare often returns { id } on delete
    }, JsonOptions);

  /// <summary>Creates a successful response with null result.</summary>
  /// <returns>JSON string representing a success response with null result.</returns>
  public static string SuccessNull() =>
    JsonSerializer.Serialize(new
    {
      success = true,
      errors = Array.Empty<object>(),
      messages = Array.Empty<object>(),
      result = (object?)null
    }, JsonOptions);

  #endregion


  #region Error Responses

  /// <summary>Creates a failed API response with a single error.</summary>
  /// <param name="code">Error code.</param>
  /// <param name="message">Error message.</param>
  /// <returns>JSON string representing the error response.</returns>
  public static string Error(int code, string message) =>
    JsonSerializer.Serialize(new
    {
      success = false,
      errors = new[] { new { code, message } },
      messages = Array.Empty<object>(),
      result = (object?)null
    }, JsonOptions);

  /// <summary>Creates a failed API response with multiple errors.</summary>
  /// <param name="errors">Collection of (code, message) tuples.</param>
  /// <returns>JSON string representing the multi-error response.</returns>
  public static string Errors(params (int Code, string Message)[] errors) =>
    JsonSerializer.Serialize(new
    {
      success = false,
      errors = errors.Select(e => new { code = e.Code, message = e.Message }),
      messages = Array.Empty<object>(),
      result = (object?)null
    }, JsonOptions);

  /// <summary>Creates a common "Authentication error" response.</summary>
  /// <returns>JSON string representing an authentication error.</returns>
  public static string AuthenticationError() =>
    Error(10000, "Authentication error");

  /// <summary>Creates a common "Invalid API Token" response.</summary>
  /// <returns>JSON string representing an invalid token error.</returns>
  public static string InvalidTokenError() =>
    Error(6003, "Invalid request headers");

  /// <summary>Creates a common "Forbidden" response.</summary>
  /// <returns>JSON string representing a forbidden error.</returns>
  public static string ForbiddenError() =>
    Error(10000, "This API Token does not have permission to access this resource");

  /// <summary>Creates a common "Not found" response.</summary>
  /// <param name="resourceType">Type of resource that was not found.</param>
  /// <returns>JSON string representing a not found error.</returns>
  public static string NotFoundError(string resourceType = "resource") =>
    Error(7003, $"Could not find {resourceType}");

  /// <summary>Creates a common "Rate limited" response.</summary>
  /// <returns>JSON string representing a rate limit error.</returns>
  public static string RateLimitedError() =>
    Error(10000, "Rate limit exceeded");

  /// <summary>Creates a validation error response.</summary>
  /// <param name="field">Field that failed validation.</param>
  /// <param name="message">Validation message.</param>
  /// <returns>JSON string representing a validation error.</returns>
  public static string ValidationError(string field, string message) =>
    Error(6003, $"{field}: {message}");

  #endregion


  #region HTTP Response Wrappers

  /// <summary>Creates an HttpResponseMessage with the given content and status.</summary>
  /// <param name="json">JSON content.</param>
  /// <param name="statusCode">HTTP status code.</param>
  /// <param name="headers">Optional headers to add.</param>
  /// <returns>Configured HttpResponseMessage.</returns>
  public static HttpResponseMessage CreateResponse(
    string json,
    HttpStatusCode statusCode,
    Dictionary<string, string>? headers = null)
  {
    var response = new HttpResponseMessage(statusCode)
    {
      Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    if (headers != null)
    {
      foreach (var (key, value) in headers)
      {
        response.Headers.TryAddWithoutValidation(key, value);
      }
    }

    return response;
  }

  /// <summary>Creates a 200 OK response with success content.</summary>
  /// <typeparam name="T">The result type.</typeparam>
  /// <param name="result">The result object.</param>
  /// <returns>Configured HttpResponseMessage with 200 OK.</returns>
  public static HttpResponseMessage CreateSuccessResponse<T>(T result) =>
    CreateResponse(Success(result), HttpStatusCode.OK);

  /// <summary>Creates a 429 Too Many Requests response with Retry-After header.</summary>
  /// <param name="retryAfterSeconds">Seconds until retry is allowed.</param>
  /// <returns>Configured HttpResponseMessage with 429 status.</returns>
  public static HttpResponseMessage Create429Response(int retryAfterSeconds = 60) =>
    CreateResponse(
      RateLimitedError(),
      HttpStatusCode.TooManyRequests,
      new Dictionary<string, string> { ["Retry-After"] = retryAfterSeconds.ToString() });

  /// <summary>Creates a 500 Internal Server Error response.</summary>
  /// <returns>Configured HttpResponseMessage with 500 status.</returns>
  public static HttpResponseMessage Create500Response() =>
    CreateResponse(
      Error(10000, "Internal server error"),
      HttpStatusCode.InternalServerError);

  /// <summary>Creates a 502 Bad Gateway response.</summary>
  /// <returns>Configured HttpResponseMessage with 502 status.</returns>
  public static HttpResponseMessage Create502Response() =>
    CreateResponse(
      Error(10000, "Bad gateway"),
      HttpStatusCode.BadGateway);

  /// <summary>Creates a 503 Service Unavailable response.</summary>
  /// <returns>Configured HttpResponseMessage with 503 status.</returns>
  public static HttpResponseMessage Create503Response() =>
    CreateResponse(
      Error(10000, "Service temporarily unavailable"),
      HttpStatusCode.ServiceUnavailable);

  /// <summary>Creates a 401 Unauthorized response.</summary>
  /// <returns>Configured HttpResponseMessage with 401 status.</returns>
  public static HttpResponseMessage Create401Response() =>
    CreateResponse(AuthenticationError(), HttpStatusCode.Unauthorized);

  /// <summary>Creates a 403 Forbidden response.</summary>
  /// <returns>Configured HttpResponseMessage with 403 status.</returns>
  public static HttpResponseMessage Create403Response() =>
    CreateResponse(ForbiddenError(), HttpStatusCode.Forbidden);

  /// <summary>Creates a 404 Not Found response.</summary>
  /// <param name="resourceType">Type of resource that was not found.</param>
  /// <returns>Configured HttpResponseMessage with 404 status.</returns>
  public static HttpResponseMessage Create404Response(string resourceType = "resource") =>
    CreateResponse(NotFoundError(resourceType), HttpStatusCode.NotFound);

  /// <summary>Creates a 400 Bad Request response.</summary>
  /// <param name="message">Error message.</param>
  /// <returns>Configured HttpResponseMessage with 400 status.</returns>
  public static HttpResponseMessage Create400Response(string message = "Bad request") =>
    CreateResponse(Error(6003, message), HttpStatusCode.BadRequest);

  #endregion
}
