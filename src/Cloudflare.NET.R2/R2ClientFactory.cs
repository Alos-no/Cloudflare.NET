namespace Cloudflare.NET.R2;

using System.Collections.Concurrent;
using Amazon.S3;
using Configuration;
using Core;
using Core.Internal;
using Core.Validation;
using Exceptions;
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
    ThrowHelper.ThrowIfNullOrWhiteSpace(name);

#if NET7_0_OR_GREATER
    ObjectDisposedException.ThrowIf(_disposed, this);
#else
    if (_disposed)
      throw new ObjectDisposedException(nameof(R2ClientFactory));
#endif

    // Use GetOrAdd to ensure thread-safe creation of clients.
    return _clientCache.GetOrAdd(name, CreateClientCore);
  }

  #endregion

  #region Methods

  /// <summary>Creates a new R2 client instance for the given name.</summary>
  /// <param name="name">The name of the client configuration.</param>
  /// <returns>A new <see cref="IR2Client" /> instance.</returns>
  /// <exception cref="CloudflareR2ConfigurationException">
  ///   Thrown when required configuration is missing or invalid for the
  ///   named client.
  /// </exception>
  private IR2Client CreateClientCore(string name)
  {
    // Retrieve the named R2 settings.
    var r2Settings = _r2OptionsMonitor.Get(name);

    // Retrieve the named Cloudflare options (for Account ID).
    var cloudflareSettings = _cloudflareOptionsMonitor.Get(name);

    // Validate configuration and collect all errors for a comprehensive error message.
    ValidateNamedClientConfiguration(name, r2Settings, cloudflareSettings);

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


  /// <summary>Validates the configuration for a named R2 client and throws a descriptive exception if invalid.</summary>
  /// <param name="name">The name of the client configuration being validated.</param>
  /// <param name="r2Settings">The R2 settings to validate.</param>
  /// <param name="cloudflareSettings">The Cloudflare API settings to validate (for Account ID).</param>
  /// <exception cref="CloudflareR2ConfigurationException">Thrown when any required configuration is missing or invalid.</exception>
  private static void ValidateNamedClientConfiguration(string               name,
                                                       R2Settings           r2Settings,
                                                       CloudflareApiOptions cloudflareSettings)
  {
    var errors = new List<string>();

    // Validate R2 settings using the static validation method for consistent error messages.
    var r2Result = R2SettingsValidator.ValidateConfiguration(name, r2Settings);

    if (r2Result.Failed)
      errors.AddRange(r2Result.Failures);

    // Validate Cloudflare settings using the static validation method with R2-specific requirements.
    var cfResult = CloudflareApiOptionsValidator.ValidateConfiguration(
      name, cloudflareSettings, CloudflareValidationRequirements.ForR2);

    if (cfResult.Failed)
      errors.AddRange(cfResult.Failures);

    // Throw a comprehensive exception if any validation errors occurred.
    if (errors.Count > 0)
    {
      var message = errors.Count == 1
        ? $"Cloudflare R2 configuration error: {errors[0]}"
        : $"Cloudflare R2 configuration errors for named client '{name}':\n- " + string.Join("\n- ", errors);

      throw new CloudflareR2ConfigurationException(message);
    }
  }

  #endregion
}
