namespace Cloudflare.NET.Accounts.Buckets;

using Core.Models;
using Models;

/// <summary>
///   <para>Defines the contract for interacting with Cloudflare R2 bucket resources.</para>
///   <para>
///     This API provides operations for managing R2 buckets including creation, deletion, listing,
///     CORS configuration, lifecycle policies, custom domains, managed domains, bucket locks,
///     and Sippy (incremental migration) configuration.
///   </para>
/// </summary>
/// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/" />
public interface IR2BucketsApi
{
  #region Core Bucket Operations

  /// <summary>Creates a new R2 bucket within the configured account with optional location and storage settings.</summary>
  /// <param name="bucketName">The desired name for the new bucket. Must be 3-64 characters and unique.</param>
  /// <param name="locationHint">
  ///   Optional location hint suggesting where the bucket's data should be stored.
  ///   Use <see cref="R2LocationHint" /> constants (e.g., <see cref="R2LocationHint.EastNorthAmerica" />).
  /// </param>
  /// <param name="jurisdiction">
  ///   Optional jurisdictional restriction guaranteeing data residency.
  ///   Use <see cref="R2Jurisdiction" /> constants (e.g., <see cref="R2Jurisdiction.EuropeanUnion" />).
  /// </param>
  /// <param name="storageClass">
  ///   Optional default storage class for new objects.
  ///   Use <see cref="R2StorageClass" /> constants (e.g., <see cref="R2StorageClass.InfrequentAccess" />).
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the <see cref="R2Bucket" /> from
  ///   the Cloudflare API, detailing the created bucket.
  /// </returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/methods/create/" />
  Task<R2Bucket> CreateAsync(
    string            bucketName,
    R2LocationHint?   locationHint      = null,
    R2Jurisdiction?   jurisdiction      = null,
    R2StorageClass?   storageClass      = null,
    CancellationToken cancellationToken = default);

  /// <summary>Gets the properties of an existing R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket to retrieve (3-64 characters).</param>
  /// <param name="jurisdiction">
  ///   Optional jurisdictional restriction for buckets with data residency requirements.
  ///   Use <see cref="R2Jurisdiction" /> constants.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the <see cref="R2Bucket" />
  ///   with the bucket's properties.
  /// </returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/methods/get/" />
  Task<R2Bucket> GetAsync(
    string            bucketName,
    R2Jurisdiction?   jurisdiction      = null,
    CancellationToken cancellationToken = default);

  /// <summary>Lists R2 buckets in the account, allowing for manual pagination control.</summary>
  /// <param name="filters">Optional filters for pagination.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A single page of R2 buckets along with pagination information.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/methods/list/" />
  Task<CursorPaginatedResult<R2Bucket>> ListAsync(
    ListR2BucketsFilters? filters           = null,
    CancellationToken     cancellationToken = default);

  /// <summary>Lists all R2 buckets in the account, automatically handling cursor-based pagination.</summary>
  /// <param name="filters">Optional filters for pagination. The cursor will be ignored.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of all R2 buckets in the account.</returns>
  IAsyncEnumerable<R2Bucket> ListAllAsync(
    ListR2BucketsFilters? filters           = null,
    CancellationToken     cancellationToken = default);

  /// <summary>Deletes an R2 bucket. The bucket must be empty before it can be deleted.</summary>
  /// <param name="bucketName">The name of the bucket to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/methods/delete/" />
  Task DeleteAsync(string bucketName, CancellationToken cancellationToken = default);

  #endregion


  #region CORS Configuration

  /// <summary>Gets the CORS policy for an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="BucketCorsPolicy" /> with the current CORS rules.
  /// </returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/subresources/cors/methods/get/" />
  Task<BucketCorsPolicy> GetCorsAsync(string bucketName, CancellationToken cancellationToken = default);

  /// <summary>Sets or updates the CORS policy for an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="corsPolicy">The CORS policy to apply to the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/subresources/cors/methods/update/" />
  Task SetCorsAsync(string bucketName, BucketCorsPolicy corsPolicy, CancellationToken cancellationToken = default);

  /// <summary>Deletes the CORS policy for an R2 bucket, removing all CORS rules.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/subresources/cors/methods/delete/" />
  Task DeleteCorsAsync(string bucketName, CancellationToken cancellationToken = default);

  #endregion


  #region Lifecycle Configuration

  /// <summary>Gets the lifecycle policy for an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="BucketLifecyclePolicy" /> with the current lifecycle rules.
  /// </returns>
  /// <remarks>
  ///   Lifecycle rules determine object retention, automatic deletion, storage class transitions, and cleanup of
  ///   incomplete multipart uploads. A bucket can have up to 1000 lifecycle rules.
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/subresources/lifecycle/methods/get/" />
  Task<BucketLifecyclePolicy> GetLifecycleAsync(string bucketName, CancellationToken cancellationToken = default);

  /// <summary>Sets or updates the lifecycle policy for an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="lifecyclePolicy">The lifecycle policy to apply to the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  /// <remarks>
  ///   <para>Lifecycle rules can perform the following actions:</para>
  ///   <list type="bullet">
  ///     <item>
  ///       <description>Delete objects after a specified age or on a specific date</description>
  ///     </item>
  ///     <item>
  ///       <description>Abort incomplete multipart uploads after a specified age</description>
  ///     </item>
  ///     <item>
  ///       <description>Transition objects to Infrequent Access storage class</description>
  ///     </item>
  ///   </list>
  ///   <para>Rules are processed within 24 hours of being set. Maximum of 1000 rules per bucket.</para>
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/subresources/lifecycle/methods/update/" />
  Task SetLifecycleAsync(
    string                bucketName,
    BucketLifecyclePolicy lifecyclePolicy,
    CancellationToken     cancellationToken = default);

  /// <summary>Deletes the lifecycle policy for an R2 bucket, removing all lifecycle rules.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  /// <remarks>
  ///   Cloudflare R2 does not have a dedicated DELETE endpoint for lifecycle policies. This method removes the policy
  ///   by setting an empty rules array.
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/r2/buckets/object-lifecycles/" />
  Task DeleteLifecycleAsync(string bucketName, CancellationToken cancellationToken = default);

  #endregion


  #region Custom Domain Configuration

  /// <summary>Lists all custom domains attached to an R2 bucket.</summary>
  /// <param name="bucketName">The name of the R2 bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains a list of
  ///   <see cref="CustomDomain" /> objects representing all custom domains for the bucket.
  /// </returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/subresources/domains/" />
  Task<IReadOnlyList<CustomDomain>> ListCustomDomainsAsync(
    string            bucketName,
    CancellationToken cancellationToken = default);

  /// <summary>Attaches a custom domain to an R2 bucket, allowing it to be served from a custom hostname.</summary>
  /// <param name="bucketName">The name of the R2 bucket.</param>
  /// <param name="hostname">The custom hostname to attach (e.g., "files.example.com").</param>
  /// <param name="zoneId">The ID of the zone the hostname belongs to.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="CustomDomainResponse" /> from the API.
  /// </returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/subresources/domains/" />
  Task<CustomDomainResponse> AttachCustomDomainAsync(
    string            bucketName,
    string            hostname,
    string            zoneId,
    CancellationToken cancellationToken = default);

  /// <summary>Gets the current status of a custom domain attached to a bucket, including ownership and SSL status.</summary>
  /// <param name="bucketName">The name of the R2 bucket.</param>
  /// <param name="hostname">The custom hostname to check.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="CustomDomainResponse" /> with the latest status.
  /// </returns>
  Task<CustomDomainResponse> GetCustomDomainStatusAsync(
    string            bucketName,
    string            hostname,
    CancellationToken cancellationToken = default);

  /// <summary>Updates the configuration of a custom domain attached to a bucket.</summary>
  /// <param name="bucketName">The name of the R2 bucket.</param>
  /// <param name="hostname">The custom hostname to update.</param>
  /// <param name="request">The update request with new domain settings.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="CustomDomainResponse" /> with the updated configuration.
  /// </returns>
  Task<CustomDomainResponse> UpdateCustomDomainAsync(
    string                    bucketName,
    string                    hostname,
    UpdateCustomDomainRequest request,
    CancellationToken         cancellationToken = default);

  /// <summary>Detaches a custom domain from an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="hostname">The custom hostname to detach.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task DetachCustomDomainAsync(
    string            bucketName,
    string            hostname,
    CancellationToken cancellationToken = default);

  #endregion


  #region Managed Domain (r2.dev) Configuration

  /// <summary>Gets the current state of public access over the bucket's R2-managed (r2.dev) domain.</summary>
  /// <param name="bucketName">The name of the R2 bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="ManagedDomainResponse" /> with the r2.dev domain status.
  /// </returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/subresources/domains/" />
  Task<ManagedDomainResponse> GetManagedDomainAsync(
    string            bucketName,
    CancellationToken cancellationToken = default);

  /// <summary>Enables the public r2.dev URL for a given bucket.</summary>
  /// <param name="bucketName">The name of the bucket to modify.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="ManagedDomainResponse" /> with the updated status.
  /// </returns>
  Task<ManagedDomainResponse> EnableManagedDomainAsync(
    string            bucketName,
    CancellationToken cancellationToken = default);

  /// <summary>Disables the public r2.dev URL for a given bucket.</summary>
  /// <param name="bucketName">The name of the bucket to modify.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task DisableManagedDomainAsync(string bucketName, CancellationToken cancellationToken = default);

  #endregion


  #region Bucket Lock Configuration

  /// <summary>Gets the lock rules for an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="BucketLockPolicy" /> with the current lock rules.
  /// </returns>
  /// <remarks>
  ///   Bucket locks prevent the deletion and overwriting of objects for a specified period or indefinitely.
  ///   If multiple rules apply to the same prefix or object key, the strictest retention requirement takes precedence.
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/r2/buckets/bucket-locks/" />
  Task<BucketLockPolicy> GetLockAsync(string bucketName, CancellationToken cancellationToken = default);

  /// <summary>Sets or updates the lock rules for an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="lockPolicy">The lock policy to apply to the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="BucketLockPolicy" /> with the applied lock rules.
  /// </returns>
  /// <remarks>
  ///   <para>Lock rules can have the following condition types:</para>
  ///   <list type="bullet">
  ///     <item>
  ///       <description>Age-based: Retention for a specific duration in seconds</description>
  ///     </item>
  ///     <item>
  ///       <description>Date-based: Retention until a specific date</description>
  ///     </item>
  ///     <item>
  ///       <description>Indefinite: Permanent locking without expiration</description>
  ///     </item>
  ///   </list>
  ///   <para>Maximum of 1000 lock rules per bucket configuration.</para>
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/r2/buckets/bucket-locks/" />
  Task<BucketLockPolicy> SetLockAsync(
    string           bucketName,
    BucketLockPolicy lockPolicy,
    CancellationToken cancellationToken = default);

  /// <summary>Deletes all lock rules for an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  /// <remarks>
  ///   This method removes the lock policy by setting an empty rules array, similar to lifecycle policy deletion.
  /// </remarks>
  Task DeleteLockAsync(string bucketName, CancellationToken cancellationToken = default);

  #endregion


  #region Sippy (Incremental Migration) Configuration

  /// <summary>Gets the Sippy (incremental migration) configuration for an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="SippyConfig" /> with the current Sippy configuration.
  /// </returns>
  /// <remarks>
  ///   Sippy is an incremental migration service that copies data from cloud storage providers (AWS S3, GCS)
  ///   to R2 as it's requested, helping avoid upfront egress fees during migration.
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/r2/data-migration/sippy/" />
  Task<SippyConfig> GetSippyAsync(string bucketName, CancellationToken cancellationToken = default);

  /// <summary>Enables or updates Sippy (incremental migration) for an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="request">The Sippy configuration to apply.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="SippyConfig" /> with the applied configuration.
  /// </returns>
  /// <remarks>
  ///   <para>Sippy supports migration from:</para>
  ///   <list type="bullet">
  ///     <item><description>Amazon S3</description></item>
  ///     <item><description>Google Cloud Storage (GCS)</description></item>
  ///   </list>
  ///   <para>
  ///     When enabled, objects not found in R2 are simultaneously returned from the source bucket
  ///     and copied to R2 for future requests.
  ///   </para>
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/r2/data-migration/sippy/" />
  Task<SippyConfig> EnableSippyAsync(
    string              bucketName,
    EnableSippyRequest  request,
    CancellationToken   cancellationToken = default);

  /// <summary>Disables Sippy (incremental migration) for an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  /// <seealso href="https://developers.cloudflare.com/r2/data-migration/sippy/" />
  Task DisableSippyAsync(string bucketName, CancellationToken cancellationToken = default);

  #endregion


  #region Temporary Credentials

  /// <summary>
  ///   Creates temporary access credentials for R2 that can be optionally scoped to specific buckets or prefixes.
  /// </summary>
  /// <param name="request">The request specifying the credentials to create.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="TempCredentials" /> with the temporary access credentials.
  /// </returns>
  /// <remarks>
  ///   Temporary credentials are useful for granting limited, time-bound access to R2 resources
  ///   without exposing your main API credentials. The credentials include an access key ID,
  ///   secret access key, and session token.
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/temporary_credentials/" />
  Task<TempCredentials> CreateTempCredentialsAsync(
    CreateTempCredentialsRequest request,
    CancellationToken            cancellationToken = default);

  #endregion
}
