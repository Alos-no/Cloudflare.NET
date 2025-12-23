namespace Cloudflare.NET.R2;

using Cloudflare.NET.Accounts.Models;

/// <summary>
///   Defines the contract for a factory that creates and caches <see cref="IR2Client" /> instances.
///   Supports named configurations for multi-account scenarios and jurisdiction-specific clients
///   for accessing buckets in different geographic regions.
/// </summary>
/// <remarks>
///   <para>
///     All clients created by this factory are cached and reused. The AWS S3 client is thread-safe
///     and designed to be used as a singleton, so caching improves performance by avoiding
///     repeated client construction.
///   </para>
///   <para>
///     <strong>Cache Keys:</strong>
///     <list type="bullet">
///       <item>
///         <description>
///           <c>GetClient(name)</c> - Cached by <c>(name, configured-jurisdiction)</c>
///         </description>
///       </item>
///       <item>
///         <description>
///           <c>GetClient(jurisdiction)</c> - Cached by <c>("", jurisdiction)</c>
///         </description>
///       </item>
///       <item>
///         <description>
///           <c>GetClient(name, jurisdiction)</c> - Cached by <c>(name, jurisdiction)</c>
///         </description>
///       </item>
///     </list>
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Single account, multiple jurisdictions
///   var defaultClient = factory.GetClient(R2Jurisdiction.Default);
///   var euClient = factory.GetClient(R2Jurisdiction.EuropeanUnion);
///
///   // Multiple accounts
///   var prodClient = factory.GetClient("production");
///   var stagingClient = factory.GetClient("staging");
///
///   // Multiple accounts with jurisdiction override
///   var prodEuClient = factory.GetClient("production", R2Jurisdiction.EuropeanUnion);
///   </code>
/// </example>
public interface IR2ClientFactory
{
  #region Methods

  /// <summary>
  ///   Gets or creates a named <see cref="IR2Client" /> instance configured with the specified named options.
  /// </summary>
  /// <param name="name">
  ///   The name of the client configuration to use. Must match the name used during registration
  ///   with
  ///   <see
  ///     cref="ServiceCollectionExtensions.AddCloudflareR2Client(Microsoft.Extensions.DependencyInjection.IServiceCollection, string, System.Action{Configuration.R2Settings})" />
  ///   .
  /// </param>
  /// <returns>A cached <see cref="IR2Client" /> instance configured with the named options.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is null or whitespace.</exception>
  /// <exception cref="Exceptions.CloudflareR2ConfigurationException">
  ///   Thrown when the named configuration is missing or invalid.
  /// </exception>
  /// <remarks>
  ///   <para>
  ///     The client uses the jurisdiction specified in the named configuration's
  ///     <see cref="Configuration.R2Settings.Jurisdiction" />.
  ///     To access a different jurisdiction with the same credentials, use
  ///     <see cref="GetClient(string, R2Jurisdiction)" />.
  ///   </para>
  ///   <para>
  ///     Clients are cached by <c>(name, configured-jurisdiction)</c> and reused for subsequent calls.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Registration
  ///   services.AddCloudflareR2Client("primary", options => {
  ///       options.AccessKeyId = "primary-key";
  ///       options.SecretAccessKey = "primary-secret";
  ///   });
  ///   services.AddCloudflareR2Client("backup", options => {
  ///       options.AccessKeyId = "backup-key";
  ///       options.SecretAccessKey = "backup-secret";
  ///   });
  ///
  ///   // Usage via factory
  ///   public class MyService(IR2ClientFactory factory)
  ///   {
  ///       public async Task DoSomething()
  ///       {
  ///           var primaryClient = factory.GetClient("primary");
  ///           var backupClient = factory.GetClient("backup");
  ///           // ...
  ///       }
  ///   }
  ///   </code>
  /// </example>
  IR2Client GetClient(string name);


  /// <summary>
  ///   Gets an <see cref="IR2Client" /> for the specified jurisdiction using the default (unnamed) credentials.
  /// </summary>
  /// <param name="jurisdiction">The target jurisdiction for R2 operations.</param>
  /// <returns>A cached <see cref="IR2Client" /> instance configured for the specified jurisdiction.</returns>
  /// <exception cref="Exceptions.CloudflareR2ConfigurationException">
  ///   Thrown when the default R2 configuration is missing or invalid.
  /// </exception>
  /// <remarks>
  ///   <para>
  ///     This method uses the credentials from the default (unnamed) R2 configuration registered via
  ///     <see
  ///       cref="ServiceCollectionExtensions.AddCloudflareR2Client(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{Configuration.R2Settings})" />
  ///     .
  ///   </para>
  ///   <para>
  ///     The same R2 credentials work across all jurisdictions within an account; only the S3 endpoint differs.
  ///     Clients are cached by jurisdiction and reused for subsequent calls.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Access buckets in different jurisdictions with the same credentials
  ///   var globalClient = factory.GetClient(R2Jurisdiction.Default);
  ///   var euClient = factory.GetClient(R2Jurisdiction.EuropeanUnion);
  ///   var fedRampClient = factory.GetClient(R2Jurisdiction.FedRamp);
  ///
  ///   // Upload to EU-jurisdictional bucket
  ///   await euClient.UploadAsync("my-eu-bucket", "file.txt", stream);
  ///   </code>
  /// </example>
  IR2Client GetClient(R2Jurisdiction jurisdiction);


  /// <summary>
  ///   Gets an <see cref="IR2Client" /> for the specified jurisdiction using named credentials.
  /// </summary>
  /// <param name="name">
  ///   The name of the client configuration providing credentials. Must match the name used during registration.
  /// </param>
  /// <param name="jurisdiction">The target jurisdiction for R2 operations.</param>
  /// <returns>
  ///   A cached <see cref="IR2Client" /> instance configured with the named credentials and specified jurisdiction.
  /// </returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> is null or whitespace.</exception>
  /// <exception cref="Exceptions.CloudflareR2ConfigurationException">
  ///   Thrown when the named configuration is missing or invalid.
  /// </exception>
  /// <remarks>
  ///   <para>
  ///     Use this method when you have multiple accounts (each with different credentials) and need to access
  ///     buckets in specific jurisdictions within those accounts.
  ///   </para>
  ///   <para>
  ///     Clients are cached by <c>(name, jurisdiction)</c> tuple and reused for subsequent calls.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Production account has buckets in both default and EU jurisdictions
  ///   var prodDefault = factory.GetClient("production", R2Jurisdiction.Default);
  ///   var prodEu = factory.GetClient("production", R2Jurisdiction.EuropeanUnion);
  ///
  ///   // DR account uses FedRAMP jurisdiction
  ///   var drFedRamp = factory.GetClient("disaster-recovery", R2Jurisdiction.FedRamp);
  ///   </code>
  /// </example>
  IR2Client GetClient(string name, R2Jurisdiction jurisdiction);

  #endregion
}
