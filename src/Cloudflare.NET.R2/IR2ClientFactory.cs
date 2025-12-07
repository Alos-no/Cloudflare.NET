namespace Cloudflare.NET.R2;

/// <summary>
///   Defines the contract for a factory that creates named <see cref="IR2Client" /> instances. This allows
///   applications to work with multiple R2 configurations simultaneously, such as different buckets in different accounts.
/// </summary>
/// <remarks>
///   <para>
///     Named clients are registered using the
///     <see
///       cref="ServiceCollectionExtensions.AddCloudflareR2Client(Microsoft.Extensions.DependencyInjection.IServiceCollection, string, System.Action{Configuration.R2Settings})" />
///     overload with a name parameter.
///   </para>
///   <example>
///     <code>
/// // Registration
/// services.AddCloudflareR2Client("primary", options => {
///     options.AccessKeyId = "primary-key";
///     options.SecretAccessKey = "primary-secret";
/// });
/// services.AddCloudflareR2Client("backup", options => {
///     options.AccessKeyId = "backup-key";
///     options.SecretAccessKey = "backup-secret";
/// });
/// 
/// // Usage via factory
/// public class MyService(IR2ClientFactory factory)
/// {
///     public async Task DoSomething()
///     {
///         var primaryClient = factory.CreateClient("primary");
///         var backupClient = factory.CreateClient("backup");
///         // ...
///     }
/// }
/// </code>
///   </example>
/// </remarks>
public interface IR2ClientFactory
{
  #region Methods

  /// <summary>Creates an <see cref="IR2Client" /> instance configured with the specified named options.</summary>
  /// <param name="name">The name of the client configuration to use. This must match the name used during registration.</param>
  /// <returns>A new <see cref="IR2Client" /> instance configured with the named options.</returns>
  /// <exception cref="InvalidOperationException">Thrown when no client with the specified name has been registered.</exception>
  /// <remarks>
  ///   <para>
  ///     The returned client should be cached or reused where possible, as creating new clients incurs overhead from
  ///     establishing S3 connections.
  ///   </para>
  /// </remarks>
  IR2Client CreateClient(string name);

  #endregion
}
