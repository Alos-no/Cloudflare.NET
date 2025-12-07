namespace Cloudflare.NET;

using Accounts;
using Zones;

/// <summary>
///   Defines the contract for the primary client for interacting with the Cloudflare API. This client acts as a
///   facade, providing access to different API resource areas such as Accounts and Zones.
/// </summary>
/// <remarks>
///   <para>
///     This is the main entry point for the SDK. It should be registered with a dependency injection container as a
///     transient service using the <c>AddCloudflareApiClient()</c> extension method.
///   </para>
///   <example>
///     Inject this interface into your services to access all Cloudflare APIs:
///     <code>
/// public class MyService(ICloudflareApiClient cf)
/// {
///     public async Task DoSomething()
///     {
///         var zones = await cf.Zones.ListZonesAsync();
///         // ...
///     }
/// }
/// </code>
///   </example>
/// </remarks>
public interface ICloudflareApiClient
{
  #region Properties & Fields - Public

  /// <summary>
  ///   Gets the API resource for managing Account-level resources. This includes R2 buckets, account-wide IP Access
  ///   Rules, and account-level WAF custom rulesets.
  /// </summary>
  IAccountsApi Accounts { get; }

  /// <summary>
  ///   Gets the API resource for managing Zone-level resources. This includes DNS records, zone-specific IP Access
  ///   Rules, WAF configurations, and other settings scoped to a specific zone.
  /// </summary>
  IZonesApi Zones { get; }

  #endregion
}
