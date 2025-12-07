namespace Cloudflare.NET.Accounts;

using AccessRules;
using Core.Models;
using Models;
using Rulesets;

/// <summary>
///   <para>Defines the contract for interacting with Cloudflare Account resources.</para>
///   <para>
///     This includes managing R2 buckets and their associated domains, as well as account-level security resources
///     like IP Access Rules and WAF Rulesets.
///   </para>
/// </summary>
public interface IAccountsApi
{
  /// <summary>Gets the API for managing account-level IP Access Rules.</summary>
  /// <remarks>Corresponds to the `/accounts/{account_id}/firewall/access_rules/rules` endpoint.</remarks>
  IAccountAccessRulesApi AccessRules { get; }

  /// <summary>Gets the API for managing account-level Rulesets (e.g., WAF Custom Rules).</summary>
  /// <remarks>Corresponds to the `/accounts/{account_id}/rulesets` endpoint family.</remarks>
  IAccountRulesetsApi Rulesets { get; }

  /// <summary>Creates a new R2 bucket within the configured account.</summary>
  /// <param name="bucketName">The desired name for the new bucket. Must be unique.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the <see cref="R2Bucket" /> from
  ///   the Cloudflare API, detailing the created bucket.
  /// </returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/r2/subresources/buckets/methods/create/" />
  Task<R2Bucket> CreateR2BucketAsync(string bucketName, CancellationToken cancellationToken = default);

  /// <summary>Lists R2 buckets in the account, allowing for manual pagination control.</summary>
  /// <param name="filters">Optional filters for pagination.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A single page of R2 buckets along with pagination information.</returns>
  Task<CursorPaginatedResult<R2Bucket>> ListR2BucketsAsync(ListR2BucketsFilters? filters           = null,
                                                           CancellationToken     cancellationToken = default);

  /// <summary>Lists all R2 buckets in the account, automatically handling cursor-based pagination.</summary>
  /// <param name="filters">Optional filters for pagination. The cursor will be ignored.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of all R2 buckets in the account.</returns>
  IAsyncEnumerable<R2Bucket> ListAllR2BucketsAsync(ListR2BucketsFilters? filters = null, CancellationToken cancellationToken = default);

  /// <summary>Disables the public `r2.dev` URL for a given bucket.</summary>
  /// <param name="bucketName">The name of the bucket to modify.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task DisableDevUrlAsync(string bucketName, CancellationToken cancellationToken = default);

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
  Task<CustomDomainResponse> AttachCustomDomainAsync(string            bucketName,
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
  Task<CustomDomainResponse> GetCustomDomainStatusAsync(string bucketName, string hostname, CancellationToken cancellationToken = default);

  /// <summary>Detaches a custom domain from an R2 bucket.</summary>
  /// <param name="bucketName">The name of the bucket.</param>
  /// <param name="hostname">The custom hostname to detach.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task DetachCustomDomainAsync(string bucketName, string hostname, CancellationToken cancellationToken = default);

  /// <summary>Deletes an R2 bucket. The bucket must be empty before it can be deleted.</summary>
  /// <param name="bucketName">The name of the bucket to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task DeleteR2BucketAsync(string bucketName, CancellationToken cancellationToken = default);
}
