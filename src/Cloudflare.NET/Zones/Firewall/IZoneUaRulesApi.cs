namespace Cloudflare.NET.Zones.Firewall;

using Core.Models;
using Security.Firewall.Models;

/// <summary>
///   <para>Defines the contract for managing User-Agent blocking rules.</para>
///   <para>These rules allow you to block or challenge requests based on their User-Agent header.</para>
/// </summary>
public interface IZoneUaRulesApi
{
  /// <summary>
  ///   Lists all User-Agent blocking rules for a zone, allowing for manual pagination
  ///   control.
  /// </summary>
  /// <remarks>
  ///   This method is intended for developers who need to control the pagination process
  ///   manually. Use the properties of the returned <see cref="PagePaginatedResult{T}" /> to
  ///   determine if there are more pages and to construct the filter for the next call.
  /// </remarks>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="filters">Optional pagination filters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A single page of User-Agent blocking rules along with pagination information.</returns>
  Task<PagePaginatedResult<UaRule>> ListAsync(string              zoneId,
                                              ListUaRulesFilters? filters           = null,
                                              CancellationToken   cancellationToken = default);

  /// <summary>Lists all User-Agent blocking rules for a zone, automatically handling pagination.</summary>
  /// <remarks>This method simplifies fetching all rules by abstracting away the pagination logic.</remarks>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="perPage">The number of results to fetch per API page.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of all User-Agent blocking rules for the zone.</returns>
  IAsyncEnumerable<UaRule> ListAllAsync(string zoneId, int? perPage = null, CancellationToken cancellationToken = default);

  /// <summary>Gets a single User-Agent blocking rule by its ID.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="ruleId">The ID of the rule.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The requested User-Agent blocking rule.</returns>
  Task<UaRule> GetAsync(string zoneId, string ruleId, CancellationToken cancellationToken = default);

  /// <summary>Creates a new User-Agent blocking rule for a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="request">The request containing the details of the rule to create.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created User-Agent blocking rule.</returns>
  Task<UaRule> CreateAsync(string zoneId, CreateUaRuleRequest request, CancellationToken cancellationToken = default);

  /// <summary>Updates an existing User-Agent blocking rule.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="ruleId">The ID of the rule to update.</param>
  /// <param name="request">The request containing the fields to update.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated User-Agent blocking rule.</returns>
  Task<UaRule> UpdateAsync(string zoneId, string ruleId, UpdateUaRuleRequest request, CancellationToken cancellationToken = default);

  /// <summary>Deletes a User-Agent blocking rule from a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="ruleId">The ID of the rule to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The ID of the deleted rule.</returns>
  Task<string> DeleteAsync(string zoneId, string ruleId, CancellationToken cancellationToken = default);
}
