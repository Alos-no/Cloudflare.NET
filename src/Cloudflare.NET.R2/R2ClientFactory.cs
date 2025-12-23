namespace Cloudflare.NET.R2;

using System.Collections.Concurrent;
using Amazon.S3;
using Cloudflare.NET.Accounts.Models;
using Configuration;
using Core;
using Core.Internal;
using Core.Validation;
using Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
///   Factory that creates and caches <see cref="IR2Client" /> instances. Supports named configurations
///   for multi-account scenarios and jurisdiction-specific clients for accessing buckets in different
///   geographic regions.
/// </summary>
/// <remarks>
///   <para>
///     This factory caches created clients to leverage the thread-safe, singleton-friendly nature of
///     the AWS S3 client. Clients are cached by a composite key of <c>(name, jurisdiction)</c>.
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

  /// <summary>
  ///   Cache of created R2 clients by composite key (name, jurisdiction).
  ///   Empty string for name represents the default (unnamed) configuration.
  /// </summary>
  private readonly ConcurrentDictionary<(string Name, string Jurisdiction), IR2Client> _clientCache = new();

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

  #endregion


  #region IDisposable

  /// <summary>Disposes all cached R2 clients and their underlying S3 clients.</summary>
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


  #region IR2ClientFactory Implementation

  /// <inheritdoc />
  public IR2Client GetClient(string name)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(name);
    ObjectDisposedException.ThrowIf(_disposed, this);

    // Get the configured jurisdiction for this named client.
    var r2Settings   = _r2OptionsMonitor.Get(name);
    var jurisdiction = r2Settings.Jurisdiction;

    // Cache key uses the configured jurisdiction.
    var cacheKey = (name, jurisdiction.Value);

    return _clientCache.GetOrAdd(cacheKey, _ => CreateClientCore(name, jurisdiction));
  }


  /// <inheritdoc />
  public IR2Client GetClient(R2Jurisdiction jurisdiction)
  {
    ObjectDisposedException.ThrowIf(_disposed, this);

    // Empty string represents the default (unnamed) configuration.
    var cacheKey = (Name: string.Empty, Jurisdiction: jurisdiction.Value);

    return _clientCache.GetOrAdd(cacheKey, _ => CreateClientForDefaultWithJurisdiction(jurisdiction));
  }


  /// <inheritdoc />
  public IR2Client GetClient(string name, R2Jurisdiction jurisdiction)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(name);
    ObjectDisposedException.ThrowIf(_disposed, this);

    var cacheKey = (name, jurisdiction.Value);

    return _clientCache.GetOrAdd(cacheKey, _ => CreateClientCore(name, jurisdiction));
  }

  #endregion


  #region Private Methods

  /// <summary>
  ///   Creates a new R2 client instance for the default (unnamed) configuration with a jurisdiction override.
  /// </summary>
  /// <param name="jurisdiction">The jurisdiction to use for the S3 endpoint.</param>
  /// <returns>A new <see cref="IR2Client" /> instance.</returns>
  /// <exception cref="CloudflareR2ConfigurationException">
  ///   Thrown when required configuration is missing or invalid.
  /// </exception>
  private IR2Client CreateClientForDefaultWithJurisdiction(R2Jurisdiction jurisdiction)
  {
    // Use the default (unnamed) options - CurrentValue gets the unnamed instance.
    var r2Settings         = _r2OptionsMonitor.CurrentValue;
    var cloudflareSettings = _cloudflareOptionsMonitor.CurrentValue;

    // Validate configuration.
    ValidateConfiguration(string.Empty, r2Settings, cloudflareSettings);

    // Construct the endpoint URL for the specified jurisdiction.
    var endpointUrl = R2Settings.GetEndpointUrlForJurisdiction(cloudflareSettings.AccountId, jurisdiction);

    return CreateS3ClientAndR2Client(r2Settings, endpointUrl);
  }


  /// <summary>
  ///   Creates a new R2 client instance for a named configuration with an optional jurisdiction override.
  /// </summary>
  /// <param name="name">The name of the client configuration.</param>
  /// <param name="jurisdiction">The jurisdiction to use for the S3 endpoint.</param>
  /// <returns>A new <see cref="IR2Client" /> instance.</returns>
  /// <exception cref="CloudflareR2ConfigurationException">
  ///   Thrown when required configuration is missing or invalid.
  /// </exception>
  private IR2Client CreateClientCore(string name, R2Jurisdiction jurisdiction)
  {
    // Retrieve the named R2 settings.
    var r2Settings = _r2OptionsMonitor.Get(name);

    // Retrieve the named Cloudflare options (for Account ID).
    var cloudflareSettings = _cloudflareOptionsMonitor.Get(name);

    // Validate configuration.
    ValidateConfiguration(name, r2Settings, cloudflareSettings);

    // Construct the endpoint URL for the specified jurisdiction.
    var endpointUrl = R2Settings.GetEndpointUrlForJurisdiction(cloudflareSettings.AccountId, jurisdiction);

    return CreateS3ClientAndR2Client(r2Settings, endpointUrl);
  }


  /// <summary>
  ///   Creates the underlying S3 client and wraps it in an R2 client.
  /// </summary>
  /// <param name="r2Settings">The R2 settings containing credentials.</param>
  /// <param name="endpointUrl">The fully-formed S3 endpoint URL.</param>
  /// <returns>A new <see cref="IR2Client" /> instance.</returns>
  private IR2Client CreateS3ClientAndR2Client(R2Settings r2Settings, string endpointUrl)
  {
    var config = new AmazonS3Config
    {
      ServiceURL           = endpointUrl,
      ForcePathStyle       = true,
      AuthenticationRegion = r2Settings.Region
    };

    var s3Client = new AmazonS3Client(r2Settings.AccessKeyId, r2Settings.SecretAccessKey, config);

    return new R2Client(_loggerFactory, s3Client);
  }


  /// <summary>
  ///   Validates the configuration for an R2 client and throws a descriptive exception if invalid.
  /// </summary>
  /// <param name="name">The name of the client configuration (empty string for default).</param>
  /// <param name="r2Settings">The R2 settings to validate.</param>
  /// <param name="cloudflareSettings">The Cloudflare API settings to validate.</param>
  /// <exception cref="CloudflareR2ConfigurationException">
  ///   Thrown when any required configuration is missing or invalid.
  /// </exception>
  private static void ValidateConfiguration(string               name,
                                            R2Settings           r2Settings,
                                            CloudflareApiOptions cloudflareSettings)
  {
    var errors      = new List<string>();
    var displayName = string.IsNullOrEmpty(name) ? "default" : name;

    // Validate R2 settings.
    var r2Result = R2SettingsValidator.ValidateConfiguration(name, r2Settings);

    if (r2Result.Failed)
      errors.AddRange(r2Result.Failures);

    // Validate Cloudflare settings (Account ID required for R2).
    var cfResult = CloudflareApiOptionsValidator.ValidateConfiguration(
      name, cloudflareSettings, CloudflareValidationRequirements.ForR2);

    if (cfResult.Failed)
      errors.AddRange(cfResult.Failures);

    if (errors.Count > 0)
    {
      var message = errors.Count == 1
        ? $"Cloudflare R2 configuration error: {errors[0]}"
        : $"Cloudflare R2 configuration errors for '{displayName}' client:\n- " + string.Join("\n- ", errors);

      throw new CloudflareR2ConfigurationException(message);
    }
  }

  #endregion
}
