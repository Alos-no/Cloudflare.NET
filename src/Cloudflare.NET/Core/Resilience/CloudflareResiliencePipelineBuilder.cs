namespace Cloudflare.NET.Core.Resilience;

using System.Net;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using RateLimitHeaders.Polly;

/// <summary>
///   Builds and configures resilience pipelines for Cloudflare API clients.
///   Used by both DI registration and dynamic client creation paths to ensure
///   consistent resilience behavior.
/// </summary>
/// <remarks>
///   <para>
///     The resilience pipeline includes the following strategies (outermost to innermost):
///   </para>
///   <list type="number">
///     <item>
///       <description>
///         <b>Rate Limiter</b> - Client-side concurrency limiting to prevent overwhelming the API.
///       </description>
///     </item>
///     <item>
///       <description>
///         <b>Rate Limit Headers</b> - Proactive throttling based on server-returned rate limit headers.
///       </description>
///     </item>
///     <item>
///       <description>
///         <b>Total Request Timeout</b> - Overall timeout (60 seconds) covering all retries.
///       </description>
///     </item>
///     <item>
///       <description>
///         <b>Retry Strategy</b> - Exponential backoff with jitter for transient failures
///         (only for idempotent HTTP methods).
///       </description>
///     </item>
///     <item>
///       <description>
///         <b>Circuit Breaker</b> - Stops sending requests after consecutive failures.
///       </description>
///     </item>
///     <item>
///       <description>
///         <b>Attempt Timeout</b> - Per-request timeout from <see cref="CloudflareApiOptions.DefaultTimeout" />.
///       </description>
///     </item>
///   </list>
/// </remarks>
public static class CloudflareResiliencePipelineBuilder
{
  #region Constants

  /// <summary>
  ///   The total timeout for all retries combined. This is the outermost timeout
  ///   that limits how long the entire operation (including retries) can take.
  /// </summary>
  private static readonly TimeSpan TotalRequestTimeout = TimeSpan.FromSeconds(60);

  /// <summary>The initial delay between retry attempts before jitter is applied.</summary>
  private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromSeconds(1);

  #endregion


  #region Methods - Public

  /// <summary>
  ///   Configures an existing resilience pipeline builder with Cloudflare-specific strategies
  ///   (rate limiting, retries, circuit breaker, timeouts).
  /// </summary>
  /// <param name="builder">The resilience pipeline builder to configure.</param>
  /// <param name="options">The Cloudflare API options containing resilience configuration.</param>
  /// <param name="logger">Optional logger for resilience events. If null, events are not logged.</param>
  /// <param name="clientName">Optional client name for logging context and strategy naming.</param>
  /// <remarks>
  ///   <para>
  ///     This method is used by the DI registration path via <c>AddResilienceHandler()</c>.
  ///     The same configuration logic is used by <see cref="Build" /> for dynamic clients.
  ///   </para>
  /// </remarks>
  public static void Configure(ResiliencePipelineBuilder<HttpResponseMessage> builder,
                               CloudflareApiOptions                           options,
                               ILogger?                                       logger     = null,
                               string?                                        clientName = null)
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(options);

    // Create name prefix for this specific client's resilience components.
    var namePrefix = clientName is null ? "Cloudflare" : $"Cloudflare:{clientName}";

    // The standard pipeline order is:
    // Rate Limiter -> Rate Limit Headers -> Total Timeout -> Retry -> Circuit Breaker -> Attempt Timeout.
    // Ref: https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience#standard-pipeline

    ConfigureRateLimiter(builder, options, logger, namePrefix);
    ConfigureRateLimitHeaders(builder, options);
    ConfigureTotalTimeout(builder, namePrefix);
    ConfigureRetry(builder, options, logger, namePrefix);
    ConfigureCircuitBreaker(builder, namePrefix);
    ConfigureAttemptTimeout(builder, options, namePrefix);
  }


  /// <summary>
  ///   Builds a standalone resilience pipeline configured for Cloudflare API calls.
  /// </summary>
  /// <param name="options">The Cloudflare API options containing resilience configuration.</param>
  /// <param name="logger">Optional logger for resilience events. If null, events are not logged.</param>
  /// <param name="clientName">Optional client name for logging context and strategy naming.</param>
  /// <returns>
  ///   A configured <see cref="ResiliencePipeline{HttpResponseMessage}" /> ready for use
  ///   with an <see cref="HttpClient" />.
  /// </returns>
  /// <remarks>
  ///   <para>
  ///     This method is used by the dynamic client creation path to build a resilience pipeline
  ///     without requiring DI registration. The pipeline can be used with a
  ///     <see cref="ResilienceDelegatingHandler" /> to wrap an <see cref="HttpClient" />.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  /// var pipeline = CloudflareResiliencePipelineBuilder.Build(options, logger);
  /// var handler = new ResilienceDelegatingHandler(pipeline, new SocketsHttpHandler());
  /// var httpClient = new HttpClient(handler);
  /// </code>
  /// </example>
  public static ResiliencePipeline<HttpResponseMessage> Build(CloudflareApiOptions options,
                                                              ILogger?             logger     = null,
                                                              string?              clientName = null)
  {
    ArgumentNullException.ThrowIfNull(options);

    var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

    Configure(builder, options, logger, clientName);

    return builder.Build();
  }

  #endregion


  #region Methods - Private

  /// <summary>
  ///   Configures the client-side rate limiter strategy.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This is the OUTERMOST strategy. It limits the number of concurrent requests
  ///     to prevent overwhelming the Cloudflare API.
  ///   </para>
  /// </remarks>
  private static void ConfigureRateLimiter(ResiliencePipelineBuilder<HttpResponseMessage> builder,
                                           CloudflareApiOptions                           options,
                                           ILogger?                                       logger,
                                           string                                         namePrefix)
  {
    var rateLimiterName = $"{namePrefix}:RateLimiter";

    builder.AddRateLimiter(new HttpRateLimiterStrategyOptions
    {
      Name = rateLimiterName,
      DefaultRateLimiterOptions = new ConcurrencyLimiterOptions
      {
        PermitLimit          = Math.Max(1, options.RateLimiting.PermitLimit),
        QueueLimit           = Math.Max(0, options.RateLimiting.QueueLimit),
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
      },
      OnRejected = args =>
      {
        if (logger is null)
          return default;

        // Try to extract a Retry-After hint if the limiter can calculate one.
        args.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter);

        logger.LogWarning(
          "Request rejected by {Strategy}. Concurrency limit reached. RetryAfter={RetryAfter}.",
          rateLimiterName, retryAfter == default ? "n/a" : $"{(int)retryAfter.TotalSeconds}s");

        return default;
      }
    });
  }


  /// <summary>
  ///   Configures proactive throttling based on server-returned rate limit headers.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This processes headers like RateLimit-Remaining, RateLimit-Limit, and RateLimit-Reset
  ///     to preemptively slow down requests before hitting 429 responses.
  ///   </para>
  /// </remarks>
  private static void ConfigureRateLimitHeaders(ResiliencePipelineBuilder<HttpResponseMessage> builder,
                                                CloudflareApiOptions                           options)
  {
    // Ref: https://alos.no/ratelimitheaders/articles/polly-integration.html
    builder.AddRateLimitHeaders(rateLimitOptions =>
    {
      // Enable proactive throttling to delay requests when quota is running low.
      rateLimitOptions.EnableProactiveThrottling = options.RateLimiting.EnableProactiveThrottling;

      // Set the threshold at which throttling begins (e.g., 0.1 = 10% remaining).
      rateLimitOptions.QuotaLowThreshold = options.RateLimiting.QuotaLowThreshold;
    });
  }


  /// <summary>
  ///   Configures the total request timeout covering all retries.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This is an outer timeout that limits how long the entire operation (including all retry
  ///     attempts) can take. It prevents unbounded retry loops.
  ///   </para>
  /// </remarks>
  private static void ConfigureTotalTimeout(ResiliencePipelineBuilder<HttpResponseMessage> builder,
                                            string                                         namePrefix)
  {
    builder.AddTimeout(new HttpTimeoutStrategyOptions
    {
      Name    = $"{namePrefix}:TotalTimeout",
      Timeout = TotalRequestTimeout
    });
  }


  /// <summary>
  ///   Configures the retry strategy for transient failures.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Only idempotent HTTP methods (GET, HEAD, OPTIONS, TRACE, PUT, DELETE) are retried.
  ///     Non-idempotent methods (POST, PATCH) are not retried to prevent duplicate side effects.
  ///   </para>
  ///   <para>
  ///     Retries are performed with exponential backoff and jitter. The strategy honors
  ///     the Retry-After header when present.
  ///   </para>
  /// </remarks>
  private static void ConfigureRetry(ResiliencePipelineBuilder<HttpResponseMessage> builder,
                                     CloudflareApiOptions                           options,
                                     ILogger?                                       logger,
                                     string                                         namePrefix)
  {
    // Polly v8 validates MaxRetryAttempts >= 1. If users set MaxRetries = 0 to disable retries,
    // we skip adding the retry component entirely to avoid validation failures.
    if (options.RateLimiting.MaxRetries <= 0)
      return;

    var retryOptions = new HttpRetryStrategyOptions
    {
      // Defaults handle 5xx, 408, 429, HttpRequestException, and TimeoutRejectedException.
      // Ref: https://learn.microsoft.com/en-us/dotnet/api/polly.extensions.http.httpretrystrategyoptions
      Name             = $"{namePrefix}:Retry",
      BackoffType      = DelayBackoffType.Exponential,
      Delay            = RetryBaseDelay,
      UseJitter        = true,
      MaxRetryAttempts = options.RateLimiting.MaxRetries,

      // By default, HttpRetryStrategyOptions honors the 'Retry-After' header. A custom generator is not needed.
      // Ref: https://devblogs.microsoft.com/dotnet/building-resilient-cloud-services-with-dotnet-8/

      // This custom predicate ensures we always retry on server errors (5xx) and transient exceptions,
      // but only retry on 429s if rate limit handling is explicitly enabled in options.
      ShouldHandle = args => ShouldRetry(args, options),
      OnRetry      = args => OnRetry(args, options, logger)
    };

    // IMPORTANT: keep DisableForUnsafeHttpMethods removed so DELETE/PUT can retry when appropriate.
    // retryOptions.DisableForUnsafeHttpMethods();

    builder.AddRetry(retryOptions);
  }


  /// <summary>
  ///   Determines whether a failed request should be retried based on the outcome and options.
  /// </summary>
  /// <param name="args">The retry predicate arguments containing the outcome.</param>
  /// <param name="options">The Cloudflare API options.</param>
  /// <returns>True if the request should be retried; otherwise, false.</returns>
  private static ValueTask<bool> ShouldRetry(RetryPredicateArguments<HttpResponseMessage> args,
                                             CloudflareApiOptions                         options)
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
      return new ValueTask<bool>(options.RateLimiting.IsEnabled);

    return new ValueTask<bool>(false);
  }


  /// <summary>
  ///   Handles the retry event by logging the retry attempt.
  /// </summary>
  private static ValueTask OnRetry(OnRetryArguments<HttpResponseMessage> args,
                                   CloudflareApiOptions                  options,
                                   ILogger?                              logger)
  {
    if (logger is null)
      return default;

    var req = args.Outcome.Result?.RequestMessage;

    logger.LogWarning(
      "Transient failure for {Method} {Uri}. Attempt {Attempt}/{MaxAttempts}. Next delay: {Delay}.",
      req?.Method,
      req?.RequestUri,
      args.AttemptNumber + 1,
      options.RateLimiting.MaxRetries,
      args.RetryDelay);

    return default;
  }


  /// <summary>
  ///   Configures the circuit breaker strategy.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The circuit breaker stops sending requests after too many consecutive failures,
  ///     allowing the downstream service time to recover.
  ///   </para>
  /// </remarks>
  private static void ConfigureCircuitBreaker(ResiliencePipelineBuilder<HttpResponseMessage> builder,
                                              string                                         namePrefix)
  {
    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
      Name = $"{namePrefix}:CircuitBreaker"
    });
  }


  /// <summary>
  ///   Configures the per-attempt timeout.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This is the INNERMOST strategy. It limits how long each individual attempt
  ///     (before any retry) can take. The timeout value comes from
  ///     <see cref="CloudflareApiOptions.DefaultTimeout" />.
  ///   </para>
  /// </remarks>
  private static void ConfigureAttemptTimeout(ResiliencePipelineBuilder<HttpResponseMessage> builder,
                                              CloudflareApiOptions                           options,
                                              string                                         namePrefix)
  {
    builder.AddTimeout(new HttpTimeoutStrategyOptions
    {
      Name    = $"{namePrefix}:AttemptTimeout",
      Timeout = options.DefaultTimeout
    });
  }

  #endregion
}
