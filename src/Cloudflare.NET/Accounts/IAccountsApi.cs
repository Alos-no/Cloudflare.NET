namespace Cloudflare.NET.Accounts;

using Models;

/// <summary>
///   Defines the contract for interacting with Cloudflare Account resources, including R2
///   buckets and custom domains.
/// </summary>
public interface IAccountsApi
{
  /// <summary>Creates a new R2 bucket.</summary>
  /// <param name="bucketName">The desired name for the new bucket.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="CreateBucketResponse" /> from the Cloudflare API.
  /// </returns>
  Task<CreateBucketResponse> CreateR2BucketAsync(string bucketName, CancellationToken cancellationToken = default);

  /// <summary>Disables the public r2.dev URL for a given bucket.</summary>
  /// <param name="bucketName">The name of the bucket to modify.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task DisableDevUrlAsync(string bucketName, CancellationToken cancellationToken = default);

  /// <summary>Attaches a custom domain to an R2 bucket.</summary>
  /// <param name="bucketName">The name of the R2 bucket.</param>
  /// <param name="hostname">The custom hostname to attach.</param>
  /// <param name="zoneId">The ID of the zone the hostname belongs to.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="CustomDomainResponse" /> from the API.
  /// </returns>
  Task<CustomDomainResponse> AttachCustomDomainAsync(string            bucketName,
                                                     string            hostname,
                                                     string            zoneId,
                                                     CancellationToken cancellationToken = default);

  /// <summary>Gets the current status of a custom domain attached to a bucket.</summary>
  /// <param name="bucketName">The name of the R2 bucket.</param>
  /// <param name="hostname">The custom hostname to check.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   <see cref="CustomDomainResponse" /> with the latest status.
  /// </returns>
  Task<CustomDomainResponse> GetCustomDomainStatusAsync(string bucketName, string hostname, CancellationToken cancellationToken = default);

  /// <summary>Detaches a custom domain from an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="hostname">The custom hostname to detach.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task DetachCustomDomainAsync(string bucketName, string hostname, CancellationToken cancellationToken = default);

  /// <summary>Deletes an R2 bucket. The bucket must be empty.</summary>
  /// <param name="bucketName">The name of the bucket to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task DeleteR2BucketAsync(string bucketName, CancellationToken cancellationToken = default);
}
