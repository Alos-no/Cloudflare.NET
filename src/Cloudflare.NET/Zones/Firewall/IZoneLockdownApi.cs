namespace Cloudflare.NET.Zones.Firewall;

using Core.Models;
using Security.Firewall.Models;

/// <summary>
///   <para>Defines the contract for managing Zone Lockdown rules.</para>
///   <para>
///     Lockdown rules specify a list of IP addresses that are the only ones allowed to access
///     a given URL or set of URLs.
///   </para>
/// </summary>
public interface IZoneLockdownApi
{
  /// <summary>Lists all Lockdown rules for a zone, allowing for manual pagination.</summary>
  /// <remarks>
  ///   This method is intended for developers who need to control the pagination process
  ///   manually. Use the properties of the returned <see cref="PagePaginatedResult{T}" /> to
  ///   determine if there are more pages and to construct the filter for the next call.
  /// </remarks>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="filters">Optional pagination filters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A single page of Lockdown rules along with pagination information.</returns>
  Task<PagePaginatedResult<Lockdown>> ListAsync(string               zoneId,
                                                ListLockdownFilters? filters           = null,
                                                CancellationToken    cancellationToken = default);

  /// <summary>Lists all Lockdown rules for a zone, automatically handling pagination.</summary>
  /// <remarks>This method simplifies fetching all rules by abstracting away the pagination logic.</remarks>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="perPage">The number of results to fetch per API page.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of all Lockdown rules for the zone.</returns>
  IAsyncEnumerable<Lockdown> ListAllAsync(string zoneId, int? perPage = null, CancellationToken cancellationToken = default);

  /// <summary>Gets a single Lockdown rule by its ID.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="lockdownId">The ID of the Lockdown rule.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The requested Lockdown rule.</returns>
  Task<Lockdown> GetAsync(string zoneId, string lockdownId, CancellationToken cancellationToken = default);

  /// <summary>Creates a new Lockdown rule for a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="request">The request containing the details of the rule to create.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created Lockdown rule.</returns>
  Task<Lockdown> CreateAsync(string zoneId, CreateLockdownRequest request, CancellationToken cancellationToken = default);

  /// <summary>Updates an existing Lockdown rule.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="lockdownId">The ID of the rule to update.</param>
  /// <param name="request">The request containing the fields to update.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated Lockdown rule.</returns>
  Task<Lockdown> UpdateAsync(string                zoneId,
                             string                lockdownId,
                             UpdateLockdownRequest request,
                             CancellationToken     cancellationToken = default);

  /// <summary>Deletes a Lockdown rule from a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="lockdownId">The ID of the rule to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The ID of the deleted rule.</returns>
  Task<string> DeleteAsync(string zoneId, string lockdownId, CancellationToken cancellationToken = default);
}
