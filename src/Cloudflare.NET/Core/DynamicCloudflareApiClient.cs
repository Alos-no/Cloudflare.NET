namespace Cloudflare.NET.Core;

using Accounts;
using ApiTokens;
using AuditLogs;
using Dns;
using Members;
using Roles;
using Subscriptions;
using Turnstile;
using User;
using Workers;
using Zones;

/// <summary>
///   A Cloudflare API client created dynamically (at runtime) that owns its
///   <see cref="HttpClient" /> and disposes it when disposed.
/// </summary>
/// <remarks>
///   <para>
///     Unlike DI-managed clients where the <see cref="HttpClient" /> lifetime is managed by
///     <see cref="IHttpClientFactory" />, dynamic clients own their <see cref="HttpClient" />
///     and must dispose it to release the underlying <see cref="System.Net.Http.SocketsHttpHandler" />
///     and connections.
///   </para>
///   <para>
///     This class wraps a standard <see cref="CloudflareApiClient" /> and adds disposal semantics.
///     All API operations are delegated to the inner client.
///   </para>
///   <para>
///     Users should dispose this client when it is no longer needed:
///   </para>
///   <code>
/// using var client = factory.CreateClient(options);
/// // Use the client...
/// </code>
/// </remarks>
internal sealed class DynamicCloudflareApiClient : ICloudflareApiClient
{
  #region Properties & Fields - Non-Public

  /// <summary>The inner Cloudflare API client that handles all API operations.</summary>
  private readonly CloudflareApiClient _innerClient;

  /// <summary>The HttpClient that this instance owns and will dispose.</summary>
  private readonly HttpClient _ownedHttpClient;

  /// <summary>Indicates whether this instance has been disposed.</summary>
  private bool _disposed;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="DynamicCloudflareApiClient" /> class.
  /// </summary>
  /// <param name="innerClient">The inner Cloudflare API client.</param>
  /// <param name="ownedHttpClient">The HttpClient that this instance owns and will dispose.</param>
  internal DynamicCloudflareApiClient(CloudflareApiClient innerClient, HttpClient ownedHttpClient)
  {
    _innerClient     = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
    _ownedHttpClient = ownedHttpClient ?? throw new ArgumentNullException(nameof(ownedHttpClient));
  }

  #endregion


  #region Properties Impl - ICloudflareApiClient

  /// <inheritdoc />
  public IAccountsApi Accounts => ThrowIfDisposed()._innerClient.Accounts;

  /// <inheritdoc />
  public IUserApi User => ThrowIfDisposed()._innerClient.User;

  /// <inheritdoc />
  public IZonesApi Zones => ThrowIfDisposed()._innerClient.Zones;

  /// <inheritdoc />
  public IDnsApi Dns => ThrowIfDisposed()._innerClient.Dns;

  /// <inheritdoc />
  public IAuditLogsApi AuditLogs => ThrowIfDisposed()._innerClient.AuditLogs;

  /// <inheritdoc />
  public IApiTokensApi ApiTokens => ThrowIfDisposed()._innerClient.ApiTokens;

  /// <inheritdoc />
  public IRolesApi Roles => ThrowIfDisposed()._innerClient.Roles;

  /// <inheritdoc />
  public IMembersApi Members => ThrowIfDisposed()._innerClient.Members;

  /// <inheritdoc />
  public ISubscriptionsApi Subscriptions => ThrowIfDisposed()._innerClient.Subscriptions;

  /// <inheritdoc />
  public IWorkersApi Workers => ThrowIfDisposed()._innerClient.Workers;

  /// <inheritdoc />
  public ITurnstileApi Turnstile => ThrowIfDisposed()._innerClient.Turnstile;

  #endregion


  #region Methods Impl - IDisposable

  /// <summary>
  ///   Releases the resources used by this client, including the owned <see cref="HttpClient" />.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     After disposal, any attempt to access the API properties will throw
  ///     <see cref="ObjectDisposedException" />.
  ///   </para>
  /// </remarks>
  public void Dispose()
  {
    if (_disposed)
      return;

    _ownedHttpClient.Dispose();
    _disposed = true;
  }

  #endregion


  #region Methods - Private

  /// <summary>
  ///   Throws <see cref="ObjectDisposedException" /> if this instance has been disposed.
  /// </summary>
  /// <returns>This instance, for fluent chaining.</returns>
  /// <exception cref="ObjectDisposedException">Thrown if this instance has been disposed.</exception>
  private DynamicCloudflareApiClient ThrowIfDisposed()
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    return this;
  }

  #endregion
}
