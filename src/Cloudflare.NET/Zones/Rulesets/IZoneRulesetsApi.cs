namespace Cloudflare.NET.Zones.Rulesets;

using Security.Rulesets.Models;
using System;

/// <summary>Defines the contract for managing Rulesets at the zone level.</summary>
public interface IZoneRulesetsApi
{
  /// <summary>Lists all rulesets for a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A read-only list of rulesets.</returns>
  Task<IReadOnlyList<Ruleset>> ListAsync(string zoneId, CancellationToken cancellationToken = default);

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
  Task<Ruleset> UpdatePhaseEntrypointAsync(string            zoneId,
                                           string            phase,
                                           IEnumerable<Rule> rules,
                                           CancellationToken cancellationToken = default);

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
