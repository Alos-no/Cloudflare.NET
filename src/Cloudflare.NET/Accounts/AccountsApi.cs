namespace Cloudflare.NET.Accounts;

using Core;
using Microsoft.Extensions.Options;
using Models;

/// <summary>Implements the API for managing Cloudflare Account resources.</summary>
/// <remarks>Initializes a new instance of the <see cref="AccountsApi" /> class.</remarks>
/// <param name="httpClient">The HttpClient for making requests.</param>
/// <param name="options">The Cloudflare API options.</param>
public class AccountsApi(HttpClient httpClient, IOptions<CloudflareApiOptions> options)
  : ApiResource(httpClient), IAccountsApi
{
  #region Properties & Fields - Non-Public

  /// <summary>The Cloudflare Account ID.</summary>
  private readonly string _accountId = options.Value.AccountId;

  #endregion

  #region Methods Impl

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
  public async Task<CreateBucketResponse> CreateR2BucketAsync(string bucketName, CancellationToken cancellationToken = default)
  {
    var requestBody = new CreateBucketRequest(bucketName);
    var endpoint    = $"accounts/{_accountId}/r2/buckets";
    return await PostAsync<CreateBucketResponse>(endpoint, requestBody, cancellationToken);
  }

  #endregion
}
