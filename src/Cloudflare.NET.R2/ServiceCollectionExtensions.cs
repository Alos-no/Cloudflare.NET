namespace Cloudflare.NET.R2;

using Amazon.S3;
using Configuration;
using Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
///   Provides extension methods for setting up the Cloudflare R2 client in an
///   IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
  #region Methods

  /// <summary>Registers the IR2Client and its dependencies with the service collection.</summary>
  /// <param name="services">The IServiceCollection to add the services to.</param>
  /// <param name="configuration">The application configuration, used to bind R2 settings.</param>
  /// <returns>The IServiceCollection so that additional calls can be chained.</returns>
  public static IServiceCollection AddCloudflareR2Client(this IServiceCollection services, IConfiguration configuration)
  {
    // Bind the R2 settings from the "R2" configuration section.
    services.Configure<R2Settings>(configuration.GetSection("R2"));

    // Register the AmazonS3Client as a singleton, configured specifically for Cloudflare R2.
    services.AddSingleton<IAmazonS3>(sp =>
    {
      var r2Settings         = sp.GetRequiredService<IOptions<R2Settings>>().Value;
      var cloudflareSettings = sp.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;

      if (string.IsNullOrWhiteSpace(cloudflareSettings.AccountId))
        throw new InvalidOperationException("Cloudflare Account ID is missing. Cannot construct R2 endpoint URL.");

      var endpointUrl = string.Format(r2Settings.EndpointUrl, cloudflareSettings.AccountId);

      var config = new AmazonS3Config
      {
        ServiceURL           = endpointUrl,
        ForcePathStyle       = true,
        AuthenticationRegion = r2Settings.Region,
      };

      return new AmazonS3Client(r2Settings.AccessKeyId, r2Settings.SecretAccessKey, config);
    });

    // Register the primary R2 client service.
    services.AddSingleton<IR2Client, R2Client>();

    return services;
  }

  #endregion
}
