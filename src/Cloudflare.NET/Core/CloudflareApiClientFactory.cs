namespace Cloudflare.NET.Core;

using Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resilience;
using Validation;

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
    ThrowHelper.ThrowIfNullOrWhiteSpace(name);

    // Retrieve the named options. IOptionsMonitor.Get(name) returns the default value if the name
    // is not found, so we validate that critical properties are set.
    var options = _optionsMonitor.Get(name);

    // Validate the named options using the shared validator for consistent, clear error messages.
    ValidateNamedClientConfiguration(name, options);

    // The HttpClient name follows a convention: "CloudflareApiClient:{name}"
    // This matches the name used during registration in ServiceCollectionExtensions.
    var httpClientName = ServiceCollectionExtensions.GetHttpClientName(name);
    var httpClient     = _httpClientFactory.CreateClient(httpClientName);

    // Wrap the named options in IOptions<T> for the CloudflareApiClient constructor.
    var optionsWrapper = new NamedOptionsWrapper<CloudflareApiOptions>(options);

    return new CloudflareApiClient(httpClient, optionsWrapper, _loggerFactory);
  }


  /// <inheritdoc />
  public ICloudflareApiClient CreateClient(CloudflareApiOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    // Validate the options using the shared validator for consistent, clear error messages.
    ValidateNamedClientConfiguration("dynamic", options);

    // Build the resilience pipeline using the shared builder.
    // This ensures the same resilience behavior as DI-registered clients.
    var logger   = _loggerFactory.CreateLogger(LoggingConstants.Categories.HttpResilience);
    var pipeline = CloudflareResiliencePipelineBuilder.Build(options, logger, clientName: "dynamic");

    // Create the handler chain: ResilienceHandler → SocketsHttpHandler.
    // SocketsHttpHandler is used for proper connection pooling and lifetime management.
    var socketHandler = new SocketsHttpHandler
    {
      // PooledConnectionLifetime ensures connections are recycled periodically,
      // which helps with DNS changes and prevents stale connections.
      // This mirrors the behavior of IHttpClientFactory-managed handlers.
      PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    };

    var resilienceHandler = new ResilienceDelegatingHandler(pipeline, socketHandler);

    // Create and configure the HttpClient using the shared configurator.
    // This ensures the same configuration as DI-registered clients.
    var httpClient = new HttpClient(resilienceHandler);
    CloudflareHttpClientConfigurator.Configure(httpClient, options, setAuthorizationHeader: true);

    // Wrap the options in IOptions<T> for the CloudflareApiClient constructor.
    var optionsWrapper = new NamedOptionsWrapper<CloudflareApiOptions>(options);

    // Create the inner client that handles all API operations.
    var innerClient = new CloudflareApiClient(httpClient, optionsWrapper, _loggerFactory);

    // Wrap in DynamicCloudflareApiClient which handles disposal of the owned HttpClient.
    return new DynamicCloudflareApiClient(innerClient, httpClient);
  }

  #endregion

  #region Methods

  /// <summary>Validates the configuration for a named Cloudflare API client.</summary>
  /// <param name="name">The name of the client configuration being validated.</param>
  /// <param name="options">The options to validate.</param>
  /// <exception cref="InvalidOperationException">Thrown when required configuration is missing or invalid.</exception>
  private static void ValidateNamedClientConfiguration(string name, CloudflareApiOptions options)
  {
    // Use the static validation method for consistent error messages that include the client name.
    var result = CloudflareApiOptionsValidator.ValidateConfiguration(
      name, options, CloudflareValidationRequirements.Default);

    if (result.Failed)
    {
      var message = result.Failures.Count() == 1
        ? $"Cloudflare API configuration error: {result.Failures.First()}"
        : $"Cloudflare API configuration errors for named client '{name}':\n- " + string.Join("\n- ", result.Failures);

      throw new InvalidOperationException(message);
    }
  }

  #endregion
}
