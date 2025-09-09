namespace Cloudflare.NET.Security.Firewall.Models;

using System.Text.Json.Serialization;

/// <summary>Defines the request payload for creating a new IP Access Rule.</summary>
/// <param name="Mode">The action to take (e.g., block, challenge).</param>
/// <param name="Configuration">The rule configuration, specifying the target and value.</param>
/// <param name="Notes">An optional note for the rule.</param>
public record CreateAccessRuleRequest(
  [property: JsonPropertyName("mode")]
  AccessRuleMode Mode,
  [property: JsonPropertyName("configuration")]
  AccessRuleConfiguration Configuration,
  [property: JsonPropertyName("notes")]
  string? Notes = null
);

/// <summary>
///   Defines the request payload for updating an existing IP Access Rule. All properties
///   are optional.
/// </summary>
/// <param name="Mode">The new action to take. If null, the mode is not changed.</param>
/// <param name="Notes">
///   The new note for the rule. If null, the note is not changed. To clear a
///   note, provide an empty string.
/// </param>
public record UpdateAccessRuleRequest(
  [property: JsonPropertyName("mode")]
  AccessRuleMode? Mode = null,
  [property: JsonPropertyName("notes")]
  string? Notes = null
);
