namespace Cloudflare.NET.Tests.Shared.Helpers;

using System.Net;
using Cloudflare.NET.Core.Exceptions;
using Xunit;

/// <summary>
///   Centralized assertion helpers for Cloudflare API error handling tests.
///   Use these instead of duplicating error handling tests in every feature.
/// </summary>
public static class CloudflareApiTestHelpers
{
  #region API Error Assertions

  /// <summary>
  ///   Asserts that an API call throws CloudflareApiException with the expected error.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <param name="expectedCode">Expected error code.</param>
  /// <param name="expectedMessageContains">Expected substring in error message.</param>
  /// <returns>The thrown CloudflareApiException for further inspection.</returns>
  public static async Task<CloudflareApiException> AssertApiErrorAsync<T>(
    Func<Task<T>> action,
    int expectedCode,
    string? expectedMessageContains = null)
  {
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(async () => await action());

    Assert.NotEmpty(exception.Errors);
    Assert.Contains(exception.Errors, e => e.Code == expectedCode);

    if (expectedMessageContains != null)
    {
      Assert.Contains(exception.Errors, e => e.Message.Contains(expectedMessageContains));
    }

    return exception;
  }

  /// <summary>
  ///   Asserts that an API call throws CloudflareApiException with multiple errors.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <param name="expectedErrorCount">Expected number of errors.</param>
  /// <returns>The thrown CloudflareApiException for further inspection.</returns>
  public static async Task<CloudflareApiException> AssertMultipleApiErrorsAsync<T>(
    Func<Task<T>> action,
    int expectedErrorCount)
  {
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(async () => await action());

    Assert.Equal(expectedErrorCount, exception.Errors.Count);

    return exception;
  }

  /// <summary>
  ///   Asserts that an API call (void/Task) throws CloudflareApiException.
  /// </summary>
  /// <param name="action">The async action to execute.</param>
  /// <param name="expectedCode">Expected error code.</param>
  /// <returns>The thrown CloudflareApiException for further inspection.</returns>
  public static async Task<CloudflareApiException> AssertApiErrorAsync(
    Func<Task> action,
    int expectedCode)
  {
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(async () => await action());

    Assert.Contains(exception.Errors, e => e.Code == expectedCode);

    return exception;
  }

  /// <summary>
  ///   Asserts that an API call throws CloudflareApiException containing a specific error.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <param name="expectedErrorCodes">Expected error codes (any match is acceptable).</param>
  /// <returns>The thrown CloudflareApiException for further inspection.</returns>
  public static async Task<CloudflareApiException> AssertApiErrorAsync<T>(
    Func<Task<T>> action,
    params int[] expectedErrorCodes)
  {
    var exception = await Assert.ThrowsAsync<CloudflareApiException>(async () => await action());

    Assert.NotEmpty(exception.Errors);
    Assert.Contains(exception.Errors, e => expectedErrorCodes.Contains(e.Code));

    return exception;
  }

  #endregion


  #region HTTP Error Assertions

  /// <summary>
  ///   Asserts that an API call throws HttpRequestException with 401 Unauthorized.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertUnauthorizedAsync<T>(Func<Task<T>> action)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);

    return exception;
  }

  /// <summary>
  ///   Asserts that an API call throws HttpRequestException with 403 Forbidden.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertForbiddenAsync<T>(Func<Task<T>> action)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);

    return exception;
  }

  /// <summary>
  ///   Asserts that an API call throws HttpRequestException with 404 Not Found.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertNotFoundAsync<T>(Func<Task<T>> action)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);

    return exception;
  }

  /// <summary>
  ///   Asserts that an API call throws HttpRequestException with 429 Rate Limited.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertRateLimitedAsync<T>(Func<Task<T>> action)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);

    return exception;
  }

  /// <summary>
  ///   Asserts that an API call throws HttpRequestException with a 5xx server error.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <param name="expectedStatusCode">Expected 5xx status code (500, 502, 503).</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertServerErrorAsync<T>(
    Func<Task<T>> action,
    HttpStatusCode expectedStatusCode)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(expectedStatusCode, exception.StatusCode);

    return exception;
  }

  /// <summary>
  ///   Asserts that an API call throws HttpRequestException with 400 Bad Request.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertBadRequestAsync<T>(Func<Task<T>> action)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);

    return exception;
  }

  /// <summary>
  ///   Asserts that an API call throws HttpRequestException with the specified status code.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <param name="expectedStatusCode">Expected HTTP status code.</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertHttpErrorAsync<T>(
    Func<Task<T>> action,
    HttpStatusCode expectedStatusCode)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(expectedStatusCode, exception.StatusCode);

    return exception;
  }

  #endregion


  #region Void Overloads

  /// <summary>Asserts 401 Unauthorized for void async methods.</summary>
  /// <param name="action">The async action to execute.</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertUnauthorizedAsync(Func<Task> action)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);

    return exception;
  }

  /// <summary>Asserts 403 Forbidden for void async methods.</summary>
  /// <param name="action">The async action to execute.</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertForbiddenAsync(Func<Task> action)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);

    return exception;
  }

  /// <summary>Asserts 404 Not Found for void async methods.</summary>
  /// <param name="action">The async action to execute.</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertNotFoundAsync(Func<Task> action)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);

    return exception;
  }

  /// <summary>Asserts 429 Rate Limited for void async methods.</summary>
  /// <param name="action">The async action to execute.</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertRateLimitedAsync(Func<Task> action)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);

    return exception;
  }

  /// <summary>Asserts 400 Bad Request for void async methods.</summary>
  /// <param name="action">The async action to execute.</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertBadRequestAsync(Func<Task> action)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);

    return exception;
  }

  /// <summary>Asserts HTTP error for void async methods.</summary>
  /// <param name="action">The async action to execute.</param>
  /// <param name="expectedStatusCode">Expected HTTP status code.</param>
  /// <returns>The thrown HttpRequestException for further inspection.</returns>
  public static async Task<HttpRequestException> AssertHttpErrorAsync(
    Func<Task> action,
    HttpStatusCode expectedStatusCode)
  {
    var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await action());

    Assert.Equal(expectedStatusCode, exception.StatusCode);

    return exception;
  }

  #endregion
}
