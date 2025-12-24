namespace Cloudflare.NET.Accounts.Kv;

using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core;
using Core.Internal;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;

/// <summary>Implementation of Workers KV API operations.</summary>
public class KvApi : ApiResource, IKvApi
{
  #region Constants

  /// <summary>The HTTP header name for KV value expiration.</summary>
  private const string ExpirationHeaderName = "expiration";

  #endregion


  #region Properties & Fields - Non-Public

  /// <summary>The Cloudflare Account ID.</summary>
  private readonly string _accountId;

  /// <summary>JSON serializer options for camelCase (used by bulk get endpoint).</summary>
  private readonly JsonSerializerOptions _camelCaseOptions;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="KvApi" /> class.</summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  /// <param name="options">The Cloudflare API options containing the account ID.</param>
  /// <param name="loggerFactory">The factory to create loggers.</param>
  public KvApi(HttpClient httpClient, IOptions<CloudflareApiOptions> options, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<KvApi>())
  {
    _accountId = options.Value.AccountId;

    // The KV Bulk Get endpoint uses camelCase for request body properties.
    _camelCaseOptions = new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
  }

  #endregion


  #region Helper Methods

  /// <summary>URL-encodes a key name for use in API paths.</summary>
  /// <param name="key">The key name to encode.</param>
  /// <returns>The URL-encoded key name.</returns>
  private static string EncodeKeyName(string key) => Uri.EscapeDataString(key);

  /// <summary>Builds query string parameters for expiration options.</summary>
  /// <param name="options">The write options containing expiration settings.</param>
  /// <returns>Query string (including leading '?') or empty string if no expiration.</returns>
  private static string BuildExpirationQueryParams(KvWriteOptions? options)
  {
    if (options == null)
      return string.Empty;

    var parts = new List<string>();

    if (options.Expiration.HasValue)
      parts.Add($"expiration={options.Expiration.Value}");
    else if (options.ExpirationTtl.HasValue)
      parts.Add($"expiration_ttl={options.ExpirationTtl.Value}");

    return parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
  }

  /// <summary>Extracts the expiration timestamp from the HTTP response headers.</summary>
  /// <param name="response">The HTTP response.</param>
  /// <returns>The expiration timestamp, or null if not present.</returns>
  private static long? ExtractExpirationHeader(HttpResponseMessage response)
  {
    if (response.Headers.TryGetValues(ExpirationHeaderName, out var expirationValues))
    {
      var expirationStr = expirationValues.FirstOrDefault();

      if (!string.IsNullOrEmpty(expirationStr) && long.TryParse(expirationStr, out var exp))
        return exp;
    }

    return null;
  }

  #endregion


  #region Namespace Operations

  /// <inheritdoc />
  public async Task<PagePaginatedResult<KvNamespace>> ListAsync(
    ListKvNamespacesFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    var queryParams = new List<string>();

    if (filters?.Page.HasValue == true)
      queryParams.Add($"page={filters.Page.Value}");

    if (filters?.PerPage.HasValue == true)
      queryParams.Add($"per_page={filters.PerPage.Value}");

    if (filters?.Order.HasValue == true)
      queryParams.Add($"order={EnumHelper.GetEnumMemberValue(filters.Order.Value)}");

    if (filters?.Direction.HasValue == true)
      queryParams.Add($"direction={EnumHelper.GetEnumMemberValue(filters.Direction.Value)}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces{queryString}";

    return await GetPagePaginatedResultAsync<KvNamespace>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async IAsyncEnumerable<KvNamespace> ListAllAsync(
    ListKvNamespacesFilters? filters = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var page = 1;
    var hasMore = true;

    while (hasMore)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var pageFilters = new ListKvNamespacesFilters(
        Page: page,
        PerPage: filters?.PerPage ?? 100,
        Order: filters?.Order,
        Direction: filters?.Direction
      );

      var result = await ListAsync(pageFilters, cancellationToken);

      foreach (var ns in result.Items)
      {
        yield return ns;
      }

      // Check if there are more pages.
      hasMore = result.PageInfo != null
        && result.PageInfo.Page < result.PageInfo.TotalPages;

      page++;
    }
  }

  /// <inheritdoc />
  public async Task<KvNamespace> CreateAsync(
    string title,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces";
    var request = new CreateKvNamespaceRequest(title);

    return await PostAsync<KvNamespace>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<KvNamespace> GetAsync(
    string namespaceId,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}";

    return await GetAsync<KvNamespace>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<KvNamespace> RenameAsync(
    string namespaceId,
    string title,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}";
    var request = new RenameKvNamespaceRequest(title);

    return await PutAsync<KvNamespace>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteAsync(
    string namespaceId,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}";

    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  #endregion


  #region Key Operations

  /// <inheritdoc />
  public async Task<CursorPaginatedResult<KvKey>> ListKeysAsync(
    string namespaceId,
    ListKvKeysFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    var queryParams = new List<string>();

    if (!string.IsNullOrEmpty(filters?.Prefix))
      queryParams.Add($"prefix={Uri.EscapeDataString(filters.Prefix)}");

    if (filters?.Limit.HasValue == true)
      queryParams.Add($"limit={filters.Limit.Value}");

    if (!string.IsNullOrEmpty(filters?.Cursor))
      queryParams.Add($"cursor={Uri.EscapeDataString(filters.Cursor)}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}/keys{queryString}";

    return await GetCursorPaginatedResultAsync<KvKey>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async IAsyncEnumerable<KvKey> ListAllKeysAsync(
    string namespaceId,
    string? prefix = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    string? cursor = null;

    do
    {
      cancellationToken.ThrowIfCancellationRequested();

      var result = await ListKeysAsync(
        namespaceId,
        new ListKvKeysFilters(Prefix: prefix, Limit: 1000, Cursor: cursor),
        cancellationToken);

      foreach (var key in result.Items)
      {
        yield return key;
      }

      // Get cursor from CursorInfo (standard envelope pattern).
      cursor = result.CursorInfo?.Cursor;
    }
    while (!string.IsNullOrEmpty(cursor));
  }

  #endregion


  #region Value Operations

  /// <inheritdoc />
  public async Task<string?> GetValueAsync(
    string namespaceId,
    string key,
    CancellationToken cancellationToken = default)
  {
    var result = await GetValueWithExpirationAsync(namespaceId, key, cancellationToken);

    return result?.Value;
  }

  /// <inheritdoc />
  public async Task<KvStringValueResult?> GetValueWithExpirationAsync(
    string namespaceId,
    string key,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}/values/{EncodeKeyName(key)}";

    Logger.SendingRequest("GET", endpoint);

    var response = await HttpClient.GetAsync(endpoint, cancellationToken);

    Logger.ReceivedResponse(response.StatusCode, response.RequestMessage?.RequestUri);

    // Return null if key doesn't exist.
    if (response.StatusCode == HttpStatusCode.NotFound)
      return null;

    // Handle other error responses.
    if (!response.IsSuccessStatusCode)
    {
      var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
      throw new HttpRequestException(
        $"Cloudflare API request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}). Response Body: {responseBody}",
        null,
        response.StatusCode);
    }

    var value = await response.Content.ReadAsStringAsync(cancellationToken);
    var expiration = ExtractExpirationHeader(response);

    return new KvStringValueResult(value, expiration);
  }

  /// <inheritdoc />
  public async Task<byte[]?> GetValueBytesAsync(
    string namespaceId,
    string key,
    CancellationToken cancellationToken = default)
  {
    var result = await GetValueBytesWithExpirationAsync(namespaceId, key, cancellationToken);

    return result?.Value;
  }

  /// <inheritdoc />
  public async Task<KvValueResult?> GetValueBytesWithExpirationAsync(
    string namespaceId,
    string key,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}/values/{EncodeKeyName(key)}";

    Logger.SendingRequest("GET", endpoint);

    var response = await HttpClient.GetAsync(endpoint, cancellationToken);

    Logger.ReceivedResponse(response.StatusCode, response.RequestMessage?.RequestUri);

    // Return null if key doesn't exist.
    if (response.StatusCode == HttpStatusCode.NotFound)
      return null;

    // Handle other error responses.
    if (!response.IsSuccessStatusCode)
    {
      var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
      throw new HttpRequestException(
        $"Cloudflare API request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}). Response Body: {responseBody}",
        null,
        response.StatusCode);
    }

    var value = await response.Content.ReadAsByteArrayAsync(cancellationToken);
    var expiration = ExtractExpirationHeader(response);

    return new KvValueResult(value, expiration);
  }

  /// <inheritdoc />
  public async Task<JsonElement?> GetMetadataAsync(
    string namespaceId,
    string key,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}/metadata/{EncodeKeyName(key)}";

    try
    {
      return await GetAsync<JsonElement?>(endpoint, cancellationToken);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
      return null;
    }
  }

  /// <inheritdoc />
  public async Task WriteValueAsync(
    string namespaceId,
    string key,
    string value,
    KvWriteOptions? options = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}/values/{EncodeKeyName(key)}";

    // Expiration is always passed via query params (even with multipart).
    var queryParams = BuildExpirationQueryParams(options);
    var fullEndpoint = endpoint + queryParams;

    Logger.SendingRequest("PUT", fullEndpoint);

    HttpResponseMessage response;

    if (options?.Metadata != null)
    {
      // Use multipart/form-data when metadata is provided.
      using var content = new MultipartFormDataContent();
      content.Add(new StringContent(value), "value");
      content.Add(new StringContent(options.Metadata.Value.GetRawText()), "metadata");

      response = await HttpClient.PutAsync(fullEndpoint, content, cancellationToken);
    }
    else
    {
      // Simple text body for value-only writes.
      var content = new StringContent(value, Encoding.UTF8, "text/plain");
      response = await HttpClient.PutAsync(fullEndpoint, content, cancellationToken);
    }

    Logger.ReceivedResponse(response.StatusCode, response.RequestMessage?.RequestUri);

    if (!response.IsSuccessStatusCode)
    {
      var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
      throw new HttpRequestException(
        $"Cloudflare API request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}). Response Body: {responseBody}",
        null,
        response.StatusCode);
    }
  }

  /// <inheritdoc />
  public async Task WriteValueAsync(
    string namespaceId,
    string key,
    byte[] value,
    KvWriteOptions? options = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}/values/{EncodeKeyName(key)}";

    // Expiration is always passed via query params (even with multipart).
    var queryParams = BuildExpirationQueryParams(options);
    var fullEndpoint = endpoint + queryParams;

    Logger.SendingRequest("PUT", fullEndpoint);

    HttpResponseMessage response;

    if (options?.Metadata != null)
    {
      // Use multipart/form-data when metadata is provided.
      using var content = new MultipartFormDataContent();
      content.Add(new ByteArrayContent(value), "value");
      content.Add(new StringContent(options.Metadata.Value.GetRawText()), "metadata");

      response = await HttpClient.PutAsync(fullEndpoint, content, cancellationToken);
    }
    else
    {
      // Binary body for value-only writes.
      var content = new ByteArrayContent(value);
      content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
      response = await HttpClient.PutAsync(fullEndpoint, content, cancellationToken);
    }

    Logger.ReceivedResponse(response.StatusCode, response.RequestMessage?.RequestUri);

    if (!response.IsSuccessStatusCode)
    {
      var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
      throw new HttpRequestException(
        $"Cloudflare API request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}). Response Body: {responseBody}",
        null,
        response.StatusCode);
    }
  }

  /// <inheritdoc />
  public async Task DeleteValueAsync(
    string namespaceId,
    string key,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}/values/{EncodeKeyName(key)}";

    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  #endregion


  #region Bulk Operations

  /// <inheritdoc />
  public async Task<KvBulkWriteResult> BulkWriteAsync(
    string namespaceId,
    IEnumerable<KvBulkWriteItem> items,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}/bulk";

    // The API expects a JSON array of items directly.
    return await PutAsync<KvBulkWriteResult>(endpoint, items.ToList(), cancellationToken);
  }

  /// <inheritdoc />
  public async Task<KvBulkDeleteResult> BulkDeleteAsync(
    string namespaceId,
    IEnumerable<string> keys,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}/bulk/delete";

    // The API expects a simple JSON array of key names: ["key1", "key2"].
    // Use POST method (not DELETE) as per API documentation.
    return await PostAsync<KvBulkDeleteResult>(endpoint, keys.ToList(), cancellationToken);
  }

  /// <inheritdoc />
  public async Task<IReadOnlyDictionary<string, string?>> BulkGetAsync(
    string namespaceId,
    IEnumerable<string> keys,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}/bulk/get";
    var request = new KvBulkGetRequest(keys.ToList());

    // The bulk get endpoint uses camelCase for request body (withMetadata, not with_metadata).
    var jsonContent = JsonSerializer.Serialize(request, _camelCaseOptions);
    var result = await PostJsonAsync<KvBulkGetResult>(endpoint, jsonContent, cancellationToken);

    return result.Values;
  }

  /// <inheritdoc />
  public async Task<IReadOnlyDictionary<string, KvBulkGetItemWithMetadata?>> BulkGetWithMetadataAsync(
    string namespaceId,
    IEnumerable<string> keys,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/storage/kv/namespaces/{Uri.EscapeDataString(namespaceId)}/bulk/get";
    var request = new KvBulkGetRequest(keys.ToList(), WithMetadata: true);

    // The bulk get endpoint uses camelCase for request body (withMetadata, not with_metadata).
    var jsonContent = JsonSerializer.Serialize(request, _camelCaseOptions);
    var result = await PostJsonAsync<KvBulkGetResultWithMetadata>(endpoint, jsonContent, cancellationToken);

    return result.Values;
  }

  #endregion
}
