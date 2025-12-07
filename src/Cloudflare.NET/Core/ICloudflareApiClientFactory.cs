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

  #endregion
}
