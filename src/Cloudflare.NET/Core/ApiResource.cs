namespace Cloudflare.NET.Core;

using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Exceptions;
using Microsoft.Extensions.Logging;
using Models;
#if !NET8_0_OR_GREATER
using Json;
#endif

/// <summary>Base class for API resource clients, providing shared functionality.</summary>
public abstract class ApiResource
{
  #region Properties & Fields - Non-Public

  /// <summary>The configured HttpClient for making API requests.</summary>
  protected readonly HttpClient HttpClient;

  /// <summary>The logger for this API resource.</summary>
  protected readonly ILogger Logger;

  /// <summary>JSON serializer options for snake_case conversion.</summary>
  private readonly JsonSerializerOptions _serializerOptions;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the ApiResource.</summary>
  /// <param name="httpClient">The HttpClient to use for requests.</param>
  /// <param name="logger">The logger for this API resource.</param>
  protected ApiResource(HttpClient httpClient, ILogger logger)
  {
    HttpClient = httpClient;
    Logger     = logger;
    _serializerOptions = new JsonSerializerOptions
    {
#if NET8_0_OR_GREATER
      PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
#else
      PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
#endif
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
  }

  #endregion

  #region Methods

  /// <summary>Sends a GET request to the specified URI.</summary>
  /// <typeparam name="TResult">The expected type of the "result" object in the JSON response.</typeparam>
  /// <param name="requestUri">The URI to send the request to.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> GetAsync<TResult>(string requestUri, CancellationToken cancellationToken = default)
  {
    using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
    Logger.SendingRequest("GET", requestUri);
    var response = await HttpClient.GetAsync(requestUri, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Sends a POST request with a JSON payload to the specified URI.</summary>
  /// <typeparam name="TResult">The expected type of the "result" object in the JSON response.</typeparam>
  /// <param name="requestUri">The URI to send the request to.</param>
  /// <param name="payload">The object to serialize as the JSON request body.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> PostAsync<TResult>(string            requestUri,
                                                   object?           payload,
                                                   CancellationToken cancellationToken = default)
  {
    using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
    Logger.SendingRequest("POST", requestUri);

    var response = await HttpClient.PostAsJsonAsync(requestUri, payload, _serializerOptions, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Sends a PUT request with a JSON payload to the specified URI.</summary>
  /// <typeparam name="TResult">The expected type of the "result" object in the JSON response.</typeparam>
  /// <param name="requestUri">The URI to send the request to.</param>
  /// <param name="payload">The object to serialize as the JSON request body.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> PutAsync<TResult>(string requestUri, object? payload, CancellationToken cancellationToken = default)
  {
    using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
    Logger.SendingRequest("PUT", requestUri);
    var response = await HttpClient.PutAsJsonAsync(requestUri, payload, _serializerOptions, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Sends a POST request with a JSON payload and optional custom headers to the specified URI.</summary>
  /// <typeparam name="TResult">The expected type of the "result" object in the JSON response.</typeparam>
  /// <param name="requestUri">The URI to send the request to.</param>
  /// <param name="payload">The object to serialize as the JSON request body.</param>
  /// <param name="headers">Optional custom headers to include in the request.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> PostAsync<TResult>(
    string                              requestUri,
    object?                             payload,
    IEnumerable<KeyValuePair<string, string>>? headers,
    CancellationToken                   cancellationToken = default)
  {
    using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
    Logger.SendingRequest("POST", requestUri);

    var jsonContent = System.Text.Json.JsonSerializer.Serialize(payload, _serializerOptions);
    var content     = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

    using var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };

    if (headers is not null)
    {
      foreach (var header in headers)
      {
        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
      }
    }

    var response = await HttpClient.SendAsync(request, cancellationToken);

    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Sends a POST request with a pre-serialized JSON string to the specified URI.</summary>
  /// <remarks>
  ///   Use this method when you need custom JSON serialization (e.g., camelCase instead of snake_case). The caller is
  ///   responsible for serializing the payload to JSON.
  /// </remarks>
  /// <typeparam name="TResult">The expected type of the "result" object in the JSON response.</typeparam>
  /// <param name="requestUri">The URI to send the request to.</param>
  /// <param name="jsonContent">The pre-serialized JSON string to send as the request body.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> PostJsonAsync<TResult>(string            requestUri,
                                                       string            jsonContent,
                                                       CancellationToken cancellationToken = default)
  {
    using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
    Logger.SendingRequest("POST", requestUri);
    var content  = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
    var response = await HttpClient.PostAsync(requestUri, content, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Sends a PUT request with a pre-serialized JSON string to the specified URI.</summary>
  /// <remarks>
  ///   Use this method when you need custom JSON serialization (e.g., camelCase instead of snake_case). The caller is
  ///   responsible for serializing the payload to JSON.
  /// </remarks>
  /// <typeparam name="TResult">The expected type of the "result" object in the JSON response.</typeparam>
  /// <param name="requestUri">The URI to send the request to.</param>
  /// <param name="jsonContent">The pre-serialized JSON string to send as the request body.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> PutJsonAsync<TResult>(string            requestUri,
                                                      string            jsonContent,
                                                      CancellationToken cancellationToken = default)
  {
    using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
    Logger.SendingRequest("PUT", requestUri);
    var content  = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
    var response = await HttpClient.PutAsync(requestUri, content, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Sends a PATCH request with a JSON payload to the specified URI.</summary>
  /// <typeparam name="TResult">The expected type of the "result" object in the JSON response.</typeparam>
  /// <param name="requestUri">The URI to send the request to.</param>
  /// <param name="payload">The object to serialize as the JSON request body.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> PatchAsync<TResult>(string            requestUri,
                                                    object?           payload,
                                                    CancellationToken cancellationToken = default)
  {
    using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
    Logger.SendingRequest("PATCH", requestUri);
    var response = await HttpClient.PatchAsJsonAsync(requestUri, payload, _serializerOptions, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Sends a DELETE request to the specified URI.</summary>
  /// <typeparam name="TResult">The expected type of the "result" object in the JSON response.</typeparam>
  /// <param name="requestUri">The URI to send the request to.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> DeleteAsync<TResult>(string requestUri, CancellationToken cancellationToken = default)
  {
    using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
    Logger.SendingRequest("DELETE", requestUri);
    var response = await HttpClient.DeleteAsync(requestUri, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Sends a GET request to an endpoint that returns a raw string response (e.g., BIND file export).</summary>
  /// <param name="requestUri">The URI to send the request to.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The raw string content of the response body.</returns>
  /// <exception cref="CloudflareApiException">Thrown if the API returns a JSON error envelope.</exception>
  /// <exception cref="HttpRequestException">Thrown if the API returns a non-success status code that is not a JSON error.</exception>
  protected async Task<string> GetStringAsync(string requestUri, CancellationToken cancellationToken = default)
  {
    using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
    Logger.SendingRequest("GET", requestUri);
    var response = await HttpClient.GetAsync(requestUri, cancellationToken);
    Logger.ReceivedResponse(response.StatusCode, response.RequestMessage?.RequestUri);
    // ReadAsStringAsync with CancellationToken is only available in .NET 5+.
#if NET5_0_OR_GREATER
    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
#else
    var responseBody = await response.Content.ReadAsStringAsync();
#endif

    if (!response.IsSuccessStatusCode)
    {
      Logger.RequestFailed(
        response.RequestMessage?.RequestUri,
        (int)response.StatusCode,
        response.ReasonPhrase,
        responseBody);

      // Attempt to parse the body as a standard Cloudflare JSON error.
      try
      {
        ApiResponse<object>? cloudflareResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, _serializerOptions);
        if (cloudflareResponse is { Success: false, Errors.Count: > 0 })
          throw new CloudflareApiException(
            "Cloudflare API returned a failure response.",
            cloudflareResponse.Errors);
      }
      catch (JsonException)
      {
        // Ignore if not a JSON error; the HttpRequestException below will be thrown.
      }

#if NET5_0_OR_GREATER
      throw new HttpRequestException(
        $"Cloudflare API request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}). Response Body: {responseBody}",
        null,
        response.StatusCode);
#else
      throw new HttpRequestException(
        $"Cloudflare API request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}). Response Body: {responseBody}");
#endif
    }

    return responseBody;
  }

  /// <summary>Sends a POST request with a multipart/form-data payload containing a single file stream.</summary>
  /// <typeparam name="TResult">The expected type of the "result" object in the JSON response.</typeparam>
  /// <param name="requestUri">The URI to send the request to.</param>
  /// <param name="stream">The stream content to upload.</param>
  /// <param name="fileName">The name of the file in the form data.</param>
  /// <param name="formKey">The key for the file in the form data.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> PostMultipartFileAsync<TResult>(
    string            requestUri,
    Stream            stream,
    string            fileName,
    string            formKey           = "file",
    CancellationToken cancellationToken = default)
  {
    using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
    Logger.SendingRequest("POST", requestUri);

    using var content = new MultipartFormDataContent();
    content.Add(new StreamContent(stream), formKey, fileName);

    var response = await HttpClient.PostAsync(requestUri, content, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Gets a single page of results from a page-based paginated endpoint.</summary>
  /// <returns>A result object containing the items for the page and the pagination metadata.</returns>
  protected async Task<PagePaginatedResult<TItem>> GetPagePaginatedResultAsync<TItem>(
    string            requestUri,
    CancellationToken cancellationToken = default)
  {
    using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
    Logger.SendingRequest("GET", requestUri);

    var httpResponse = await HttpClient.GetAsync(requestUri, cancellationToken);
    var apiResponse  = await ProcessAndDeserializeAsync<IReadOnlyList<TItem>>(httpResponse, cancellationToken);
    var items        = apiResponse.Result ?? [];

    return new PagePaginatedResult<TItem>(items, apiResponse.ResultInfo);
  }

  /// <summary>Gets a single page of results from a cursor-based paginated endpoint.</summary>
  /// <returns>A result object containing the items for the page and the cursor metadata.</returns>
  protected Task<CursorPaginatedResult<TItem>> GetCursorPaginatedResultAsync<TItem>(
    string            requestUri,
    CancellationToken cancellationToken = default)
  {
    // This is a convenience overload for the common case where the API returns a direct list.
    // The "wrapper" is the list itself, and the extractor is an identity function.
    return GetCursorPaginatedResultAsync<IReadOnlyList<TItem>, TItem>(
      requestUri,
      wrapper => wrapper,
      cancellationToken);
  }


  /// <summary>
  ///   Gets a single page of results from a cursor-based paginated endpoint where the item list is nested within a
  ///   wrapper object in the API response. This is the core implementation.
  /// </summary>
  /// <typeparam name="TWrapper">The type of the wrapper object in the 'result' field.</typeparam>
  /// <typeparam name="TItem">The type of the item in the list.</typeparam>
  /// <param name="requestUri">The full request URI for the API endpoint.</param>
  /// <param name="listExtractor">A function to extract the list of items from the wrapper object.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A result object containing the items for the page and the cursor metadata.</returns>
  protected async Task<CursorPaginatedResult<TItem>> GetCursorPaginatedResultAsync<TWrapper, TItem>(
    string                               requestUri,
    Func<TWrapper, IReadOnlyList<TItem>> listExtractor,
    CancellationToken                    cancellationToken = default)
  {
    using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
    Logger.SendingRequest("GET", requestUri);

    var httpResponse = await HttpClient.GetAsync(requestUri, cancellationToken);
    var apiResponse  = await ProcessAndDeserializeAsync<TWrapper>(httpResponse, cancellationToken);

    var items = apiResponse.Result is not null
      ? listExtractor(apiResponse.Result)
      : [];

    // Some Cloudflare APIs (e.g., R2 Buckets) return cursor in the standard result_info field,
    // while others use a separate cursor_result_info field. Create a unified CursorResultInfo.
    var cursorInfo = apiResponse.CursorResultInfo
      ?? (apiResponse.ResultInfo?.Cursor != null
        ? new CursorResultInfo(
            apiResponse.ResultInfo.Count,
            apiResponse.ResultInfo.PerPage,
            apiResponse.ResultInfo.Cursor)
        : null);

    return new CursorPaginatedResult<TItem>(items, cursorInfo);
  }

  /// <summary>Gets a paginated list of resources, automatically handling multiple pages of results.</summary>
  /// <typeparam name="TItem">The type of the item in the list.</typeparam>
  /// <param name="baseUri">The base request URI, without any page or per_page parameters.</param>
  /// <param name="perPage">The number of items to request per page. If null, the API default is used.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of items.</returns>
  protected async IAsyncEnumerable<TItem> GetPaginatedAsync<TItem>(
    string                                     baseUri,
    int?                                       perPage           = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var currentPage  = 1;
    var hasMorePages = true;
    var separator    = baseUri.Contains('?') ? "&" : "?";
    var perPageQuery = perPage.HasValue ? $"&per_page={perPage.Value}" : string.Empty;

    while (hasMorePages)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var requestUri = $"{baseUri}{separator}page={currentPage}{perPageQuery}";

      using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
      Logger.SendingRequest("GET", requestUri);
      var httpResponse = await HttpClient.GetAsync(requestUri, cancellationToken);
      ApiResponse<IReadOnlyList<TItem>> apiResponse =
        await ProcessAndDeserializeAsync<IReadOnlyList<TItem>>(httpResponse, cancellationToken);

      var items = apiResponse.Result ?? [];

      foreach (var item in items)
        yield return item;

      var resultInfo = apiResponse.ResultInfo;
      // The loop terminates if result_info is missing, if the current page was empty, or if we've reached the last page.
      if (resultInfo is not null && resultInfo.Count > 0 && resultInfo.Page < resultInfo.TotalPages)
        currentPage++;
      else
        hasMorePages = false;
    }
  }

  /// <summary>Gets a paginated list of resources using cursor-based pagination.</summary>
  /// <typeparam name="TItem">The type of the item in the list.</typeparam>
  /// <param name="baseUri">The base request URI, without any cursor or per_page parameters.</param>
  /// <param name="perPage">The number of items to request per page.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of items.</returns>
  protected IAsyncEnumerable<TItem> GetCursorPaginatedAsync<TItem>(
    string            baseUri,
    int?              perPage,
    CancellationToken cancellationToken)
  {
    // This overload delegates to the core implementation with an identity extractor.
    return GetCursorPaginatedAsync<IReadOnlyList<TItem>, TItem>(
      baseUri,
      perPage,
      wrapper => wrapper,
      cancellationToken);
  }

  /// <summary>
  ///   Gets a paginated list of resources using cursor-based pagination where the item list is nested within a
  ///   wrapper object. This is the core implementation.
  /// </summary>
  /// <typeparam name="TWrapper">The type of the wrapper object in the 'result' field.</typeparam>
  /// <typeparam name="TItem">The type of the item in the list.</typeparam>
  /// <param name="baseUri">The base request URI, without any cursor or per_page parameters.</param>
  /// <param name="perPage">The number of items to request per page.</param>
  /// <param name="listExtractor">A function to extract the list of items from the wrapper object.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of items.</returns>
  protected async IAsyncEnumerable<TItem> GetCursorPaginatedAsync<TWrapper, TItem>(
    string                                     baseUri,
    int?                                       perPage,
    Func<TWrapper, IReadOnlyList<TItem>>       listExtractor,
    [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    var nextCursor   = (string?)null;
    var hasMorePages = true;
    var separator    = baseUri.Contains('?') ? "&" : "?";
    var perPageQuery = perPage.HasValue ? $"&per_page={perPage.Value}" : string.Empty;

    do
    {
      cancellationToken.ThrowIfCancellationRequested();

      var cursorQuery = !string.IsNullOrEmpty(nextCursor) ? $"&cursor={nextCursor}" : string.Empty;
      var requestUri  = $"{baseUri}{separator}{perPageQuery}{cursorQuery}";

      using var scope = Logger.BeginScope("RequestUri: {RequestUri}", requestUri);
      Logger.SendingRequest("GET", requestUri);

      var httpResponse = await HttpClient.GetAsync(requestUri, cancellationToken);
      var apiResponse =
        await ProcessAndDeserializeAsync<TWrapper>(httpResponse, cancellationToken);

      var items = apiResponse.Result is not null
        ? listExtractor(apiResponse.Result)
        : [];

      foreach (var item in items)
        yield return item;

      // Some Cloudflare APIs (e.g., R2 Buckets) return cursor in the standard result_info field,
      // while others use a separate cursor_result_info field. Check both sources for the cursor.
      var cursor = apiResponse.CursorResultInfo?.Cursor ?? apiResponse.ResultInfo?.Cursor;

      if (!string.IsNullOrEmpty(cursor))
        nextCursor = cursor;
      else
        hasMorePages = false;
    } while (hasMorePages);
  }

  /// <summary>Processes the HttpResponseMessage from a Cloudflare API call.</summary>
  /// <typeparam name="T">The expected type of the "result" object in the JSON response.</typeparam>
  /// <param name="response">The HttpResponseMessage to process.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The deserialized "result" object.</returns>
  /// <exception cref="HttpRequestException">Thrown if the API call returns a non-success status code.</exception>
  /// <exception cref="JsonException">Thrown if the API response body cannot be deserialized.</exception>
  /// <exception cref="CloudflareApiException">
  ///   Thrown if the API returns a success status code but the response indicates
  ///   failure (e.g., `success: false`).
  /// </exception>
  protected async Task<T> ProcessResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
  {
    var apiResponse = await ProcessAndDeserializeAsync<T>(response, cancellationToken);

    // The result can be null for some successful operations (e.g., DELETE),
    // so we allow it and return default.
    return apiResponse.Result ?? default!;
  }

  /// <summary>
  ///   Handles the common logic for processing an API response: checking for non-success status, deserializing, and
  ///   checking the internal 'success' flag.
  /// </summary>
  /// <returns>The full, successfully deserialized <see cref="ApiResponse{T}" />.</returns>
  protected async Task<ApiResponse<T>> ProcessAndDeserializeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
  {
    var requestUri = response.RequestMessage?.RequestUri;
    Logger.ReceivedResponse(response.StatusCode, requestUri);

    // ReadAsStringAsync with CancellationToken is only available in .NET 5+.
#if NET5_0_OR_GREATER
    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
#else
    var responseBody = await response.Content.ReadAsStringAsync();
#endif

    // If the status code is not successful, we throw a detailed HttpRequestException.
    if (!response.IsSuccessStatusCode)
    {
      Logger.RequestFailed(
        requestUri,
        (int)response.StatusCode,
        response.ReasonPhrase,
        responseBody);

      // Enhanced diagnostics: CF-RAY can appear multiple times; Retry-After may be a delta or an absolute date; any may be missing.
      var cfRayDisplay = "none";

      if (response.Headers.TryGetValues("CF-RAY", out var cfRayValues))
        cfRayDisplay = string.Join(", ", cfRayValues);

      string retryAfterDisplay;
      var    retryAfter = response.Headers.RetryAfter;

      if (retryAfter is null)
      {
        retryAfterDisplay = "none";
      }
      else if (retryAfter.Delta is { } delta)
      {
        var seconds = Math.Max(0, (long)Math.Ceiling(delta.TotalSeconds));
        retryAfterDisplay = $"{seconds}s";
      }
      else if (retryAfter.Date is { } date)
      {
        var delay = date - DateTimeOffset.UtcNow;
        if (delay > TimeSpan.Zero)
        {
          var seconds = (long)Math.Ceiling(delay.TotalSeconds);
          retryAfterDisplay = $"{seconds}s (until {date:O})";
        }
        else
        {
          retryAfterDisplay = $"expired at {date:O}";
        }
      }
      else
      {
        retryAfterDisplay = "unrecognized format";
      }

      var dateHeaderDisplay = response.Headers.Date?.ToString() ?? "none";

      Logger.LogDiagnostics(
        cfRayDisplay,
        retryAfterDisplay,
        dateHeaderDisplay);

      // This will create a detailed exception message including the status code, reason, and response body.
#if NET5_0_OR_GREATER
      throw new HttpRequestException(
        $"Cloudflare API request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}). Response Body: {responseBody}",
        null,
        response.StatusCode);
#else
      throw new HttpRequestException(
        $"Cloudflare API request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}). Response Body: {responseBody}");
#endif
    }

    // Attempt to deserialize the standard Cloudflare API envelope.
    ApiResponse<T>? cloudflareResponse;
    try
    {
      cloudflareResponse = JsonSerializer.Deserialize<ApiResponse<T>>(responseBody, _serializerOptions);
    }
    catch (JsonException ex)
    {
      Logger.DeserializationFailed(ex, requestUri, responseBody);
      // If deserialization fails, throw an exception with the raw response body for debugging.
      throw new JsonException($"Failed to deserialize Cloudflare API response. Raw response: {responseBody}", ex);
    }

    // Check the 'success' flag within the Cloudflare response body.
    if (cloudflareResponse is null || !cloudflareResponse.Success)
    {
      var errors        = cloudflareResponse?.Errors ?? [];
      var errorMessages = string.Join(", ", errors.Select(e => $"[{e.Code}] {e.Message}"));
      Logger.ApiReturnedFailure(
        requestUri,
        errorMessages,
        responseBody);
      throw new CloudflareApiException(
        $"Cloudflare API returned a failure response: {errorMessages}. Raw response: {responseBody}",
        errors);
    }

    Logger.ProcessedSuccessResponse(requestUri);

    return cloudflareResponse;
  }

  #endregion
}
