namespace Cloudflare.NET.Core;

using System.Net;
using Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

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
                          })
                          .AddHttpMessageHandler<AuthenticationHandler>();

    // Add the standard, well-tuned resilience pipeline (timeouts, transient retries, CB).
    // This complements the custom 429 policy we add below.
    builder.AddStandardResilienceHandler();

    // Add a resilience handler that is conditionally configured based on the options.
    builder.AddResilienceHandler("CloudflareRateLimitPolicy", (resilienceBuilder, context) =>
    {
      // Resolve options at the time the pipeline is built.
      var options = context.ServiceProvider.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;

      // Only add a retry strategy if rate limit handling is explicitly enabled.
      if (!options.RateLimiting.IsEnabled)
        return;

      // Resolve a logger once when we build the pipeline.
      var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();
      var logger        = loggerFactory.CreateLogger(LoggingConstants.Categories.HttpRateLimiting);

      // Polly v8: use the generic RetryStrategyOptions<HttpResponseMessage>.
      resilienceBuilder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
      {
        // Retry only on HTTP 429 and only for safe/idempotent request methods.
        ShouldHandle = static args =>
        {
          var response = args.Outcome.Result;

          if (response is null || response.StatusCode != HttpStatusCode.TooManyRequests)
            return ValueTask.FromResult(false);

          var method = response.RequestMessage?.Method;

          var isSafe =
            method == HttpMethod.Get ||
            method == HttpMethod.Head ||
            method == HttpMethod.Options;

          return ValueTask.FromResult(isSafe);
        },

        // Respect configuration.
        MaxRetryAttempts = Math.Max(0, options.RateLimiting.MaxRetries),

        // Exponential backoff with jitter; base delay 1s (used if no Retry-After).
        BackoffType = DelayBackoffType.Exponential,

        Delay = TimeSpan.FromSeconds(1),

        UseJitter = true,

        // Prefer server-provided Retry-After; otherwise fall back to the strategy delay above.
        DelayGenerator = static args =>
        {
          var response = args.Outcome.Result;

          if (response?.Headers.RetryAfter is not { } retryAfter)
            return new ValueTask<TimeSpan?>((TimeSpan?)null);

          if (retryAfter.Delta is { } delta && delta > TimeSpan.Zero)
            return new ValueTask<TimeSpan?>(delta);

          if (retryAfter.Date is not { } date)
            return new ValueTask<TimeSpan?>((TimeSpan?)null);

          var delay = date - DateTimeOffset.UtcNow;

          if (delay > TimeSpan.Zero)
            return new ValueTask<TimeSpan?>(delay);

          // Null => use computed exponential+jitter delay.
          return new ValueTask<TimeSpan?>((TimeSpan?)null);
        },

        OnRetry = args =>
        {
          var req = args.Outcome.Result?.RequestMessage;

          logger.LogWarning(
            "Rate limit (429) for {Method} {Uri}. Attempt {Attempt}/{MaxAttempts}. Next delay: {Delay}.",
            req?.Method,
            req?.RequestUri,
            args.AttemptNumber + 1,
            options.RateLimiting.MaxRetries,
            args.RetryDelay);

          return default;
        }
      });
    });

    return services;
  }

  #endregion
}
