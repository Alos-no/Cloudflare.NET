namespace Cloudflare.NET.R2;

using Amazon.S3;
using Configuration;
using Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
///   Provides extension methods for setting up the Cloudflare R2 client in an
///   <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
  #region Methods

  /// <summary>
  ///   <para>
  ///     Registers the <see cref="IR2Client" /> and its dependencies using a configuration
  ///     section.
  ///   </para>
  ///   <para>
  ///     This is a convenience method that binds to the "R2" section of the application's
  ///     <see cref="IConfiguration" />. It also requires the "Cloudflare" section for the Account
  ///     ID.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="configuration">The application configuration, used to bind R2 settings.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  public static IServiceCollection AddCloudflareR2Client(this IServiceCollection services, IConfiguration configuration)
  {
    return services.AddCloudflareR2Client(options => configuration.GetSection("R2").Bind(options));
  }


  /// <summary>
  ///   <para>
  ///     Registers the <see cref="IR2Client" /> and its dependencies, allowing for fine-grained
  ///     programmatic configuration.
  ///   </para>
  ///   <para>
  ///     This method sets up the underlying S3-compatible client tailored for R2 and registers
  ///     the high-level <see cref="IR2Client" /> as a singleton.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="configureOptions">An action to configure the <see cref="R2Settings" />.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  public static IServiceCollection AddCloudflareR2Client(this IServiceCollection services, Action<R2Settings> configureOptions)
  {
    // Bind the R2 settings from the "R2" configuration section.
    services.Configure(configureOptions);

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
        AuthenticationRegion = r2Settings.Region
      };

      return new AmazonS3Client(r2Settings.AccessKeyId, r2Settings.SecretAccessKey, config);
    });

    // Register the primary R2 client service.
    services.AddSingleton<IR2Client, R2Client>();

    return services;
  }

  #endregion
}
