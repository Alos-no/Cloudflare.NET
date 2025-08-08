namespace Cloudflare.NET.Analytics;

using System.Text.Json;
using Core;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
///   Provides extension methods for setting up the Cloudflare Analytics API client in an
///   IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
  #region Methods

  /// <summary>
  ///   Registers the IAnalyticsApi and its dependencies with the service collection. This
  ///   should be called after AddCloudflareApiClient().
  /// </summary>
  /// <param name="services">The IServiceCollection to add the services to.</param>
  /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
  public static IServiceCollection AddCloudflareAnalytics(this IServiceCollection services)
  {
    // Register the Analytics API implementation.
    services.AddSingleton<IAnalyticsApi, AnalyticsApi>();

    // Register a singleton GraphQL client. It's configured with the API endpoint and
    // authorization token from the Cloudflare options, which are expected to be
    // registered by the core SDK.
    services.AddSingleton<IGraphQLClient>(sp =>
    {
      var options = sp.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;
      if (string.IsNullOrWhiteSpace(options.ApiToken))
        throw new InvalidOperationException(
          "Cloudflare API Token is missing. Please ensure AddCloudflareApiClient() is configured correctly.");
      if (string.IsNullOrWhiteSpace(options.GraphQlApiUrl))
        throw new InvalidOperationException(
          "Cloudflare GraphQL API URL is missing. Please configure it in the 'Cloudflare' settings section.");

      var camelCaseOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
      var client           = new GraphQLHttpClient(options.GraphQlApiUrl, new SystemTextJsonSerializer(camelCaseOptions));
      client.HttpClient.DefaultRequestHeaders.Authorization = new("Bearer", options.ApiToken);
      return client;
    });

    return services;
  }

  #endregion
}
