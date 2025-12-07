namespace Cloudflare.NET.Analytics;

using System.Collections.Concurrent;
using System.Text.Json;
using Core;
using Core.Validation;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
///   Factory that creates named <see cref="IAnalyticsApi" /> instances. Each named client is configured with its
///   own GraphQL endpoint and authentication token from the corresponding <see cref="CloudflareApiOptions" />.
/// </summary>
/// <remarks>
///   <para>
///     This factory caches created clients to avoid the overhead of creating new GraphQL clients on every request.
///     Clients are cached by name and reused for subsequent requests.
///   </para>
///   <para>
///     Named clients are registered using
///     <see
///       cref="ServiceCollectionExtensions.AddCloudflareAnalytics(Microsoft.Extensions.DependencyInjection.IServiceCollection, string)" />
///     .
///   </para>
/// </remarks>
public sealed class AnalyticsApiFactory : IAnalyticsApiFactory, IDisposable
{
  #region Properties & Fields - Non-Public

  /// <summary>Cache of created Analytics API clients by name.</summary>
  private readonly ConcurrentDictionary<string, (IAnalyticsApi Api, IGraphQLClient GraphQlClient)> _clientCache = new();

  /// <summary>The HTTP client factory for creating named HttpClient instances.</summary>
  private readonly IHttpClientFactory _httpClientFactory;

  /// <summary>The logger factory for creating loggers.</summary>
  private readonly ILoggerFactory _loggerFactory;

  /// <summary>The options monitor for Cloudflare API options.</summary>
  private readonly IOptionsMonitor<CloudflareApiOptions> _optionsMonitor;

  /// <summary>Flag to track if the factory has been disposed.</summary>
  private bool _disposed;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AnalyticsApiFactory" /> class.</summary>
  /// <param name="httpClientFactory">The HTTP client factory.</param>
  /// <param name="optionsMonitor">The options monitor for named Cloudflare API options.</param>
  /// <param name="loggerFactory">The logger factory.</param>
  public AnalyticsApiFactory(IHttpClientFactory                    httpClientFactory,
                             IOptionsMonitor<CloudflareApiOptions> optionsMonitor,
                             ILoggerFactory                        loggerFactory)
  {
    _httpClientFactory = httpClientFactory;
    _optionsMonitor    = optionsMonitor;
    _loggerFactory     = loggerFactory;
  }


  /// <summary>Disposes all cached GraphQL clients.</summary>
  public void Dispose()
  {
    if (_disposed)
      return;

    _disposed = true;

    // Dispose all cached GraphQL clients.
    foreach (var (_, graphQlClient) in _clientCache.Values)
      if (graphQlClient is IDisposable disposable)
        disposable.Dispose();

    _clientCache.Clear();
  }

  #endregion

  #region Methods Impl

  /// <inheritdoc />
  public IAnalyticsApi CreateClient(string name)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ObjectDisposedException.ThrowIf(_disposed, this);

    // Use GetOrAdd to ensure thread-safe creation of clients.
    return _clientCache.GetOrAdd(name, CreateClientCore).Api;
  }

  #endregion

  #region Methods

  /// <summary>Creates a new Analytics API client instance for the given name.</summary>
  /// <param name="name">The name of the client configuration.</param>
  /// <returns>A tuple containing the new <see cref="IAnalyticsApi" /> instance and its underlying GraphQL client.</returns>
  /// <exception cref="InvalidOperationException">
  ///   Thrown when required configuration is missing or invalid for the named client.
  /// </exception>
  private (IAnalyticsApi Api, IGraphQLClient GraphQlClient) CreateClientCore(string name)
  {
    // Retrieve the named Cloudflare options.
    var options = _optionsMonitor.Get(name);

    // Validate the named options using the shared validator for consistent, clear error messages.
    ValidateNamedClientConfiguration(name, options);

    // Get the named HttpClient.
    var httpClientName = GetHttpClientName(name);
    var httpClient     = _httpClientFactory.CreateClient(httpClientName);

    // Create the GraphQL client configuration.
    var gqlOptions = new GraphQLHttpClientOptions
    {
      EndPoint = new Uri(options.GraphQlApiUrl)
    };

    // While the code generator uses [JsonPropertyName] which takes precedence,
    // setting CamelCase is a robust default for any ad-hoc types.
    var camelCaseOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    var serializer       = new SystemTextJsonSerializer(camelCaseOptions);

    // GraphQLHttpClient has a constructor overload that accepts an HttpClient.
    var graphQlClient = new GraphQLHttpClient(gqlOptions, serializer, httpClient);

    // Create and return the Analytics API.
    var analyticsApi = new AnalyticsApi(graphQlClient, _loggerFactory);

    return (analyticsApi, graphQlClient);
  }


  /// <summary>Validates the configuration for a named Analytics client.</summary>
  /// <param name="name">The name of the client configuration being validated.</param>
  /// <param name="options">The options to validate.</param>
  /// <exception cref="InvalidOperationException">Thrown when required configuration is missing or invalid.</exception>
  private static void ValidateNamedClientConfiguration(string name, CloudflareApiOptions options)
  {
    // Use the static validation method for consistent error messages that include the client name.
    var result = CloudflareApiOptionsValidator.ValidateConfiguration(
      name, options, CloudflareValidationRequirements.ForAnalytics);

    if (result.Failed)
    {
      var message = result.Failures.Count() == 1
        ? $"Cloudflare Analytics configuration error: {result.Failures.First()}"
        : $"Cloudflare Analytics configuration errors for named client '{name}':\n- " + string.Join("\n- ", result.Failures);

      throw new InvalidOperationException(message);
    }
  }


  /// <summary>Gets the HttpClient name for a given Analytics client name.</summary>
  /// <param name="clientName">The logical name of the client.</param>
  /// <returns>The HttpClient name to use with <see cref="IHttpClientFactory" />.</returns>
  internal static string GetHttpClientName(string clientName)
  {
    return $"{Constants.GraphQlHttpClientName}:{clientName}";
  }

  #endregion
}
