namespace Cloudflare.NET.Core;

using System.Net;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using Auth;
using Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using RateLimitHeaders.Polly;
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

                            ConfigureHttpClient(client, options);
                          })
                          .AddHttpMessageHandler<AuthenticationHandler>();

    // Add the resilience handler for the default client.
    AddResilienceHandler(builder, true, null);

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

      ConfigureHttpClient(client, options);

      // Set the Authorization header directly for named clients.
      if (!string.IsNullOrWhiteSpace(options.ApiToken))
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiToken);
    });

    // Add the resilience handler for the named client.
    AddResilienceHandler(builder, false, name);

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

  /// <summary>Configures the base properties of an HttpClient for Cloudflare API access.</summary>
  /// <param name="client">The HttpClient to configure.</param>
  /// <param name="options">The options containing the API configuration.</param>
  private static void ConfigureHttpClient(HttpClient client, CloudflareApiOptions options)
  {
    // Use the URL from the options, which has a built-in default value.
    if (string.IsNullOrWhiteSpace(options.ApiBaseUrl))
      throw new InvalidOperationException(
        "Cloudflare API Base URL is missing. Please configure it in the 'Cloudflare' settings section.");

    client.BaseAddress = new Uri(options.ApiBaseUrl);

    // Set a long HttpClient.Timeout so that our resilience pipeline's TotalRequestTimeout is the effective timeout.
    // Ref: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience#httpclient-timeout
    client.Timeout = TimeSpan.FromMinutes(5);
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
    builder.AddResilienceHandler(pipelineName, (b, context) =>
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

      ConfigureResiliencePipeline(b, cfOptions, logger, clientName);
    });
  }


  /// <summary>Configures the resilience pipeline with rate limiting, retries, and circuit breaker.</summary>
  /// <param name="builder">The resilience pipeline builder.</param>
  /// <param name="cfOptions">The Cloudflare API options.</param>
  /// <param name="logger">The logger for resilience events.</param>
  /// <param name="clientName">Optional client name for logging context.</param>
  private static void ConfigureResiliencePipeline(ResiliencePipelineBuilder<HttpResponseMessage> builder,
                                                  CloudflareApiOptions                           cfOptions,
                                                  ILogger                                        logger,
                                                  string?                                        clientName)
  {
    // Create name prefixes for this specific client's resilience components.
    var namePrefix = clientName is null ? "Cloudflare" : $"Cloudflare:{clientName}";

    // The standard pipeline order is:
    // Rate Limiter -> Rate Limit Headers -> Total Timeout -> Retry -> Circuit Breaker -> Attempt Timeout.
    // Ref: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience#standard-pipeline

    // 1. OUTERMOST: Client-side rate limiting to avoid sending requests that are likely to be throttled.
    var rateLimiterName = $"{namePrefix}:RateLimiter";

    builder.AddRateLimiter(new HttpRateLimiterStrategyOptions
    {
      Name = rateLimiterName,
      DefaultRateLimiterOptions = new ConcurrencyLimiterOptions
      {
        PermitLimit          = Math.Max(1, cfOptions.RateLimiting.PermitLimit),
        QueueLimit           = Math.Max(0, cfOptions.RateLimiting.QueueLimit),
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
      },
      OnRejected = args =>
      {
        // Try to extract a Retry-After hint if the limiter can calculate one.
        args.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter);

        logger.LogWarning(
          "Request rejected by {Strategy}. Concurrency limit reached. RetryAfter={RetryAfter}.",
          rateLimiterName, retryAfter == default ? "n/a" : $"{(int)retryAfter.TotalSeconds}s");

        return default;
      }
    });

    // 2. Rate Limit Headers: Proactive throttling based on server-returned rate limit headers.
    // This processes headers like RateLimit-Remaining, RateLimit-Limit, and RateLimit-Reset to
    // preemptively slow down requests before hitting 429 responses.
    // Ref: https://alos.no/ratelimitheaders/articles/polly-integration.html
    builder.AddRateLimitHeaders(options =>
    {
      // Enable proactive throttling to delay requests when quota is running low.
      options.EnableProactiveThrottling = cfOptions.RateLimiting.EnableProactiveThrottling;

      // Set the threshold at which throttling begins (e.g., 0.1 = 10% remaining).
      options.QuotaLowThreshold = cfOptions.RateLimiting.QuotaLowThreshold;
    });

    // 3. Total Request Timeout: An outer timeout that covers the entire operation, including all retries.
    builder.AddTimeout(new HttpTimeoutStrategyOptions
    {
      Name    = $"{namePrefix}:TotalTimeout",
      Timeout = TimeSpan.FromSeconds(60)
    });

    // 4. Retry Strategy: Handles transient failures. Only add when enabled.
    // Polly v8 validates MaxRetryAttempts >= 1. If users set MaxRetries = 0 to disable retries,
    // we skip adding the retry component entirely to avoid validation failures.
    if (cfOptions.RateLimiting.MaxRetries > 0)
    {
      var retryOptions = new HttpRetryStrategyOptions
      {
        // Defaults handle 5xx, 408, 429, HttpRequestException, and TimeoutRejectedException.
        // Ref: https://learn.microsoft.com/en-us/dotnet/api/polly.extensions.http.httpretrystrategyoptions
        Name             = $"{namePrefix}:Retry",
        BackoffType      = DelayBackoffType.Exponential,
        Delay            = TimeSpan.FromSeconds(1),
        UseJitter        = true,
        MaxRetryAttempts = cfOptions.RateLimiting.MaxRetries,

        // By default, HttpRetryStrategyOptions honors the 'Retry-After' header. A custom generator is not needed.
        // Ref: https://devblogs.microsoft.com/dotnet/building-resilient-cloud-services-with-dotnet-8/

        // This custom predicate ensures we always retry on server errors (5xx) and transient exceptions,
        // but only retry on 429s if rate limit handling is explicitly enabled in options.
        ShouldHandle = args =>
        {
          // ----- Method-based gating (idempotency) -----
          // Do NOT retry non-idempotent methods.
          // Retry allowed for: GET, HEAD, OPTIONS, TRACE, PUT, DELETE.
          // No retry for: POST, PATCH, CONNECT (and anything else not listed above).

          // 0) Gate by HTTP method *idempotency*, not "safety"
          var method = args.Outcome.Result?.RequestMessage?.Method;

          // Treat GET, HEAD, OPTIONS, TRACE, PUT, DELETE as idempotent.
          if (method is not null)
          {
            var isIdempotent =
              method == HttpMethod.Get ||
              method == HttpMethod.Head ||
              method == HttpMethod.Options ||
              method == HttpMethod.Trace ||
              method == HttpMethod.Put ||
              method == HttpMethod.Delete;

            if (!isIdempotent)
              return new ValueTask<bool>(false);
          }

          // 1) Exceptions we consider transient.
          if (args.Outcome.Exception is HttpRequestException or TimeoutRejectedException)
            return new ValueTask<bool>(true);

          // 2) HTTP responses we consider transient.
          if (args.Outcome.Result is not { } response)
            return new ValueTask<bool>(false);

          var statusCode = response.StatusCode;

          // Retry on 408 and 5xx.
          if (statusCode == HttpStatusCode.RequestTimeout || (int)statusCode >= 500)
            return new ValueTask<bool>(true);

          // Retry on 429 only when rate-limit handling is enabled.
          if (statusCode == HttpStatusCode.TooManyRequests)
            return new ValueTask<bool>(cfOptions.RateLimiting.IsEnabled);

          return new ValueTask<bool>(false);
        },

        OnRetry = args =>
        {
          var req = args.Outcome.Result?.RequestMessage;

          logger.LogWarning(
            "Transient failure for {Method} {Uri}. Attempt {Attempt}/{MaxAttempts}. Next delay: {Delay}.",
            req?.Method,
            req?.RequestUri,
            args.AttemptNumber + 1,
            cfOptions.RateLimiting.MaxRetries,
            args.RetryDelay);

          return default;
        }
      };

      // IMPORTANT: keep this removed so DELETE/PUT can retry when appropriate.
      // retryOptions.DisableForUnsafeHttpMethods();

      builder.AddRetry(retryOptions);
    }

    // 5. Circuit Breaker: Stops sending requests after too many consecutive failures. Defaults are sensible.
    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions { Name = $"{namePrefix}:CircuitBreaker" });

    // 6. INNERMOST: Per-attempt timeout, using our configurable value.
    builder.AddTimeout(new HttpTimeoutStrategyOptions
    {
      Name    = $"{namePrefix}:AttemptTimeout",
      Timeout = cfOptions.DefaultTimeout
    });
  }

  #endregion
}
