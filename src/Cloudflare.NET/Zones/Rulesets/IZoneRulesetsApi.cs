namespace Cloudflare.NET.Zones.Rulesets;

using Core.Models;
using Security.Rulesets.Models;

/// <summary>
///   <para>Defines the contract for managing Rulesets at the zone level.</para>
///   <para>
///     This includes WAF Custom Rules, Rate Limiting Rules, Redirects, and other advanced
///     configurations for a specific zone.
///   </para>
/// </summary>
public interface IZoneRulesetsApi
{
  /// <summary>Lists all rulesets for a zone, allowing for manual pagination control.</summary>
  /// <remarks>
  ///   This method is intended for developers who need to control the pagination process
  ///   manually. To fetch the next page of results, use the <c>Cursor</c> from the
  ///   <c>CursorInfo</c> property of the returned <see cref="CursorPaginatedResult{T}" /> in your
  ///   next call.
  /// </remarks>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="filters">
  ///   Optional pagination filters, including the cursor from a previous
  ///   response.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A single page of rulesets along with pagination information.</returns>
  Task<CursorPaginatedResult<Ruleset>> ListAsync(string               zoneId,
                                                 ListRulesetsFilters? filters           = null,
                                                 CancellationToken    cancellationToken = default);

  /// <summary>Lists all rulesets for a zone, automatically handling cursor-based pagination.</summary>
  /// <remarks>
  ///   This method simplifies fetching all rulesets by abstracting away the pagination
  ///   logic.
  /// </remarks>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="perPage">The number of results to fetch per API page.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of all rulesets for the zone.</returns>
  IAsyncEnumerable<Ruleset> ListAllAsync(string zoneId, int? perPage = null, CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists the version history for a specific phase entrypoint at the zone level, allowing
  ///   for manual pagination.
  /// </summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="phase">The phase of the entrypoint.</param>
  /// <param name="filters">Optional pagination filters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A single page of ruleset versions along with pagination information.</returns>
  Task<PagePaginatedResult<Ruleset>> ListPhaseEntrypointVersionsAsync(
    string                      zoneId,
    string                      phase,
    ListRulesetVersionsFilters? filters           = null,
    CancellationToken           cancellationToken = default);

  /// <summary>Gets a specific version of a phase entrypoint ruleset at the zone level.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="phase">The phase of the entrypoint.</param>
  /// <param name="version">The version number to retrieve.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The specified version of the ruleset.</returns>
  Task<Ruleset> GetPhaseEntrypointVersionAsync(string zoneId, string phase, string version, CancellationToken cancellationToken = default);

  /// <summary>Gets a single ruleset by its ID within a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="rulesetId">The ID of the ruleset to retrieve.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The requested ruleset.</returns>
  Task<Ruleset> GetAsync(string zoneId, string rulesetId, CancellationToken cancellationToken = default);

  /// <summary>Creates a new ruleset for a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="request">The request containing the details of the ruleset to create.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created ruleset.</returns>
  Task<Ruleset> CreateAsync(string zoneId, CreateRulesetRequest request, CancellationToken cancellationToken = default);

  /// <summary>Updates an existing ruleset in a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="rulesetId">The ID of the ruleset to update.</param>
  /// <param name="request">The request containing the fields to update.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated ruleset.</returns>
  Task<Ruleset> UpdateAsync(string zoneId, string rulesetId, UpdateRulesetRequest request, CancellationToken cancellationToken = default);

  /// <summary>Deletes a ruleset from a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="rulesetId">The ID of the ruleset to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  Task DeleteAsync(string zoneId, string rulesetId, CancellationToken cancellationToken = default);

  /// <summary>Gets the entrypoint ruleset for a specific phase at the zone level.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="phase">The phase of the entrypoint (e.g., "http_request_firewall_managed").</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The entrypoint ruleset.</returns>
  Task<Ruleset> GetPhaseEntrypointAsync(string zoneId, string phase, CancellationToken cancellationToken = default);

  /// <summary>Updates the entrypoint ruleset for a specific phase at the zone level.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="phase">The phase of the entrypoint.</param>
  /// <param name="rules">The list of rules to set for the ruleset.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated ruleset.</returns>
  Task<Ruleset> UpdatePhaseEntrypointAsync(string                         zoneId,
                                           string                         phase,
                                           IEnumerable<CreateRuleRequest> rules,
                                           CancellationToken              cancellationToken = default);

  /// <summary>Adds a new rule to a specific zone-level ruleset.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="rulesetId">The ID of the ruleset.</param>
  /// <param name="rule">The rule to add.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated parent ruleset.</returns>
  Task<Ruleset> AddRuleAsync(string zoneId, string rulesetId, CreateRuleRequest rule, CancellationToken cancellationToken = default);

  /// <summary>Updates an existing rule within a zone-level ruleset.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="rulesetId">The ID of the ruleset containing the rule.</param>
  /// <param name="ruleId">The ID of the rule to update.</param>
  /// <param name="rule">The partial rule object with fields to update.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated parent ruleset.</returns>
  Task<Ruleset> UpdateRuleAsync(string zoneId, string rulesetId, string ruleId, object rule, CancellationToken cancellationToken = default);

  /// <summary>Deletes a rule from a zone-level ruleset.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="rulesetId">The ID of the ruleset.</param>
  /// <param name="ruleId">The ID of the rule to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated parent ruleset.</returns>
  Task<Ruleset> DeleteRuleAsync(string zoneId, string rulesetId, string ruleId, CancellationToken cancellationToken = default);
}
