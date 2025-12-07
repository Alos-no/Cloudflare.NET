namespace Cloudflare.NET.Zones.AccessRules;

using Core.Models;
using Security.Firewall.Models;

/// <summary>
///   <para>Defines the contract for managing IP Access Rules at the zone level.</para>
///   <para>Zone-level rules apply only to traffic for a specific zone.</para>
/// </summary>
public interface IZoneAccessRulesApi
{
  /// <summary>Lists all IP Access Rules for the specified zone, allowing for manual pagination control.</summary>
  /// <remarks>
  ///   This method is intended for developers who need to control the pagination process manually. Use the properties
  ///   of the returned <see cref="PagePaginatedResult{T}" /> to determine if there are more pages and to construct the
  ///   filter for the next call.
  /// </remarks>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="filters">Optional filters to apply to the list operation, including pagination parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A single page of access rules along with pagination information.</returns>
  Task<PagePaginatedResult<AccessRule>> ListAsync(string                  zoneId,
                                                  ListAccessRulesFilters? filters           = null,
                                                  CancellationToken       cancellationToken = default);

  /// <summary>Lists all IP Access Rules for the specified zone, automatically handling pagination.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="filters">
  ///   Optional filters to apply to the list operation. Pagination parameters (Page, PerPage) will be
  ///   ignored.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of all access rules matching the criteria.</returns>
  IAsyncEnumerable<AccessRule> ListAllAsync(string                  zoneId,
                                            ListAccessRulesFilters? filters           = null,
                                            CancellationToken       cancellationToken = default);

  /// <summary>Gets a single IP Access Rule for a zone by its ID.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="ruleId">The ID of the rule to retrieve.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The requested access rule.</returns>
  Task<AccessRule> GetAsync(string zoneId, string ruleId, CancellationToken cancellationToken = default);

  /// <summary>Creates a new IP Access Rule for the specified zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="request">The request containing the details of the rule to create.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created access rule.</returns>
  Task<AccessRule> CreateAsync(string zoneId, CreateAccessRuleRequest request, CancellationToken cancellationToken = default);

  /// <summary>Updates an existing IP Access Rule for the specified zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="ruleId">The ID of the rule to update.</param>
  /// <param name="request">The request containing the fields to update.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated access rule.</returns>
  Task<AccessRule> UpdateAsync(string                  zoneId,
                               string                  ruleId,
                               UpdateAccessRuleRequest request,
                               CancellationToken       cancellationToken = default);

  /// <summary>Deletes an IP Access Rule from the specified zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="ruleId">The ID of the rule to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  Task DeleteAsync(string zoneId, string ruleId, CancellationToken cancellationToken = default);
}
