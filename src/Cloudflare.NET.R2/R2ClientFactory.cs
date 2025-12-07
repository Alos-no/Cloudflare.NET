namespace Cloudflare.NET.R2;

using System.Collections.Concurrent;
using Amazon.S3;
using Configuration;
using Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
///   Factory that creates named <see cref="IR2Client" /> instances. Each named client is configured with its own
///   <see cref="R2Settings" /> and uses a dedicated <see cref="AmazonS3Client" /> with appropriate credentials.
/// </summary>
/// <remarks>
///   <para>
///     This factory caches created clients to avoid the overhead of creating new S3 clients on every request. Clients
///     are cached by name and reused for subsequent requests.
///   </para>
///   <para>
///     Named clients are registered using
///     <see
///       cref="ServiceCollectionExtensions.AddCloudflareR2Client(Microsoft.Extensions.DependencyInjection.IServiceCollection, string, System.Action{R2Settings})" />
///     .
///   </para>
/// </remarks>
public sealed class R2ClientFactory : IR2ClientFactory, IDisposable
{
  #region Properties & Fields - Non-Public

  /// <summary>Cache of created R2 clients by name.</summary>
  private readonly ConcurrentDictionary<string, IR2Client> _clientCache = new();

  /// <summary>The options monitor for Cloudflare API options (contains Account ID).</summary>
  private readonly IOptionsMonitor<CloudflareApiOptions> _cloudflareOptionsMonitor;

  /// <summary>The logger factory for creating loggers.</summary>
  private readonly ILoggerFactory _loggerFactory;

  /// <summary>The options monitor for R2 settings.</summary>
  private readonly IOptionsMonitor<R2Settings> _r2OptionsMonitor;

  /// <summary>Flag to track if the factory has been disposed.</summary>
  private bool _disposed;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="R2ClientFactory" /> class.</summary>
  /// <param name="r2OptionsMonitor">The options monitor for named R2 settings.</param>
  /// <param name="cloudflareOptionsMonitor">The options monitor for named Cloudflare API options.</param>
  /// <param name="loggerFactory">The logger factory.</param>
  public R2ClientFactory(IOptionsMonitor<R2Settings>           r2OptionsMonitor,
                         IOptionsMonitor<CloudflareApiOptions> cloudflareOptionsMonitor,
                         ILoggerFactory                        loggerFactory)
  {
    _r2OptionsMonitor         = r2OptionsMonitor;
    _cloudflareOptionsMonitor = cloudflareOptionsMonitor;
    _loggerFactory            = loggerFactory;
  }


  /// <summary>Disposes all cached R2 clients.</summary>
  public void Dispose()
  {
    if (_disposed)
      return;

    _disposed = true;

    // Dispose all cached clients.
    foreach (var client in _clientCache.Values)
      if (client is IDisposable disposable)
        disposable.Dispose();

    _clientCache.Clear();
  }

  #endregion

  #region Methods Impl

  /// <inheritdoc />
  public IR2Client CreateClient(string name)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ObjectDisposedException.ThrowIf(_disposed, this);

    // Use GetOrAdd to ensure thread-safe creation of clients.
    return _clientCache.GetOrAdd(name, CreateClientCore);
  }

  #endregion

  #region Methods

  /// <summary>Creates a new R2 client instance for the given name.</summary>
  /// <param name="name">The name of the client configuration.</param>
  /// <returns>A new <see cref="IR2Client" /> instance.</returns>
  private IR2Client CreateClientCore(string name)
  {
    // Retrieve the named R2 settings.
    var r2Settings = _r2OptionsMonitor.Get(name);

    // Validate that the R2 settings are configured.
    if (string.IsNullOrWhiteSpace(r2Settings.AccessKeyId))
      throw new InvalidOperationException(
        $"No R2 client configuration found for name '{name}'. " +
        $"Ensure AddCloudflareR2Client(\"{name}\", ...) has been called during service registration.");

    // Retrieve the named Cloudflare options (for Account ID).
    var cloudflareSettings = _cloudflareOptionsMonitor.Get(name);

    if (string.IsNullOrWhiteSpace(cloudflareSettings.AccountId))
      throw new InvalidOperationException(
        $"Cloudflare Account ID is missing for named client '{name}'. " +
        $"Ensure the Account ID is configured in the Cloudflare options for this named client.");

    // Construct the R2 endpoint URL using the Account ID.
    var endpointUrl = string.Format(r2Settings.EndpointUrl, cloudflareSettings.AccountId);

    // Create the S3 client configuration.
    var config = new AmazonS3Config
    {
      ServiceURL           = endpointUrl,
      ForcePathStyle       = true,
      AuthenticationRegion = r2Settings.Region
    };

    // Create the S3 client with the named credentials.
    var s3Client = new AmazonS3Client(r2Settings.AccessKeyId, r2Settings.SecretAccessKey, config);

    // Create and return the R2 client.
    return new R2Client(_loggerFactory, s3Client);
  }

  #endregion
}
