namespace Cloudflare.NET.Core;

/// <summary>
///   Defines the contract for a factory that creates named <see cref="ICloudflareApiClient" /> instances. This
///   allows applications to work with multiple Cloudflare accounts or configurations simultaneously.
/// </summary>
/// <remarks>
///   <para>
///     Named clients are registered using the
///     <see
///       cref="ServiceCollectionExtensions.AddCloudflareApiClient(Microsoft.Extensions.DependencyInjection.IServiceCollection, string, System.Action{CloudflareApiOptions})" />
///     overload with a name parameter.
///   </para>
///   <example>
///     <code>
/// // Registration
/// services.AddCloudflareApiClient("production", options => {
///     options.ApiToken = "prod-token";
///     options.AccountId = "prod-account-id";
/// });
/// services.AddCloudflareApiClient("staging", options => {
///     options.ApiToken = "staging-token";
///     options.AccountId = "staging-account-id";
/// });
/// 
/// // Usage via factory
/// public class MyService(ICloudflareApiClientFactory factory)
/// {
///     public async Task DoSomething()
///     {
///         var prodClient = factory.CreateClient("production");
///         var stagingClient = factory.CreateClient("staging");
///         // ...
///     }
/// }
/// </code>
///   </example>
/// </remarks>
public interface ICloudflareApiClientFactory
{
  #region Methods

  /// <summary>Creates an <see cref="ICloudflareApiClient" /> instance configured with the specified named options.</summary>
  /// <param name="name">The name of the client configuration to use. This must match the name used during registration.</param>
  /// <returns>A new <see cref="ICloudflareApiClient" /> instance configured with the named options.</returns>
  /// <exception cref="InvalidOperationException">Thrown when no client with the specified name has been registered.</exception>
  ICloudflareApiClient CreateClient(string name);


  /// <summary>
  ///   Creates an <see cref="ICloudflareApiClient" /> instance dynamically from the provided options,
  ///   without requiring pre-registration in the DI container.
  /// </summary>
  /// <param name="options">The configuration options for the client.</param>
  /// <returns>
  ///   A fully configured <see cref="ICloudflareApiClient" /> with authentication and resilience
  ///   (rate limiting, retries, circuit breaker, timeouts).
  /// </returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is null.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the options fail validation (e.g., missing ApiToken).</exception>
  /// <remarks>
  ///   <para>
  ///     Use this method when client configurations are not known at application startup,
  ///     such as when users can add Cloudflare accounts at runtime through a UI.
  ///   </para>
  ///   <para>
  ///     The returned client manages its own <see cref="HttpClient" /> instance and should be disposed
  ///     when no longer needed to release resources. Use a <c>using</c> statement or call <see cref="IDisposable.Dispose" />:
  ///   </para>
  ///   <code>
  /// using var client = factory.CreateClient(options);
  /// var zones = await client.Zones.ListZonesAsync();
  /// // Client is disposed when the using scope ends
  /// </code>
  ///   <para>
  ///     Each dynamic client has its own isolated resilience pipeline (rate limiter, circuit breaker, etc.).
  ///     Dynamic clients do not share state with pre-registered named clients or other dynamic clients.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  /// // Create a dynamic client for a user-provided account
  /// var options = new CloudflareApiOptions
  /// {
  ///     ApiToken = userProvidedToken,
  ///     AccountId = userProvidedAccountId,
  ///     RateLimiting = new RateLimitingOptions
  ///     {
  ///         IsEnabled = true,
  ///         PermitLimit = 10  // Conservative limit for user accounts
  ///     }
  /// };
  ///
  /// var client = factory.CreateClient(options);
  /// </code>
  /// </example>
  ICloudflareApiClient CreateClient(CloudflareApiOptions options);

  #endregion
}
