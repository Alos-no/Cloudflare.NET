namespace Cloudflare.NET;

using Accounts;
using Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zones;

/// <summary>The primary client for interacting with the Cloudflare API.</summary>
public sealed class CloudflareApiClient : ICloudflareApiClient
{
  #region Properties & Fields - Non-Public

  /// <summary>The lazy-initialized Accounts API resource.</summary>
  private readonly Lazy<IAccountsApi> _accounts;

  /// <summary>The lazy-initialized Zones API resource.</summary>
  private readonly Lazy<IZonesApi> _zones;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the Cloudflare API client.</summary>
  /// <param name="httpClient">The HttpClient to be used for all API calls.</param>
  /// <param name="options">The Cloudflare API options.</param>
  /// <param name="loggerFactory">The factory to create loggers for API resources.</param>
  public CloudflareApiClient(HttpClient httpClient, IOptions<CloudflareApiOptions> options, ILoggerFactory loggerFactory)
  {
    // Lazily initialize API resources to avoid unnecessary allocations until they are first accessed.
    _accounts = new Lazy<IAccountsApi>(() => new AccountsApi(httpClient, options, loggerFactory));
    _zones    = new Lazy<IZonesApi>(() => new ZonesApi(httpClient, loggerFactory));
  }

  #endregion

  #region Properties Impl - Public

  /// <inheritdoc />
  public IAccountsApi Accounts => _accounts.Value;

  /// <inheritdoc />
  public IZonesApi Zones => _zones.Value;

  #endregion
}
