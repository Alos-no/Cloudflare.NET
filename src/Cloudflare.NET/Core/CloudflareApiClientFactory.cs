namespace Cloudflare.NET.Core;

using Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
///   Factory that creates named <see cref="ICloudflareApiClient" /> instances. Each named client is configured with
///   its own <see cref="CloudflareApiOptions" /> and uses a dedicated <see cref="HttpClient" /> with appropriate
///   authentication and resilience.
/// </summary>
/// <remarks>
///   <para>
///     This factory is used to support multiple Cloudflare configurations within a single application, such as
///     connecting to different accounts or using different API tokens.
///   </para>
///   <para>
///     Named clients are registered using
///     <see
///       cref="ServiceCollectionExtensions.AddCloudflareApiClient(Microsoft.Extensions.DependencyInjection.IServiceCollection, string, System.Action{CloudflareApiOptions})" />
///     .
///   </para>
/// </remarks>
public sealed class CloudflareApiClientFactory : ICloudflareApiClientFactory
{
  #region Properties & Fields - Non-Public

  /// <summary>The HTTP client factory used to create named HttpClient instances.</summary>
  private readonly IHttpClientFactory _httpClientFactory;

  /// <summary>The logger factory for creating loggers for API resources.</summary>
  private readonly ILoggerFactory _loggerFactory;

  /// <summary>The options monitor for retrieving named Cloudflare API options.</summary>
  private readonly IOptionsMonitor<CloudflareApiOptions> _optionsMonitor;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="CloudflareApiClientFactory" /> class.</summary>
  /// <param name="httpClientFactory">The HTTP client factory.</param>
  /// <param name="optionsMonitor">The options monitor for named options.</param>
  /// <param name="loggerFactory">The logger factory.</param>
  public CloudflareApiClientFactory(IHttpClientFactory                    httpClientFactory,
                                    IOptionsMonitor<CloudflareApiOptions> optionsMonitor,
                                    ILoggerFactory                        loggerFactory)
  {
    _httpClientFactory = httpClientFactory;
    _optionsMonitor    = optionsMonitor;
    _loggerFactory     = loggerFactory;
  }

  #endregion

  #region Methods Impl

  /// <inheritdoc />
  public ICloudflareApiClient CreateClient(string name)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);

    // Retrieve the named options. IOptionsMonitor.Get(name) returns the default value if the name
    // is not found, so we validate that critical properties are set.
    var options = _optionsMonitor.Get(name);

    if (string.IsNullOrWhiteSpace(options.ApiToken))
      throw new InvalidOperationException(
        $"No Cloudflare API client configuration found for name '{name}'. " +
        $"Ensure AddCloudflareApiClient(\"{name}\", ...) has been called during service registration.");

    // The HttpClient name follows a convention: "CloudflareApiClient:{name}"
    // This matches the name used during registration in ServiceCollectionExtensions.
    var httpClientName = ServiceCollectionExtensions.GetHttpClientName(name);
    var httpClient     = _httpClientFactory.CreateClient(httpClientName);

    // Wrap the named options in IOptions<T> for the CloudflareApiClient constructor.
    var optionsWrapper = new NamedOptionsWrapper<CloudflareApiOptions>(options);

    return new CloudflareApiClient(httpClient, optionsWrapper, _loggerFactory);
  }

  #endregion
}
