namespace Cloudflare.NET.Analytics;

using System.Text.Json;
using Core;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
///   Provides extension methods for setting up the Cloudflare Analytics API client in an
///   <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
  #region Methods

  /// <summary>
  ///   <para>
  ///     Registers the <see cref="IAnalyticsApi" /> and its dependencies with the service
  ///     collection.
  ///   </para>
  ///   <para>
  ///     This method should be called after
  ///     <see
  ///       cref="Core.ServiceCollectionExtensions.AddCloudflareApiClient(IServiceCollection, IConfiguration)" />
  ///     , as it relies on the core <see cref="CloudflareApiOptions" /> for authentication and
  ///     endpoint configuration.
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

              if (string.IsNullOrWhiteSpace(options.ApiToken))
                throw new InvalidOperationException(
                  "Cloudflare API Token is missing. Please ensure AddCloudflareApiClient() is configured correctly.");

              if (string.IsNullOrWhiteSpace(options.GraphQlApiUrl))
                throw new InvalidOperationException(
                  "Cloudflare GraphQL API URL is missing. Please configure it in the 'Cloudflare' settings section.");

              client.BaseAddress                         = new Uri(options.GraphQlApiUrl);
              client.DefaultRequestHeaders.Authorization = new("Bearer", options.ApiToken);
            })
            .AddStandardResilienceHandler();

    // Register a singleton GraphQL client built on the resilient HttpClient above.. It's configured with the API endpoint and
    // authorization token from the Cloudflare options, which are expected to be
    // registered by the core SDK.
    services.AddSingleton<IGraphQLClient>(sp =>
    {
      var options = sp.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;

      // While the code generator uses [JsonPropertyName] which takes precedence,
      // setting CamelCase is a robust default for any ad-hoc types.
      var camelCaseOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
      var serializer       = new SystemTextJsonSerializer(camelCaseOptions);

      var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(Constants.GraphQlHttpClientName);

      var gqlOptions = new GraphQLHttpClientOptions
      {
        EndPoint = new Uri(options.GraphQlApiUrl)
      };

      // GraphQLHttpClient has a constructor overload that accepts an HttpClient.
      var client = new GraphQLHttpClient(gqlOptions, serializer, httpClient);

      return client;
    });

    return services;
  }

  #endregion
}
