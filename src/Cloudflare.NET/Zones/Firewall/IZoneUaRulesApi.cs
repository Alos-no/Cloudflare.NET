namespace Cloudflare.NET.Zones.Firewall;

using Security.Firewall.Models;

/// <summary>Defines the contract for managing User-Agent blocking rules.</summary>
public interface IZoneUaRulesApi
{
  /// <summary>Lists all User-Agent blocking rules for a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A read-only list of User-Agent blocking rules.</returns>
  Task<IReadOnlyList<UaRule>> ListAsync(string zoneId, CancellationToken cancellationToken = default);

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
