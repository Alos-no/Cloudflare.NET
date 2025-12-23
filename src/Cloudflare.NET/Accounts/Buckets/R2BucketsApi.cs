namespace Cloudflare.NET.Accounts.Buckets;

using Core;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;

/// <summary>Implements the API for managing Cloudflare R2 bucket resources.</summary>
public class R2BucketsApi : ApiResource, IR2BucketsApi
{
  #region Constants

  /// <summary>The HTTP header name for specifying R2 jurisdiction.</summary>
  private const string JurisdictionHeaderName = "cf-r2-jurisdiction";

  #endregion


  #region Properties & Fields - Non-Public

  /// <summary>The Cloudflare Account ID.</summary>
  private readonly string _accountId;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="R2BucketsApi" /> class.</summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  /// <param name="options">The Cloudflare API options containing the account ID.</param>
  /// <param name="loggerFactory">The factory to create loggers.</param>
  public R2BucketsApi(HttpClient httpClient, IOptions<CloudflareApiOptions> options, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<R2BucketsApi>())
  {
    _accountId = options.Value.AccountId;
  }

  #endregion


  #region Helper Methods

  /// <summary>Builds jurisdiction headers for R2 API requests.</summary>
  /// <param name="jurisdiction">The optional jurisdiction restriction.</param>
  /// <returns>Headers collection if jurisdiction is specified, otherwise null.</returns>
  private static IEnumerable<KeyValuePair<string, string>>? BuildJurisdictionHeaders(R2Jurisdiction? jurisdiction)
  {
    return jurisdiction is { } j
      ? [new KeyValuePair<string, string>(JurisdictionHeaderName, j.Value)]
      : null;
  }

  #endregion


  #region Core Bucket Operations

  /// <inheritdoc />
  public async Task<R2Bucket> CreateAsync(
    string            bucketName,
    R2LocationHint?   locationHint      = null,
    R2Jurisdiction?   jurisdiction      = null,
    R2StorageClass?   storageClass      = null,
    CancellationToken cancellationToken = default)
  {
    var requestBody = new CreateBucketRequest(bucketName, locationHint, storageClass);
    var endpoint    = $"accounts/{_accountId}/r2/buckets";
    var headers     = BuildJurisdictionHeaders(jurisdiction);

    return await PostAsync<R2Bucket>(endpoint, requestBody, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<R2Bucket> GetAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    return await GetAsync<R2Bucket>(endpoint, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<CursorPaginatedResult<R2Bucket>> ListAsync(
    ListR2BucketsFilters? filters           = null,
    R2Jurisdiction?       jurisdiction      = null,
    CancellationToken     cancellationToken = default)
  {
    var queryParams = new List<string>();

    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");

    if (!string.IsNullOrEmpty(filters?.Cursor))
      queryParams.Add($"cursor={Uri.EscapeDataString(filters.Cursor)}");

    if (!string.IsNullOrEmpty(filters?.NameContains))
      queryParams.Add($"name_contains={Uri.EscapeDataString(filters.NameContains)}");

    if (!string.IsNullOrEmpty(filters?.Order))
      queryParams.Add($"order={Uri.EscapeDataString(filters.Order)}");

    if (!string.IsNullOrEmpty(filters?.Direction))
      queryParams.Add($"direction={Uri.EscapeDataString(filters.Direction)}");

    if (!string.IsNullOrEmpty(filters?.StartAfter))
      queryParams.Add($"start_after={Uri.EscapeDataString(filters.StartAfter)}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;
    var endpoint    = $"accounts/{_accountId}/r2/buckets{queryString}";
    var headers     = BuildJurisdictionHeaders(jurisdiction);

    return await GetCursorPaginatedResultAsync<ListR2BucketsResponse, R2Bucket>(
      endpoint,
      response => response.Buckets,
      headers,
      cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<R2Bucket> ListAllAsync(
    ListR2BucketsFilters? filters           = null,
    R2Jurisdiction?       jurisdiction      = null,
    CancellationToken     cancellationToken = default)
  {
    // Build the base URL with filter parameters (except cursor and per_page, which are handled by pagination).
    var queryParams = new List<string>();

    if (!string.IsNullOrEmpty(filters?.NameContains))
      queryParams.Add($"name_contains={Uri.EscapeDataString(filters.NameContains)}");

    if (!string.IsNullOrEmpty(filters?.Order))
      queryParams.Add($"order={Uri.EscapeDataString(filters.Order)}");

    if (!string.IsNullOrEmpty(filters?.Direction))
      queryParams.Add($"direction={Uri.EscapeDataString(filters.Direction)}");

    if (!string.IsNullOrEmpty(filters?.StartAfter))
      queryParams.Add($"start_after={Uri.EscapeDataString(filters.StartAfter)}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;
    var endpoint    = $"accounts/{_accountId}/r2/buckets{queryString}";
    var headers     = BuildJurisdictionHeaders(jurisdiction);

    return GetCursorPaginatedAsync<ListR2BucketsResponse, R2Bucket>(
      endpoint,
      filters?.PerPage,
      response => response.Buckets,
      headers,
      cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    await DeleteAsync<object>(endpoint, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<R2Bucket> UpdateAsync(
    string            bucketName,
    R2StorageClass    storageClass,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}";

    // Build headers: cf-r2-storage-class follows the same naming pattern as cf-r2-jurisdiction.
    var headers = new List<KeyValuePair<string, string>>
    {
      new("cf-r2-storage-class", storageClass.Value)
    };

    // Add jurisdiction header if specified.
    if (jurisdiction is { } j)
    {
      headers.Add(new KeyValuePair<string, string>(JurisdictionHeaderName, j.Value));
    }

    // PATCH with empty body; the storage_class is passed via header.
    return await PatchAsync<R2Bucket>(endpoint, new { }, headers, cancellationToken);
  }

  #endregion


  #region CORS Configuration

  /// <inheritdoc />
  public async Task<BucketCorsPolicy> GetCorsAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/cors";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    return await GetAsync<BucketCorsPolicy>(endpoint, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task SetCorsAsync(
    string            bucketName,
    BucketCorsPolicy  corsPolicy,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/cors";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    await PutAsync<object>(endpoint, corsPolicy, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteCorsAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/cors";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    await DeleteAsync<object>(endpoint, headers, cancellationToken);
  }

  #endregion


  #region Lifecycle Configuration

  /// <inheritdoc />
  public async Task<BucketLifecyclePolicy> GetLifecycleAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/lifecycle";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    return await GetAsync<BucketLifecyclePolicy>(endpoint, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task SetLifecycleAsync(
    string                bucketName,
    BucketLifecyclePolicy lifecyclePolicy,
    R2Jurisdiction?       jurisdiction      = null,
    CancellationToken     cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/lifecycle";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    // Cloudflare R2 API requires the 'conditions' field to be present in each rule, even if empty.
    // If conditions is null, we normalize it to an empty LifecycleRuleConditions object.
    // Without this normalization, the API returns error 10040 "The JSON you provided was not well formed."
    var normalizedRules = lifecyclePolicy.Rules.Select(rule =>
      rule.Conditions is null
        ? rule with { Conditions = new LifecycleRuleConditions() }
        : rule
    ).ToArray();

    var normalizedPolicy = new BucketLifecyclePolicy(normalizedRules);

    // Use custom serialization options that match Cloudflare R2 lifecycle API expectations (camelCase for lifecycle).
    // Note: The R2 lifecycle API uses camelCase property names, unlike most Cloudflare APIs which use snake_case.
    var lifecycleSerializerOptions = new System.Text.Json.JsonSerializerOptions
    {
      DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    var jsonContent = System.Text.Json.JsonSerializer.Serialize(normalizedPolicy, lifecycleSerializerOptions);

    await PutJsonAsync<object?>(endpoint, jsonContent, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteLifecycleAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    // Cloudflare R2 does not have a DELETE endpoint for lifecycle policies.
    // To remove the lifecycle policy, we PUT an empty rules array.
    var emptyPolicy = new BucketLifecyclePolicy(Array.Empty<LifecycleRule>());

    await SetLifecycleAsync(bucketName, emptyPolicy, jurisdiction, cancellationToken);
  }

  #endregion


  #region Custom Domain Configuration

  /// <inheritdoc />
  public async Task<IReadOnlyList<CustomDomain>> ListCustomDomainsAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/domains/custom";
    var headers  = BuildJurisdictionHeaders(jurisdiction);
    var response = await GetAsync<ListCustomDomainsResponse>(endpoint, headers, cancellationToken);

    return response.Domains;
  }

  /// <inheritdoc />
  public async Task<CustomDomainResponse> AttachCustomDomainAsync(
    string            bucketName,
    string            hostname,
    string            zoneId,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var requestBody = new AttachCustomDomainRequest(hostname, true, zoneId);
    var endpoint    = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/domains/custom";
    var headers     = BuildJurisdictionHeaders(jurisdiction);

    return await PostAsync<CustomDomainResponse>(endpoint, requestBody, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<CustomDomainResponse> GetCustomDomainStatusAsync(
    string            bucketName,
    string            hostname,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/domains/custom/{Uri.EscapeDataString(hostname)}";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    return await GetAsync<CustomDomainResponse>(endpoint, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<CustomDomainResponse> UpdateCustomDomainAsync(
    string                    bucketName,
    string                    hostname,
    UpdateCustomDomainRequest request,
    R2Jurisdiction?           jurisdiction      = null,
    CancellationToken         cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/domains/custom/{Uri.EscapeDataString(hostname)}";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    return await PutAsync<CustomDomainResponse>(endpoint, request, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DetachCustomDomainAsync(
    string            bucketName,
    string            hostname,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/domains/custom/{Uri.EscapeDataString(hostname)}";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    await DeleteAsync<object>(endpoint, headers, cancellationToken);
  }

  #endregion


  #region Managed Domain (r2.dev) Configuration

  /// <inheritdoc />
  public async Task<ManagedDomainResponse> GetManagedDomainAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/domains/managed";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    return await GetAsync<ManagedDomainResponse>(endpoint, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<ManagedDomainResponse> EnableManagedDomainAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var requestBody = new SetManagedDomainRequest(true);
    var endpoint    = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/domains/managed";
    var headers     = BuildJurisdictionHeaders(jurisdiction);

    return await PutAsync<ManagedDomainResponse>(endpoint, requestBody, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DisableManagedDomainAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var requestBody = new SetManagedDomainRequest(false);
    var endpoint    = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/domains/managed";
    var headers     = BuildJurisdictionHeaders(jurisdiction);

    // We don't care about the result body, just success.
    await PutAsync<object>(endpoint, requestBody, headers, cancellationToken);
  }

  #endregion


  #region Bucket Lock Configuration

  /// <inheritdoc />
  public async Task<BucketLockPolicy> GetLockAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/lock";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    return await GetAsync<BucketLockPolicy>(endpoint, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<BucketLockPolicy> SetLockAsync(
    string            bucketName,
    BucketLockPolicy  lockPolicy,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/lock";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    return await PutAsync<BucketLockPolicy>(endpoint, lockPolicy, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteLockAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    // Similar to lifecycle, we remove lock rules by setting an empty rules array.
    var emptyPolicy = new BucketLockPolicy(Array.Empty<BucketLockRule>());

    await SetLockAsync(bucketName, emptyPolicy, jurisdiction, cancellationToken);
  }

  #endregion


  #region Sippy (Incremental Migration) Configuration

  /// <inheritdoc />
  public async Task<SippyConfig> GetSippyAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/sippy";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    return await GetAsync<SippyConfig>(endpoint, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<SippyConfig> EnableSippyAsync(
    string             bucketName,
    EnableSippyRequest request,
    R2Jurisdiction?    jurisdiction      = null,
    CancellationToken  cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/sippy";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    // The request contains the source configuration directly.
    return await PutAsync<SippyConfig>(endpoint, request.SourceConfig, headers, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DisableSippyAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{Uri.EscapeDataString(bucketName)}/sippy";
    var headers  = BuildJurisdictionHeaders(jurisdiction);

    await DeleteAsync<object>(endpoint, headers, cancellationToken);
  }

  #endregion


  #region Temporary Credentials

  /// <inheritdoc />
  public async Task<TempCredentials> CreateTempCredentialsAsync(
    CreateTempCredentialsRequest request,
    CancellationToken            cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/temp-access-credentials";

    return await PostAsync<TempCredentials>(endpoint, request, cancellationToken);
  }

  #endregion
}
