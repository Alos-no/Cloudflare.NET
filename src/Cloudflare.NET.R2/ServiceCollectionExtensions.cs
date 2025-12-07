namespace Cloudflare.NET.R2;

using Amazon.S3;
using Configuration;
using Core;
using Core.Validation;
using Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

/// <summary>Provides extension methods for setting up the Cloudflare R2 client in an <see cref="IServiceCollection" />.</summary>
public static class ServiceCollectionExtensions
{
  #region Public (Default Client Registration)

  /// <summary>
  ///   <para>Registers the <see cref="IR2Client" /> and its dependencies using a configuration section.</para>
  ///   <para>
  ///     This is a convenience method that binds to the "R2" section of the application's <see cref="IConfiguration" />.
  ///     It also requires the "Cloudflare" section for the Account ID.
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
  ///     Registers the <see cref="IR2Client" /> and its dependencies, allowing for fine-grained programmatic
  ///     configuration.
  ///   </para>
  ///   <para>
  ///     This method sets up the underlying S3-compatible client tailored for R2 and registers the high-level
  ///     <see cref="IR2Client" /> as a singleton.
  ///   </para>
  ///   <para>
  ///     Configuration is validated at application startup. If required settings (AccessKeyId, SecretAccessKey,
  ///     AccountId) are missing, an <see cref="OptionsValidationException" /> is thrown with a clear error message
  ///     indicating what configuration is missing and how to fix it.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="configureOptions">An action to configure the <see cref="R2Settings" />.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  /// <exception cref="OptionsValidationException">
  ///   Thrown at application startup if required configuration is missing or invalid.
  /// </exception>
  public static IServiceCollection AddCloudflareR2Client(this IServiceCollection services, Action<R2Settings> configureOptions)
  {
    // Configure R2 settings with the provided delegate.
    services.Configure(configureOptions);

    // Register validators for early failure with clear error messages.
    // Using AddSingleton allows multiple validators to be registered.
    // The Options infrastructure runs ALL registered validators and aggregates failures.
    services.AddSingleton<IValidateOptions<R2Settings>, R2SettingsValidator>();

    // Use the shared CloudflareApiOptionsValidator from Core with R2-specific requirements.
    services.AddSingleton<IValidateOptions<CloudflareApiOptions>>(
      new CloudflareApiOptionsValidator(CloudflareValidationRequirements.ForR2));

    // Add options validation at startup to fail fast with clear error messages.
    // This validates the default (unnamed) options instance.
    services
      .AddOptions<R2Settings>()
      .ValidateOnStart();

    services
      .AddOptions<CloudflareApiOptions>()
      .ValidateOnStart();

    // Register the AmazonS3Client as a singleton, configured specifically for Cloudflare R2.
    services.AddSingleton<IAmazonS3>(sp =>
    {
      var r2Settings         = sp.GetRequiredService<IOptions<R2Settings>>().Value;
      var cloudflareSettings = sp.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;

      // Build the endpoint URL with the Account ID.
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

    // Register the factory for named clients.
    services.TryAddSingleton<IR2ClientFactory, R2ClientFactory>();

    return services;
  }

  #endregion


  #region Public (Named Client Registration)

  /// <summary>
  ///   <para>Registers a named <see cref="IR2Client" /> configuration using a configuration section.</para>
  ///   <para>
  ///     Named clients can be retrieved using <see cref="IR2ClientFactory" /> or via keyed services using
  ///     <c>[FromKeyedServices("name")]</c>.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="name">
  ///   The unique name for this client configuration. Used to retrieve the client from the factory or via
  ///   keyed services.
  /// </param>
  /// <param name="configuration">
  ///   The application configuration. Will bind to the "R2:{name}" section for R2 settings and
  ///   "Cloudflare:{name}" section for the Account ID.
  /// </param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is null or whitespace.</exception>
  public static IServiceCollection AddCloudflareR2Client(
    this IServiceCollection services,
    string                  name,
    IConfiguration          configuration)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);

    // Bind to the section "R2:{name}" for R2 settings.
    var r2SectionName = $"R2:{name}";

    return services.AddCloudflareR2Client(name, options => configuration.GetSection(r2SectionName).Bind(options));
  }


  /// <summary>
  ///   <para>Registers a named <see cref="IR2Client" /> configuration with programmatic options.</para>
  ///   <para>
  ///     Named clients can be retrieved using <see cref="IR2ClientFactory" /> or via keyed services using
  ///     <c>[FromKeyedServices("name")]</c>.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="name">
  ///   The unique name for this client configuration. Used to retrieve the client from the factory or via
  ///   keyed services.
  /// </param>
  /// <param name="configureOptions">An action to configure the <see cref="R2Settings" />.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is null or whitespace.</exception>
  /// <exception cref="CloudflareR2ConfigurationException">
  ///   Thrown when the named client is created if required configuration is missing or invalid.
  /// </exception>
  /// <remarks>
  ///   <para>
  ///     This method requires that the Cloudflare API options for the same name are also registered using
  ///     <see
  ///       cref="Core.ServiceCollectionExtensions.AddCloudflareApiClient(Microsoft.Extensions.DependencyInjection.IServiceCollection, string, System.Action{CloudflareApiOptions})" />
  ///     . The Account ID from those options is used to construct the R2 endpoint URL.
  ///   </para>
  ///   <para>
  ///     Unlike the default client registration, named clients are validated when first created via the factory
  ///     or keyed services, not at application startup. This is because named configurations may be dynamically
  ///     added or configured after startup.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  /// // Register multiple named clients
  /// services.AddCloudflareApiClient("primary", options => {
  ///     options.AccountId = "primary-account-id";
  ///     options.ApiToken = "primary-token";
  /// });
  /// services.AddCloudflareR2Client("primary", options => {
  ///     options.AccessKeyId = "primary-key";
  ///     options.SecretAccessKey = "primary-secret";
  ///     // EndpointUrl defaults to "https://{0}.r2.cloudflarestorage.com"
  /// });
  /// 
  /// // Use via factory
  /// public class MyService(IR2ClientFactory factory)
  /// {
  ///     public async Task DoSomething()
  ///     {
  ///         var primaryClient = factory.CreateClient("primary");
  ///         // ...
  ///     }
  /// }
  /// 
  /// // Or use via keyed services
  /// public class MyService([FromKeyedServices("primary")] IR2Client client)
  /// {
  ///     // ...
  /// }
  /// </code>
  /// </example>
  public static IServiceCollection AddCloudflareR2Client(this IServiceCollection services,
                                                         string                  name,
                                                         Action<R2Settings>      configureOptions)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);

    // Configure named R2 options. This allows IOptionsMonitor<R2Settings>.Get(name) to work.
    services.Configure(name, configureOptions);

    // Register validators for clear error messages when creating named clients.
    // Using AddSingleton allows multiple validators to be registered.
    services.AddSingleton<IValidateOptions<R2Settings>, R2SettingsValidator>();

    // Use the shared CloudflareApiOptionsValidator from Core with R2-specific requirements.
    services.AddSingleton<IValidateOptions<CloudflareApiOptions>>(
      new CloudflareApiOptionsValidator(CloudflareValidationRequirements.ForR2));

    // Register the factory for named clients. TryAdd ensures we don't replace an existing registration.
    services.TryAddSingleton<IR2ClientFactory, R2ClientFactory>();

    // Register a keyed service for direct DI injection using [FromKeyedServices("name")].
    services.AddKeyedSingleton<IR2Client>(name, (serviceProvider, key) =>
    {
      var factory = serviceProvider.GetRequiredService<IR2ClientFactory>();

      return factory.CreateClient((string)key!);
    });

    return services;
  }

  #endregion
}
