namespace Cloudflare.NET.Tests.Shared.Base;

using System.Net;
using Microsoft.Extensions.Logging;
using Moq;

/// <summary>
///   Generic base class for unit tests targeting a specific API interface.
///   Provides automatic API instance creation with mocked dependencies.
/// </summary>
/// <typeparam name="TApi">The API interface being tested.</typeparam>
/// <typeparam name="TImpl">The API implementation class.</typeparam>
public abstract class CloudflareUnitTestBase<TApi, TImpl> : CloudflareUnitTestBase
  where TApi : class
  where TImpl : TApi
{
  #region Properties & Fields - Non-Public

  /// <summary>The API instance under test.</summary>
  protected TApi Api { get; private set; } = default!;

  /// <summary>The typed logger mock for the implementation.</summary>
  protected Mock<ILogger<TImpl>> TypedLoggerMock { get; } = new();

  #endregion


  #region Setup Methods

  /// <summary>
  ///   Creates the API instance after configuring the mock response.
  ///   Call this after SetupSuccessResponse or similar methods.
  /// </summary>
  /// <exception cref="InvalidOperationException">
  ///   Thrown if called before configuring a mock response.
  /// </exception>
  protected void CreateApi()
  {
    if (HttpClient == null)
    {
      throw new InvalidOperationException(
        "Must call SetupSuccessResponse or similar before CreateApi()");
    }

    // Create API instance via reflection (assumes HttpClient + ILogger constructor)
    Api = (TApi)Activator.CreateInstance(typeof(TImpl), HttpClient, TypedLoggerMock.Object)!;
  }

  /// <summary>
  ///   Creates the API instance using a custom factory function.
  ///   Use this when the API has a non-standard constructor.
  /// </summary>
  /// <param name="factory">Factory function that creates the API instance.</param>
  protected void CreateApi(Func<HttpClient, ILogger<TImpl>, TApi> factory)
  {
    if (HttpClient == null)
    {
      throw new InvalidOperationException(
        "Must call SetupSuccessResponse or similar before CreateApi()");
    }

    Api = factory(HttpClient, TypedLoggerMock.Object);
  }

  /// <summary>
  ///   Convenience method: sets up success response and creates API.
  /// </summary>
  /// <typeparam name="T">Result type.</typeparam>
  /// <param name="result">The result to return.</param>
  protected void SetupAndCreate<T>(T result)
  {
    SetupSuccessResponse(result);
    CreateApi();
  }

  /// <summary>
  ///   Convenience method: sets up paginated response and creates API.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="items">Items to return.</param>
  /// <param name="page">Current page.</param>
  /// <param name="perPage">Items per page.</param>
  /// <param name="totalCount">Total item count.</param>
  protected void SetupPaginatedAndCreate<T>(
    IEnumerable<T> items,
    int page = 1,
    int perPage = 20,
    int totalCount = 1)
  {
    SetupPagePaginatedResponse(items, page, perPage, totalCount);
    CreateApi();
  }

  /// <summary>
  ///   Convenience method: sets up cursor-paginated response and creates API.
  /// </summary>
  /// <typeparam name="T">Item type.</typeparam>
  /// <param name="items">Items to return.</param>
  /// <param name="perPage">Items per page.</param>
  /// <param name="cursor">Cursor for next page.</param>
  protected void SetupCursorPaginatedAndCreate<T>(
    IEnumerable<T> items,
    int perPage = 20,
    string? cursor = null)
  {
    SetupCursorPaginatedResponse(items, perPage, cursor);
    CreateApi();
  }

  /// <summary>
  ///   Convenience method: sets up API error response and creates API.
  /// </summary>
  /// <param name="code">Error code.</param>
  /// <param name="message">Error message.</param>
  protected void SetupApiErrorAndCreate(int code, string message)
  {
    SetupApiErrorResponse(code, message);
    CreateApi();
  }

  /// <summary>
  ///   Convenience method: sets up HTTP error response and creates API.
  /// </summary>
  /// <param name="statusCode">HTTP status code.</param>
  protected void SetupHttpErrorAndCreate(HttpStatusCode statusCode)
  {
    SetupHttpErrorResponse(statusCode);
    CreateApi();
  }

  /// <summary>
  ///   Convenience method: sets up raw JSON response and creates API.
  /// </summary>
  /// <param name="json">Raw JSON response.</param>
  /// <param name="statusCode">HTTP status code.</param>
  protected void SetupRawAndCreate(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
  {
    SetupRawResponse(json, statusCode);
    CreateApi();
  }

  #endregion
}
