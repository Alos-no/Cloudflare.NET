﻿namespace Cloudflare.NET.Accounts;

using AccessRules;
using Core;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Rulesets;

/// <summary>Implements the API for managing Cloudflare Account resources.</summary>
public class AccountsApi : ApiResource, IAccountsApi
{
  #region Properties & Fields - Non-Public

  /// <summary>The Cloudflare Account ID.</summary>
  private readonly string _accountId;

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
    _accountId   = options.Value.AccountId;
    _accessRules = new Lazy<IAccountAccessRulesApi>(() => new AccountAccessRulesApi(httpClient, options, loggerFactory));
    _rulesets    = new Lazy<IAccountRulesetsApi>(() => new AccountRulesetsApi(httpClient, options, loggerFactory));
  }

  #endregion

  #region Properties Impl - Public

  /// <inheritdoc />
  public IAccountAccessRulesApi AccessRules => _accessRules.Value;

  /// <inheritdoc />
  public IAccountRulesetsApi Rulesets => _rulesets.Value;

  #endregion

  #region Methods Impl

  /// <inheritdoc />
  public async Task<CursorPaginatedResult<R2Bucket>> ListR2BucketsAsync(
    ListR2BucketsFilters? filters           = null,
    CancellationToken     cancellationToken = default)
  {
    var queryParams = new List<string>();

    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");
    if (!string.IsNullOrEmpty(filters?.Cursor))
      queryParams.Add($"cursor={filters.Cursor}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;
    var endpoint    = $"accounts/{_accountId}/r2/buckets{queryString}";

    return await GetCursorPaginatedResultAsync<ListR2BucketsResponse, R2Bucket>(
      endpoint,
      response => response.Buckets,
      cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<R2Bucket> ListAllR2BucketsAsync(ListR2BucketsFilters? filters           = null,
                                                          CancellationToken     cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets";
    return GetCursorPaginatedAsync<ListR2BucketsResponse, R2Bucket>(
      endpoint,
      filters?.PerPage,
      response => response.Buckets,
      cancellationToken);
  }

  /// <inheritdoc />
  public async Task DisableDevUrlAsync(string bucketName, CancellationToken cancellationToken = default)
  {
    var requestBody = new SetManagedDomainRequest(false);
    var endpoint    = $"accounts/{_accountId}/r2/buckets/{bucketName}/domains/managed";
    // We don't care about the result body, just success.
    await PutAsync<object>(endpoint, requestBody, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<CustomDomainResponse> AttachCustomDomainAsync(string            bucketName,
                                                                  string            hostname,
                                                                  string            zoneId,
                                                                  CancellationToken cancellationToken = default)
  {
    var requestBody = new AttachCustomDomainRequest(hostname, true, zoneId);
    var endpoint    = $"accounts/{_accountId}/r2/buckets/{bucketName}/domains/custom";
    return await PostAsync<CustomDomainResponse>(endpoint, requestBody, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<CustomDomainResponse> GetCustomDomainStatusAsync(string            bucketName,
                                                                     string            hostname,
                                                                     CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{bucketName}/domains/custom/{hostname}";
    return await GetAsync<CustomDomainResponse>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DetachCustomDomainAsync(string bucketName, string hostname, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{bucketName}/domains/custom/{hostname}";
    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteR2BucketAsync(string bucketName, CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/r2/buckets/{bucketName}";
    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<R2Bucket> CreateR2BucketAsync(string bucketName, CancellationToken cancellationToken = default)
  {
    var requestBody = new CreateBucketRequest(bucketName);
    var endpoint    = $"accounts/{_accountId}/r2/buckets";
    return await PostAsync<R2Bucket>(endpoint, requestBody, cancellationToken);
  }

  #endregion
}
