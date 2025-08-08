namespace Cloudflare.NET.Core;

using Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
///   Provides extension methods for setting up the Cloudflare API client in an
///   IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
  #region Methods

  /// <summary>Registers the ICloudflareApiClient and its dependencies with the service collection.</summary>
  /// <param name="services">The IServiceCollection to add the services to.</param>
  /// <param name="configuration">The application configuration.</param>
  /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
  public static IServiceCollection AddCloudflareApiClient(
    this IServiceCollection services,
    IConfiguration          configuration)
  {
    // Bind the options from the "Cloudflare" section of appsettings.json
    services.Configure<CloudflareApiOptions>(configuration.GetSection("Cloudflare"));

    // Register the authentication handler as a transient service.
    services.AddTransient<AuthenticationHandler>();

    // Register the HttpClient for the ICloudflareApiClient, configure its base address,
    // and attach the authentication handler to the request pipeline.
    // This registers ICloudflareApiClient as a transient service, which is the standard for typed HttpClients.
    services.AddHttpClient<ICloudflareApiClient, CloudflareApiClient>((serviceProvider, client) =>
            {
              // Resolve the configured options.
              var options = serviceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;

              // Use the URL from the options, which has a built-in default value.
              if (string.IsNullOrWhiteSpace(options.ApiBaseUrl))
                throw new InvalidOperationException(
                  "Cloudflare API Base URL is missing. Please configure it in the 'Cloudflare' settings section.");

              client.BaseAddress = new Uri(options.ApiBaseUrl);
            })
            .AddHttpMessageHandler<AuthenticationHandler>();

    return services;
  }

  #endregion
}
