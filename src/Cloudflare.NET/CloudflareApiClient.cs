namespace Cloudflare.NET;

using Accounts;
using ApiTokens;
using AuditLogs;
using Core;
using Dns;
using Members;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Roles;
using Subscriptions;
using Turnstile;
using User;
using Workers;
using Zones;

/// <summary>The primary client for interacting with the Cloudflare API.</summary>
public sealed class CloudflareApiClient : ICloudflareApiClient
{
  #region Properties & Fields - Non-Public

  /// <summary>The lazy-initialized Accounts API resource.</summary>
  private readonly Lazy<IAccountsApi> _accounts;

  /// <summary>The lazy-initialized User API resource.</summary>
  private readonly Lazy<IUserApi> _user;

  /// <summary>The lazy-initialized Zones API resource.</summary>
  private readonly Lazy<IZonesApi> _zones;

  /// <summary>The lazy-initialized DNS API resource.</summary>
  private readonly Lazy<IDnsApi> _dns;

  /// <summary>The lazy-initialized Audit Logs API resource.</summary>
  private readonly Lazy<IAuditLogsApi> _auditLogs;

  /// <summary>The lazy-initialized API Tokens API resource.</summary>
  private readonly Lazy<IApiTokensApi> _apiTokens;

  /// <summary>The lazy-initialized Roles API resource.</summary>
  private readonly Lazy<IRolesApi> _roles;

  /// <summary>The lazy-initialized Members API resource.</summary>
  private readonly Lazy<IMembersApi> _members;

  /// <summary>The lazy-initialized Subscriptions API resource.</summary>
  private readonly Lazy<ISubscriptionsApi> _subscriptions;

  /// <summary>The lazy-initialized Workers API resource.</summary>
  private readonly Lazy<IWorkersApi> _workers;

  /// <summary>The lazy-initialized Turnstile API resource.</summary>
  private readonly Lazy<ITurnstileApi> _turnstile;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the Cloudflare API client.</summary>
  /// <param name="httpClient">The HttpClient to be used for all API calls.</param>
  /// <param name="options">The Cloudflare API options.</param>
  /// <param name="loggerFactory">The factory to create loggers for API resources.</param>
  public CloudflareApiClient(HttpClient httpClient, IOptions<CloudflareApiOptions> options, ILoggerFactory loggerFactory)
  {
    // Lazily initialize API resources to avoid unnecessary allocations until they are first accessed.
    _accounts  = new Lazy<IAccountsApi>(() => new AccountsApi(httpClient, options, loggerFactory));
    _user      = new Lazy<IUserApi>(() => new UserApi(httpClient, loggerFactory));
    _zones     = new Lazy<IZonesApi>(() => new ZonesApi(httpClient, loggerFactory));
    _dns       = new Lazy<IDnsApi>(() => new DnsApi(httpClient, loggerFactory));
    _auditLogs = new Lazy<IAuditLogsApi>(() => new AuditLogsApi(httpClient, loggerFactory));
    _apiTokens = new Lazy<IApiTokensApi>(() => new ApiTokensApi(httpClient, loggerFactory));
    _roles         = new Lazy<IRolesApi>(() => new RolesApi(httpClient, loggerFactory));
    _members       = new Lazy<IMembersApi>(() => new MembersApi(httpClient, loggerFactory));
    _subscriptions = new Lazy<ISubscriptionsApi>(() => new SubscriptionsApi(httpClient, loggerFactory));
    _workers       = new Lazy<IWorkersApi>(() => new WorkersApi(httpClient, loggerFactory));
    _turnstile     = new Lazy<ITurnstileApi>(() => new TurnstileApi(httpClient, loggerFactory));
  }

  #endregion

  #region Properties Impl - Public

  /// <inheritdoc />
  public IAccountsApi Accounts => _accounts.Value;

  /// <inheritdoc />
  public IUserApi User => _user.Value;

  /// <inheritdoc />
  public IZonesApi Zones => _zones.Value;

  /// <inheritdoc />
  public IDnsApi Dns => _dns.Value;

  /// <inheritdoc />
  public IAuditLogsApi AuditLogs => _auditLogs.Value;

  /// <inheritdoc />
  public IApiTokensApi ApiTokens => _apiTokens.Value;

  /// <inheritdoc />
  public IRolesApi Roles => _roles.Value;

  /// <inheritdoc />
  public IMembersApi Members => _members.Value;

  /// <inheritdoc />
  public ISubscriptionsApi Subscriptions => _subscriptions.Value;

  /// <inheritdoc />
  public IWorkersApi Workers => _workers.Value;

  /// <inheritdoc />
  public ITurnstileApi Turnstile => _turnstile.Value;

  #endregion


  #region Methods Impl - IDisposable

  /// <summary>
  ///   Disposes this client instance.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     For DI-managed clients, this method does nothing because the <see cref="HttpClient" />
  ///     lifetime is managed by <see cref="IHttpClientFactory" />. The factory handles pooling
  ///     and disposal of the underlying handlers automatically.
  ///   </para>
  ///   <para>
  ///     For dynamic clients created via
  ///     <see cref="ICloudflareApiClientFactory.CreateClient(CloudflareApiOptions)" />,
  ///     the <see cref="DynamicCloudflareApiClient" /> wrapper handles actual disposal of resources.
  ///   </para>
  /// </remarks>
  public void Dispose()
  {
    // No-op for DI-managed clients. The HttpClient is managed by IHttpClientFactory
    // and should not be disposed by the client. For dynamic clients, the
    // DynamicCloudflareApiClient wrapper handles disposal.
  }

  #endregion
}
