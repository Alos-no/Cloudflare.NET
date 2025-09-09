namespace Cloudflare.NET.Security.Rulesets.Models;

using System.Text.Json.Serialization;

/// <summary>Defines the request payload for creating a new ruleset.</summary>
/// <param name="Name">The name of the ruleset.</param>
/// <param name="Kind">The kind of ruleset (e.g., "root", "zone"). This is immutable.</param>
/// <param name="Description">An optional description for the ruleset.</param>
/// <param name="Phase">The phase the ruleset belongs to.</param>
/// <param name="Rules">An optional list of rules to include in the new ruleset.</param>
public record CreateRulesetRequest(
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("kind")]
  string Kind,
  [property: JsonPropertyName("description")]
  string? Description = null,
  [property: JsonPropertyName("phase")]
  string? Phase = null,
  [property: JsonPropertyName("rules")]
  IReadOnlyList<CreateRuleRequest>? Rules = null
);

/// <summary>Defines the request payload for updating an existing ruleset.</summary>
/// <param name="Description">The updated description for the ruleset.</param>
/// <param name="Rules">The full list of rules to replace the existing ones.</param>
public record UpdateRulesetRequest(
  [property: JsonPropertyName("description")]
  string? Description,
  [property: JsonPropertyName("rules")]
  IReadOnlyList<CreateRuleRequest> Rules
);
