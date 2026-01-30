namespace Cloudflare.NET.Core;

using System.Net.Http.Headers;
using Auth;
using Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resilience;
using Validation;

/// <summary>Provides extension methods for setting up the Cloudflare API client in an IServiceCollection.</summary>
public static class ServiceCollectionExtensions
{
  #region Constants & Statics

  /// <summary>The prefix used for named HttpClient registrations.</summary>
  private const string HttpClientNamePrefix = "CloudflareApiClient";

  #endregion

  #region Methods

  /// <summary>
  ///   <para>Registers the <see cref="ICloudflareApiClient" /> and its dependencies using a configuration section.</para>
  ///   <para>
  ///     This is a convenience method that binds to the "Cloudflare" section of the application's
  ///     <see cref="IConfiguration" />.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="configuration">The application configuration.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  public static IServiceCollection AddCloudflareApiClient(
    this IServiceCollection services,
    IConfiguration          configuration)
  {
    // This overload is for convenience. It finds the "Cloudflare" section and uses it
    // to configure the options.
    return services.AddCloudflareApiClient(options => configuration.GetSection("Cloudflare").Bind(options));
  }


  /// <summary>
  ///   <para>
  ///     Registers the <see cref="ICloudflareApiClient" /> and its dependencies, allowing for fine-grained programmatic
  ///     configuration.
  ///   </para>
  ///   <para>
  ///     This method sets up the necessary <see cref="HttpClient" />, authentication handler, and resilience policies
  ///     for rate limiting and transient error handling.
  ///   </para>
  ///   <para>
  ///     Configuration is validated at application startup. If required settings (ApiToken) are missing, an
  ///     <see cref="OptionsValidationException" /> is thrown with a clear error message indicating what configuration is
  ///     missing and how to fix it.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="configureOptions">An action to configure the <see cref="CloudflareApiOptions" />.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  /// <exception cref="OptionsValidationException">
  ///   Thrown at application startup if required configuration is missing or
  ///   invalid.
  /// </exception>
  public static IServiceCollection AddCloudflareApiClient(this IServiceCollection      services,
                                                          Action<CloudflareApiOptions> configureOptions)
  {
    // Configure the default (unnamed) options using the provided delegate.
    services.Configure(configureOptions);

    // Register the validator for early failure with clear error messages.
    // Using AddSingleton allows multiple validators to be registered (e.g., Core + Analytics).
    // The Options infrastructure runs ALL registered validators and aggregates failures.
    services.AddSingleton<IValidateOptions<CloudflareApiOptions>>(
      new CloudflareApiOptionsValidator(CloudflareValidationRequirements.Default));

    // Add options validation at startup to fail fast with clear error messages.
    services
      .AddOptions<CloudflareApiOptions>()
      .ValidateOnStart();

    // Register the authentication handler as a transient service.
    services.AddTransient<AuthenticationHandler>();

    // Register the HttpClient for the ICloudflareApiClient, configure its base address,
    // and attach the authentication handler to the request pipeline.
    // This registers ICloudflareApiClient as a transient service, which is the standard for typed HttpClients.
    var builder = services.AddHttpClient<ICloudflareApiClient, CloudflareApiClient>((serviceProvider, client) =>
                          {
                            // Resolve the configured options.
                            var options = serviceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;

                            // Configure base properties (base address, timeout).
                            // Auth header is handled by AuthenticationHandler, so don't set it here.
                            CloudflareHttpClientConfigurator.ConfigureBase(client, options);
                          })
                          .AddHttpMessageHandler<AuthenticationHandler>();

    // Add the resilience handler for the default client.
    AddResilienceHandler(builder, resolveOptionsFromDi: true, clientName: null);

    // Register the factory as a singleton. It will be shared by all named client registrations.
    services.TryAddSingleton<ICloudflareApiClientFactory, CloudflareApiClientFactory>();

    return services;
  }

  /// <summary>
  ///   <para>Registers a named <see cref="ICloudflareApiClient" /> configuration using a configuration section.</para>
  ///   <para>
  ///     Named clients can be retrieved using <see cref="ICloudflareApiClientFactory" /> or via keyed services using
  ///     <c>[FromKeyedServices("name")]</c>.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="name">
  ///   The unique name for this client configuration. Used to retrieve the client from the factory or via
  ///   keyed services.
  /// </param>
  /// <param name="configuration">The application configuration. Will bind to the "Cloudflare:{name}" section.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is null or whitespace.</exception>
  public static IServiceCollection AddCloudflareApiClient(
    this IServiceCollection services,
    string                  name,
    IConfiguration          configuration)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(name);

    // Bind to the section "Cloudflare:{name}" for named clients.
    var sectionName = $"Cloudflare:{name}";

    return services.AddCloudflareApiClient(name, options => configuration.GetSection(sectionName).Bind(options));
  }


  /// <summary>
  ///   <para>Registers a named <see cref="ICloudflareApiClient" /> configuration with programmatic options.</para>
  ///   <para>
  ///     Named clients can be retrieved using <see cref="ICloudflareApiClientFactory" /> or via keyed services using
  ///     <c>[FromKeyedServices("name")]</c>.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="name">
  ///   The unique name for this client configuration. Used to retrieve the client from the factory or via
  ///   keyed services.
  /// </param>
  /// <param name="configureOptions">An action to configure the <see cref="CloudflareApiOptions" />.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is null or whitespace.</exception>
  /// <exception cref="InvalidOperationException">
  ///   Thrown when the named client is created if required configuration is
  ///   missing or invalid.
  /// </exception>
  /// <remarks>
  ///   <para>
  ///     Unlike the default client registration, named clients are validated when first created via the factory or keyed
  ///     services, not at application startup. This is because named configurations may be dynamically added or configured
  ///     after startup.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  /// // Register multiple named clients
  /// services.AddCloudflareApiClient("production", options => {
  ///     options.ApiToken = "prod-token";
  ///     options.AccountId = "prod-account-id";
  /// });
  /// services.AddCloudflareApiClient("staging", options => {
  ///     options.ApiToken = "staging-token";
  ///     options.AccountId = "staging-account-id";
  /// });
  /// 
  /// // Use via factory
  /// public class MyService(ICloudflareApiClientFactory factory)
  /// {
  ///     public async Task DoSomething()
  ///     {
  ///         var prodClient = factory.CreateClient("production");
  ///         // ...
  ///     }
  /// }
  /// 
  /// // Or use via keyed services
  /// public class MyService([FromKeyedServices("production")] ICloudflareApiClient client)
  /// {
  ///     // ...
  /// }
  /// </code>
  /// </example>
  public static IServiceCollection AddCloudflareApiClient(this IServiceCollection      services,
                                                          string                       name,
                                                          Action<CloudflareApiOptions> configureOptions)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(name);

    // Configure named options. This allows IOptionsMonitor<CloudflareApiOptions>.Get(name) to work.
    services.Configure(name, configureOptions);

    // Register the validator for clear error messages when creating named clients.
    // Using AddSingleton allows multiple validators to be registered (e.g., Core + Analytics).
    services.AddSingleton<IValidateOptions<CloudflareApiOptions>>(
      new CloudflareApiOptionsValidator(CloudflareValidationRequirements.Default));

    // Compute the HttpClient name for this named configuration.
    var httpClientName = GetHttpClientName(name);

    // Register a named HttpClient with its own configuration.
    // For named clients, we set the Authorization header directly since we can't use
    // the AuthenticationHandler (which relies on the default IOptions<CloudflareApiOptions>).
    var builder = services.AddHttpClient(httpClientName, (serviceProvider, client) =>
    {
      // Resolve the named options using IOptionsMonitor.
      var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CloudflareApiOptions>>();
      var options        = optionsMonitor.Get(name);

      // Configure all properties including the Authorization header for named clients.
      CloudflareHttpClientConfigurator.Configure(client, options, setAuthorizationHeader: true);
    });

    // Add the resilience handler for the named client.
    AddResilienceHandler(builder, resolveOptionsFromDi: false, clientName: name);

    // Register the factory as a singleton. It will be shared by all named client registrations.
    // TryAdd ensures we don't replace an existing registration.
    services.TryAddSingleton<ICloudflareApiClientFactory, CloudflareApiClientFactory>();

    // Register a keyed service for direct DI injection using [FromKeyedServices("name")].
    services.AddKeyedTransient<ICloudflareApiClient>(name, (serviceProvider, key) =>
    {
      var factory = serviceProvider.GetRequiredService<ICloudflareApiClientFactory>();

      return factory.CreateClient((string)key!);
    });

    return services;
  }

  /// <summary>
  ///   Gets the HttpClient name for a given client name. This is used internally by the factory to retrieve the
  ///   correct HttpClient instance.
  /// </summary>
  /// <param name="clientName">The logical name of the client.</param>
  /// <returns>The HttpClient name to use with <see cref="IHttpClientFactory" />.</returns>
  internal static string GetHttpClientName(string clientName)
  {
    return $"{HttpClientNamePrefix}:{clientName}";
  }

  /// <summary>Adds the resilience handler to an HttpClient builder.</summary>
  /// <param name="builder">The HttpClient builder.</param>
  /// <param name="resolveOptionsFromDi">
  ///   If true, resolves options from the default
  ///   <see cref="IOptions{CloudflareApiOptions}" />. If false, resolves from
  ///   <see cref="IOptionsMonitor{CloudflareApiOptions}" /> using the client name.
  /// </param>
  /// <param name="clientName">
  ///   The name of the client for named options resolution. Only used when
  ///   <paramref name="resolveOptionsFromDi" /> is false.
  /// </param>
  private static void AddResilienceHandler(IHttpClientBuilder builder, bool resolveOptionsFromDi, string? clientName)
  {
    // Create a unique resilience pipeline name based on whether this is the default or a named client.
    var pipelineName = clientName is null ? "CloudflarePipeline" : $"CloudflarePipeline:{clientName}";

    // This single resilience handler is configured with a pipeline that mirrors the standard
    // .NET resilience pipeline, but with a configurable per-attempt timeout and a client-side
    // rate limiter tuned for API access. We use a single handler to avoid the complexities and
    // potential for compounding delays that come from stacking multiple handlers.
    // Ref: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience#avoid-stacking-handlers
    builder.AddResilienceHandler(pipelineName, (pipelineBuilder, context) =>
    {
      // Resolve options based on whether this is the default or a named client.
      CloudflareApiOptions cfOptions;

      if (resolveOptionsFromDi)
      {
        cfOptions = context.ServiceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;
      }
      else
      {
        var optionsMonitor = context.ServiceProvider.GetRequiredService<IOptionsMonitor<CloudflareApiOptions>>();
        cfOptions = optionsMonitor.Get(clientName);
      }

      var logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>()
                          .CreateLogger(LoggingConstants.Categories.HttpResilience);

      // Use the shared resilience pipeline builder to ensure consistent configuration
      // between DI-registered clients and dynamically created clients.
      CloudflareResiliencePipelineBuilder.Configure(pipelineBuilder, cfOptions, logger, clientName);
    });
  }

  #endregion
}
