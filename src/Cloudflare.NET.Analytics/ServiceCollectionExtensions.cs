namespace Cloudflare.NET.Analytics;

using System.Net.Http.Headers;
using System.Text.Json;
using Core;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>
///   Provides extension methods for setting up the Cloudflare Analytics API client in an
///   <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
  #region Methods

  /// <summary>
  ///   <para>Registers the <see cref="IAnalyticsApi" /> and its dependencies with the service collection.</para>
  ///   <para>
  ///     This method should be called after
  ///     <see
  ///       cref="Core.ServiceCollectionExtensions.AddCloudflareApiClient(IServiceCollection, IConfiguration)" />
  ///     , as it relies on the core <see cref="CloudflareApiOptions" /> for authentication and endpoint configuration.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  public static IServiceCollection AddCloudflareAnalytics(this IServiceCollection services)
  {
    // Register the Analytics API implementation.
    services.AddSingleton<IAnalyticsApi, AnalyticsApi>();

    // Register a named HttpClient for GraphQL that uses the standard resilience pipeline.
    services.AddHttpClient(Constants.GraphQlHttpClientName, (sp, client) =>
            {
              var options = sp.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;

              ConfigureHttpClient(client, options);
            })
            .AddStandardResilienceHandler();

    // Register a singleton GraphQL client built on the resilient HttpClient above. It's configured with the API endpoint and
    // authorization token from the Cloudflare options, which are expected to be
    // registered by the core SDK.
    services.AddSingleton<IGraphQLClient>(sp =>
    {
      var options = sp.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;

      return CreateGraphQlClient(sp, options, Constants.GraphQlHttpClientName);
    });

    // Register the factory for named clients.
    services.TryAddSingleton<IAnalyticsApiFactory, AnalyticsApiFactory>();

    return services;
  }

  /// <summary>
  ///   <para>Registers a named <see cref="IAnalyticsApi" /> configuration.</para>
  ///   <para>
  ///     Named clients can be retrieved using <see cref="IAnalyticsApiFactory" /> or via keyed services using
  ///     <c>[FromKeyedServices("name")]</c>.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="name">
  ///   The unique name for this client configuration. This must match the name used when registering the corresponding
  ///   <see cref="CloudflareApiOptions" /> via
  ///   <see
  ///     cref="Core.ServiceCollectionExtensions.AddCloudflareApiClient(IServiceCollection, string, System.Action{CloudflareApiOptions})" />
  ///   .
  /// </param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is null or whitespace.</exception>
  /// <remarks>
  ///   <para>
  ///     This method requires that the Cloudflare API options for the same name are registered using
  ///     <see
  ///       cref="Core.ServiceCollectionExtensions.AddCloudflareApiClient(IServiceCollection, string, System.Action{CloudflareApiOptions})" />
  ///     . The API token and GraphQL URL from those options are used for authentication and endpoint configuration.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  /// // Register multiple named clients
  /// services.AddCloudflareApiClient("account1", options => {
  ///     options.ApiToken = "token1";
  ///     options.AccountId = "account1-id";
  /// });
  /// services.AddCloudflareAnalytics("account1");
  /// 
  /// services.AddCloudflareApiClient("account2", options => {
  ///     options.ApiToken = "token2";
  ///     options.AccountId = "account2-id";
  /// });
  /// services.AddCloudflareAnalytics("account2");
  /// 
  /// // Use via factory
  /// public class MyService(IAnalyticsApiFactory factory)
  /// {
  ///     public async Task DoSomething()
  ///     {
  ///         var account1Api = factory.CreateClient("account1");
  ///         // ...
  ///     }
  /// }
  /// 
  /// // Or use via keyed services
  /// public class MyService([FromKeyedServices("account1")] IAnalyticsApi api)
  /// {
  ///     // ...
  /// }
  /// </code>
  /// </example>
  public static IServiceCollection AddCloudflareAnalytics(this IServiceCollection services,
                                                          string                  name)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);

    // Compute the HttpClient name for this named configuration.
    var httpClientName = AnalyticsApiFactory.GetHttpClientName(name);

    // Register a named HttpClient for this Analytics client.
    services.AddHttpClient(httpClientName, (sp, client) =>
            {
              var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<CloudflareApiOptions>>();
              var options        = optionsMonitor.Get(name);

              ConfigureHttpClient(client, options);
            })
            .AddStandardResilienceHandler();

    // Register the factory for named clients. TryAdd ensures we don't replace an existing registration.
    services.TryAddSingleton<IAnalyticsApiFactory, AnalyticsApiFactory>();

    // Register a keyed service for direct DI injection using [FromKeyedServices("name")].
    services.AddKeyedSingleton<IAnalyticsApi>(name, (serviceProvider, key) =>
    {
      var factory = serviceProvider.GetRequiredService<IAnalyticsApiFactory>();

      return factory.CreateClient((string)key!);
    });

    return services;
  }

  /// <summary>Configures the HttpClient for GraphQL requests.</summary>
  /// <param name="client">The HttpClient to configure.</param>
  /// <param name="options">The Cloudflare API options.</param>
  private static void ConfigureHttpClient(HttpClient client, CloudflareApiOptions options)
  {
    if (string.IsNullOrWhiteSpace(options.ApiToken))
      throw new InvalidOperationException(
        "Cloudflare API Token is missing. Please ensure AddCloudflareApiClient() is configured correctly.");

    if (string.IsNullOrWhiteSpace(options.GraphQlApiUrl))
      throw new InvalidOperationException(
        "Cloudflare GraphQL API URL is missing. Please configure it in the 'Cloudflare' settings section.");

    client.BaseAddress                         = new Uri(options.GraphQlApiUrl);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiToken);
  }


  /// <summary>Creates a GraphQL client using the specified HttpClient.</summary>
  /// <param name="serviceProvider">The service provider.</param>
  /// <param name="options">The Cloudflare API options.</param>
  /// <param name="httpClientName">The name of the HttpClient to use.</param>
  /// <returns>A configured <see cref="IGraphQLClient" /> instance.</returns>
  private static IGraphQLClient CreateGraphQlClient(IServiceProvider     serviceProvider,
                                                    CloudflareApiOptions options,
                                                    string               httpClientName)
  {
    // While the code generator uses [JsonPropertyName] which takes precedence,
    // setting CamelCase is a robust default for any ad-hoc types.
    var camelCaseOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    var serializer       = new SystemTextJsonSerializer(camelCaseOptions);

    var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientName);

    var gqlOptions = new GraphQLHttpClientOptions
    {
      EndPoint = new Uri(options.GraphQlApiUrl)
    };

    // GraphQLHttpClient has a constructor overload that accepts an HttpClient.
    var client = new GraphQLHttpClient(gqlOptions, serializer, httpClient);

    return client;
  }

  #endregion
}
