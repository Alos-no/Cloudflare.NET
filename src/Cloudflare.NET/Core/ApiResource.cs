namespace Cloudflare.NET.Core;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Exceptions;
using Models;

/// <summary>Base class for API resource clients, providing shared functionality.</summary>
public abstract class ApiResource
{
  #region Properties & Fields - Non-Public

  /// <summary>The configured HttpClient for making API requests.</summary>
  private readonly HttpClient _httpClient;

  /// <summary>JSON serializer options for snake_case conversion.</summary>
  private readonly JsonSerializerOptions _serializerOptions;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the ApiResource.</summary>
  /// <param name="httpClient">The HttpClient to use for requests.</param>
  protected ApiResource(HttpClient httpClient)
  {
    _httpClient = httpClient;
    _serializerOptions = new JsonSerializerOptions
    {
      PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
  }

  #endregion

  #region Methods

  /// <summary>Sends a GET request to the specified URI.</summary>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> GetAsync<TResult>(string requestUri, CancellationToken cancellationToken = default)
  {
    var response = await _httpClient.GetAsync(requestUri, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Sends a POST request to the specified URI.</summary>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> PostAsync<TResult>(string requestUri, object? payload, CancellationToken cancellationToken = default)
  {
    var response = await _httpClient.PostAsJsonAsync(requestUri, payload, _serializerOptions, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Sends a PUT request to the specified URI.</summary>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> PutAsync<TResult>(string requestUri, object? payload, CancellationToken cancellationToken = default)
  {
    var response = await _httpClient.PutAsJsonAsync(requestUri, payload, _serializerOptions, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Sends a PATCH request to the specified URI.</summary>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> PatchAsync<TResult>(string requestUri, object? payload, CancellationToken cancellationToken = default)
  {
    var response = await _httpClient.PatchAsJsonAsync(requestUri, payload, _serializerOptions, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Sends a DELETE request to the specified URI.</summary>
  /// <returns>The deserialized "result" object from the API response.</returns>
  protected async Task<TResult> DeleteAsync<TResult>(string requestUri, CancellationToken cancellationToken = default)
  {
    var response = await _httpClient.DeleteAsync(requestUri, cancellationToken);
    return await ProcessResponse<TResult>(response, cancellationToken);
  }

  /// <summary>Processes the HttpResponseMessage from a Cloudflare API call.</summary>
  /// <typeparam name="T">The expected type of the "result" object in the JSON response.</typeparam>
  /// <param name="response">The HttpResponseMessage to process.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The deserialized "result" object.</returns>
  /// <exception cref="HttpRequestException">
  ///   Thrown if the API call returns a non-success status
  ///   code.
  /// </exception>
  /// <exception cref="JsonException">Thrown if the API response body cannot be deserialized.</exception>
  /// <exception cref="CloudflareApiException">
  ///   Thrown if the API returns a success status code
  ///   but the response indicates failure (e.g., `success: false`).
  /// </exception>
  private async Task<T> ProcessResponse<T>(HttpResponseMessage response, CancellationToken cancellationToken)
  {
    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

    // If the status code is not successful, we throw a detailed HttpRequestException.
    if (!response.IsSuccessStatusCode)
      // This will create a detailed exception message including the status code, reason, and response body.
      throw new HttpRequestException(
        $"Cloudflare API request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}). Response Body: {responseBody}",
        null,
        response.StatusCode);

    // Attempt to deserialize the standard Cloudflare API envelope.
    ApiResponse<T>? cloudflareResponse;
    try
    {
      cloudflareResponse = JsonSerializer.Deserialize<ApiResponse<T>>(responseBody, _serializerOptions);
    }
    catch (JsonException ex)
    {
      // If deserialization fails, throw an exception with the raw response body for debugging.
      throw new JsonException($"Failed to deserialize Cloudflare API response. Raw response: {responseBody}", ex);
    }

    // Check the 'success' flag within the Cloudflare response body.
    if (cloudflareResponse is null || !cloudflareResponse.Success)
    {
      var errors        = cloudflareResponse?.Errors ?? [];
      var errorMessages = string.Join(", ", errors.Select(e => $"[{e.Code}] {e.Message}"));
      throw new CloudflareApiException(
        $"Cloudflare API returned a failure response: {errorMessages}. Raw response: {responseBody}",
        errors);
    }

    // The result can be null for some successful operations (e.g., DELETE),
    // so we allow it and return default.
    return cloudflareResponse.Result ?? default!;
  }

  #endregion
}
