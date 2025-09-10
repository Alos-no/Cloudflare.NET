namespace Cloudflare.NET.Core;

using System.Net;
using System.Threading.RateLimiting;
using Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;

/// <summary>
///   Provides extension methods for setting up the Cloudflare API client in an
///   IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
  #region Methods

  /// <summary>
  ///   <para>
  ///     Registers the <see cref="ICloudflareApiClient" /> and its dependencies using a
  ///     configuration section.
  ///   </para>
  ///   <para>
  ///     This is a convenience method that binds to the "Cloudflare" section of the
  ///     application's <see cref="IConfiguration" />.
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
  ///     Registers the <see cref="ICloudflareApiClient" /> and its dependencies, allowing for
  ///     fine-grained programmatic configuration.
  ///   </para>
  ///   <para>
  ///     This method sets up the necessary <see cref="HttpClient" />, authentication handler,
  ///     and resilience policies for rate limiting and transient error handling.
  ///   </para>
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
  /// <param name="configureOptions">An action to configure the <see cref="CloudflareApiOptions" />.</param>
  /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
  public static IServiceCollection AddCloudflareApiClient(this IServiceCollection services,
                                                          Action<CloudflareApiOptions>
                                                            configureOptions)
  {
    // Configure the options using the provided delegate.
    services.Configure(configureOptions);

    // Register the authentication handler as a transient service.
    services.AddTransient<AuthenticationHandler>();

    // Register the HttpClient for the ICloudflareApiClient, configure its base address,
    // and attach the authentication handler to the request pipeline.
    // This registers ICloudflareApiClient as a transient service, which is the standard for typed HttpClients.
    var builder = services.AddHttpClient<ICloudflareApiClient, CloudflareApiClient>((serviceProvider, client) =>
                          {
                            // Resolve the configured options.
                            var options = serviceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;

                            // Use the URL from the options, which has a built-in default value.
                            if (string.IsNullOrWhiteSpace(options.ApiBaseUrl))
                              throw new InvalidOperationException(
                                "Cloudflare API Base URL is missing. Please configure it in the 'Cloudflare' settings section.");

                            client.BaseAddress = new Uri(options.ApiBaseUrl);

                            // Set a long HttpClient.Timeout so that our resilience pipeline's TotalRequestTimeout is the effective timeout.
                            // Ref: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience#httpclient-timeout
                            client.Timeout = TimeSpan.FromMinutes(5);
                          })
                          .AddHttpMessageHandler<AuthenticationHandler>();

    // This single resilience handler is configured with a pipeline that mirrors the standard
    // .NET resilience pipeline, but with a configurable per-attempt timeout and a client-side
    // rate limiter tuned for API access. We use a single handler to avoid the complexities and
    // potential for compounding delays that come from stacking multiple handlers.
    // Ref: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience#avoid-stacking-handlers
    builder.AddResilienceHandler("CloudflarePipeline", (b, context) =>
    {
      var cfOptions = context.ServiceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;
      var logger    = context.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(LoggingConstants.Categories.HttpResilience);

      // The standard pipeline order is: Rate Limiter -> Total Timeout -> Retry -> Circuit Breaker -> Attempt Timeout.
      // Ref: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience#standard-pipeline

      // 1. OUTERMOST: Client-side rate limiting to avoid sending requests that are likely to be throttled.
      const string rateLimiterName = "Cloudflare:RateLimiter";
      b.AddRateLimiter(new HttpRateLimiterStrategyOptions
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
          // Try to extract a Retry-After hint if the limiter can calculate one
          args.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter);

          logger.LogWarning(
            "Request rejected by {Strategy}. Concurrency limit reached. RetryAfter={RetryAfter}.",
            rateLimiterName, retryAfter == default ? "n/a" : $"{(int)retryAfter.TotalSeconds}s");

          return default;
        }
      });

      // 2. Total Request Timeout: An outer timeout that covers the entire operation, including all retries.
      b.AddTimeout(new HttpTimeoutStrategyOptions
      {
        Name    = "Cloudflare:TotalTimeout",
        Timeout = TimeSpan.FromSeconds(60)
      });

      // 3. Retry Strategy: Handles transient failures. Only add when enabled.
      // Polly v8 validates MaxRetryAttempts >= 1. If users set MaxRetries = 0 to disable retries,
      // we skip adding the retry component entirely to avoid validation failures.
      if (cfOptions.RateLimiting.MaxRetries > 0)
      {
        var retryOptions = new HttpRetryStrategyOptions
        {
          // Defaults handle 5xx, 408, 429, HttpRequestException, and TimeoutRejectedException.
          // Ref: https://learn.microsoft.com/en-us/dotnet/api/polly.extensions.http.httpretrystrategyoptions
          Name             = "Cloudflare:Retry",
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

            // Treat GET, HEAD, OPTIONS, TRACE, PUT, DELETE as idempotent
            if (method is not null)
            {
              var isIdempotent =
                method == HttpMethod.Get     ||
                method == HttpMethod.Head    ||
                method == HttpMethod.Options ||
                method == HttpMethod.Trace   ||
                method == HttpMethod.Put     ||
                method == HttpMethod.Delete;

              if (!isIdempotent)
                return ValueTask.FromResult(false);
            }

            // 1) Exceptions we consider transient
            if (args.Outcome.Exception is HttpRequestException or TimeoutRejectedException)
              return ValueTask.FromResult(true);

            // 2) HTTP responses we consider transient
            if (args.Outcome.Result is not { } response)
              return ValueTask.FromResult(false);

            var statusCode = response.StatusCode;

            // Retry on 408 and 5xx
            if (statusCode == HttpStatusCode.RequestTimeout || (int)statusCode >= 500)
              return ValueTask.FromResult(true);

            // Retry on 429 only when rate-limit handling is enabled
            if (statusCode == HttpStatusCode.TooManyRequests)
              return ValueTask.FromResult(cfOptions.RateLimiting.IsEnabled);

            return ValueTask.FromResult(false);
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

        b.AddRetry(retryOptions);
      }

      // 4. Circuit Breaker: Stops sending requests after too many consecutive failures. Defaults are sensible.
      b.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions { Name = "Cloudflare:CircuitBreaker" });

      // 5. INNERMOST: Per-attempt timeout, using our configurable value.
      b.AddTimeout(new HttpTimeoutStrategyOptions
      {
        Name    = "Cloudflare:AttemptTimeout",
        Timeout = cfOptions.DefaultTimeout
      });
    });

    return services;
  }

  #endregion
}
