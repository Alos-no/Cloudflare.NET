namespace Cloudflare.NET.Accounts;

using AccessRules;
using Buckets;
using Core;
using Core.Internal;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Rulesets;

/// <summary>Implements the API for managing Cloudflare Account resources.</summary>
public class AccountsApi : ApiResource, IAccountsApi
{
  #region Properties & Fields - Non-Public

  /// <summary>The lazy-initialized R2 Buckets API resource.</summary>
  private readonly Lazy<IR2BucketsApi> _buckets;

  /// <summary>The lazy-initialized Account Access Rules API resource.</summary>
  private readonly Lazy<IAccountAccessRulesApi> _accessRules;

  /// <summary>The lazy-initialized Account Rulesets API resource.</summary>
  private readonly Lazy<IAccountRulesetsApi> _rulesets;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AccountsApi" /> class.</summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  /// <param name="options">The Cloudflare API options.</param>
  /// <param name="loggerFactory">The factory to create loggers for this and child resources.</param>
  public AccountsApi(HttpClient httpClient, IOptions<CloudflareApiOptions> options, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<AccountsApi>())
  {
    _buckets     = new Lazy<IR2BucketsApi>(() => new R2BucketsApi(httpClient, options, loggerFactory));
    _accessRules = new Lazy<IAccountAccessRulesApi>(() => new AccountAccessRulesApi(httpClient, options, loggerFactory));
    _rulesets    = new Lazy<IAccountRulesetsApi>(() => new AccountRulesetsApi(httpClient, options, loggerFactory));
  }

  #endregion


  #region Properties Impl - Public

  /// <inheritdoc />
  public IR2BucketsApi Buckets => _buckets.Value;

  /// <inheritdoc />
  public IAccountAccessRulesApi AccessRules => _accessRules.Value;

  /// <inheritdoc />
  public IAccountRulesetsApi Rulesets => _rulesets.Value;

  #endregion

  #region Methods Impl - Legacy Bucket Operations (Delegating to Buckets API)

  /// <inheritdoc />
  [Obsolete("Use Buckets.ListAsync instead. This method will be removed in a future version.")]
  public Task<CursorPaginatedResult<R2Bucket>> ListR2BucketsAsync(
    ListR2BucketsFilters? filters           = null,
    CancellationToken     cancellationToken = default) =>
    Buckets.ListAsync(filters, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.ListAllAsync instead. This method will be removed in a future version.")]
  public IAsyncEnumerable<R2Bucket> ListAllR2BucketsAsync(
    ListR2BucketsFilters? filters           = null,
    CancellationToken     cancellationToken = default) =>
    Buckets.ListAllAsync(filters, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.DisableManagedDomainAsync instead. This method will be removed in a future version.")]
  public Task DisableDevUrlAsync(string bucketName, CancellationToken cancellationToken = default) =>
    Buckets.DisableManagedDomainAsync(bucketName, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.AttachCustomDomainAsync instead. This method will be removed in a future version.")]
  public Task<CustomDomainResponse> AttachCustomDomainAsync(
    string            bucketName,
    string            hostname,
    string            zoneId,
    CancellationToken cancellationToken = default) =>
    Buckets.AttachCustomDomainAsync(bucketName, hostname, zoneId, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.GetCustomDomainStatusAsync instead. This method will be removed in a future version.")]
  public Task<CustomDomainResponse> GetCustomDomainStatusAsync(
    string            bucketName,
    string            hostname,
    CancellationToken cancellationToken = default) =>
    Buckets.GetCustomDomainStatusAsync(bucketName, hostname, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.DetachCustomDomainAsync instead. This method will be removed in a future version.")]
  public Task DetachCustomDomainAsync(
    string            bucketName,
    string            hostname,
    CancellationToken cancellationToken = default) =>
    Buckets.DetachCustomDomainAsync(bucketName, hostname, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.DeleteAsync instead. This method will be removed in a future version.")]
  public Task DeleteR2BucketAsync(string bucketName, CancellationToken cancellationToken = default) =>
    Buckets.DeleteAsync(bucketName, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.CreateAsync instead. This method will be removed in a future version.")]
  public Task<R2Bucket> CreateR2BucketAsync(
    string            bucketName,
    R2LocationHint?   locationHint      = null,
    R2Jurisdiction?   jurisdiction      = null,
    R2StorageClass?   storageClass      = null,
    CancellationToken cancellationToken = default) =>
    Buckets.CreateAsync(bucketName, locationHint, jurisdiction, storageClass, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.GetCorsAsync instead. This method will be removed in a future version.")]
  public Task<BucketCorsPolicy> GetBucketCorsAsync(
    string            bucketName,
    CancellationToken cancellationToken = default) =>
    Buckets.GetCorsAsync(bucketName, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.SetCorsAsync instead. This method will be removed in a future version.")]
  public Task SetBucketCorsAsync(
    string            bucketName,
    BucketCorsPolicy  corsPolicy,
    CancellationToken cancellationToken = default) =>
    Buckets.SetCorsAsync(bucketName, corsPolicy, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.DeleteCorsAsync instead. This method will be removed in a future version.")]
  public Task DeleteBucketCorsAsync(string bucketName, CancellationToken cancellationToken = default) =>
    Buckets.DeleteCorsAsync(bucketName, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.GetLifecycleAsync instead. This method will be removed in a future version.")]
  public Task<BucketLifecyclePolicy> GetBucketLifecycleAsync(
    string            bucketName,
    CancellationToken cancellationToken = default) =>
    Buckets.GetLifecycleAsync(bucketName, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.SetLifecycleAsync instead. This method will be removed in a future version.")]
  public Task SetBucketLifecycleAsync(
    string                bucketName,
    BucketLifecyclePolicy lifecyclePolicy,
    CancellationToken     cancellationToken = default) =>
    Buckets.SetLifecycleAsync(bucketName, lifecyclePolicy, cancellationToken);

  /// <inheritdoc />
  [Obsolete("Use Buckets.DeleteLifecycleAsync instead. This method will be removed in a future version.")]
  public Task DeleteBucketLifecycleAsync(string bucketName, CancellationToken cancellationToken = default) =>
    Buckets.DeleteLifecycleAsync(bucketName, cancellationToken);

  #endregion


  #region Account Management

  /// <inheritdoc />
  public async Task<PagePaginatedResult<Account>> ListAccountsAsync(
    ListAccountsFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = BuildAccountsListQueryString(filters);

    return await GetPagePaginatedResultAsync<Account>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<Account> ListAllAccountsAsync(
    ListAccountsFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    // Build the base query string without page parameters (pagination is handled by GetPaginatedAsync).
    var baseFilters = filters is not null ? filters with { Page = null, PerPage = null } : null;
    var endpoint = BuildAccountsListQueryString(baseFilters);

    return GetPaginatedAsync<Account>(endpoint, filters?.PerPage, cancellationToken);
  }

  /// <summary>
  ///   Builds the query string for the accounts list endpoint.
  /// </summary>
  /// <param name="filters">Optional filters to apply.</param>
  /// <returns>The endpoint with query string.</returns>
  private static string BuildAccountsListQueryString(ListAccountsFilters? filters)
  {
    var queryParams = new List<string>();

    if (filters?.Name is not null)
      queryParams.Add($"name={Uri.EscapeDataString(filters.Name)}");
    if (filters?.Page is not null)
      queryParams.Add($"page={filters.Page}");
    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");
    if (filters?.Direction is not null)
      queryParams.Add($"direction={EnumHelper.GetEnumMemberValue(filters.Direction.Value)}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    return $"accounts{queryString}";
  }

  /// <inheritdoc />
  public async Task<Account> GetAccountAsync(
    string accountId,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}";

    return await GetAsync<Account>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Account> CreateAccountAsync(
    CreateAccountRequest request,
    CancellationToken cancellationToken = default)
  {
    return await PostAsync<Account>("accounts", request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Account> UpdateAccountAsync(
    string accountId,
    UpdateAccountRequest request,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}";

    return await PutAsync<Account>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<DeleteAccountResult> DeleteAccountAsync(
    string accountId,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}";

    return await DeleteAsync<DeleteAccountResult>(endpoint, cancellationToken);
  }

  #endregion
}
