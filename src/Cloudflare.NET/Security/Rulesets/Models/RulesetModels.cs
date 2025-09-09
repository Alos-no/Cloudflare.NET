namespace Cloudflare.NET.Security.Rulesets.Models;

using System.Text.Json.Serialization;

/// <summary>Represents a Cloudflare Ruleset.</summary>
/// <param name="Id">The unique identifier of the ruleset.</param>
/// <param name="Name">The name of the ruleset.</param>
/// <param name="Kind">The kind of ruleset (e.g., "root", "zone").</param>
/// <param name="Version">The version of the ruleset.</param>
/// <param name="LastUpdated">When the ruleset was last updated.</param>
/// <param name="Description">An optional description of the ruleset.</param>
/// <param name="Phase">The phase the ruleset belongs to.</param>
/// <param name="Rules">The list of rules within the ruleset.</param>
public record Ruleset(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("kind")]
  string Kind,
  [property: JsonPropertyName("version")]
  string Version,
  [property: JsonPropertyName("last_updated")]
  DateTime LastUpdated,
  [property: JsonPropertyName("description")]
  string? Description = null,
  [property: JsonPropertyName("phase")]
  string? Phase = null,
  [property: JsonPropertyName("rules")]
  IReadOnlyList<Rule>? Rules = null
);
