namespace Cloudflare.NET.Analytics;

/// <summary>
///   Defines the contract for a factory that creates named <see cref="IAnalyticsApi" /> instances. This allows
///   applications to query analytics data from multiple Cloudflare accounts simultaneously.
/// </summary>
/// <remarks>
///   <para>
///     Named clients are registered using the
///     <see
///       cref="ServiceCollectionExtensions.AddCloudflareAnalytics(Microsoft.Extensions.DependencyInjection.IServiceCollection, string)" />
///     overload with a name parameter. The corresponding Cloudflare API options (with API token and GraphQL URL) must also
///     be registered with the same name.
///   </para>
///   <example>
///     <code>
/// // Registration - first register the Cloudflare API options with the name
/// services.AddCloudflareApiClient("account1", options => {
///     options.ApiToken = "token1";
///     options.GraphQlApiUrl = "https://api.cloudflare.com/client/v4/graphql";
/// });
/// services.AddCloudflareAnalytics("account1");
/// 
/// services.AddCloudflareApiClient("account2", options => {
///     options.ApiToken = "token2";
/// });
/// services.AddCloudflareAnalytics("account2");
/// 
/// // Usage via factory
/// public class MyService(IAnalyticsApiFactory factory)
/// {
///     public async Task DoSomething()
///     {
///         var account1Api = factory.CreateClient("account1");
///         var account2Api = factory.CreateClient("account2");
///         // ...
///     }
/// }
/// </code>
///   </example>
/// </remarks>
public interface IAnalyticsApiFactory
{
  #region Methods

  /// <summary>Creates an <see cref="IAnalyticsApi" /> instance configured with the specified named options.</summary>
  /// <param name="name">The name of the client configuration to use. This must match the name used during registration.</param>
  /// <returns>A new <see cref="IAnalyticsApi" /> instance configured with the named options.</returns>
  /// <exception cref="InvalidOperationException">Thrown when no client with the specified name has been registered.</exception>
  IAnalyticsApi CreateClient(string name);

  #endregion
}
