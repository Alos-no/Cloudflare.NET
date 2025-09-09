namespace Cloudflare.NET.Zones.AccessRules;

using Security.Firewall.Models;

/// <summary>Defines the contract for managing IP Access Rules at the zone level.</summary>
public interface IZoneAccessRulesApi
{
  /// <summary>Lists all IP Access Rules for the specified zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="filters">Optional filters to apply to the list operation.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A read-only list of access rules.</returns>
  Task<IReadOnlyList<AccessRule>> ListAsync(string                  zoneId,
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
  Task<AccessRule> UpdateAsync(string zoneId, string ruleId, UpdateAccessRuleRequest request, CancellationToken cancellationToken = default);

  /// <summary>Deletes an IP Access Rule from the specified zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="ruleId">The ID of the rule to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  Task DeleteAsync(string zoneId, string ruleId, CancellationToken cancellationToken = default);
}