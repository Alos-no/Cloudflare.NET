namespace Cloudflare.NET.Security.Firewall.Models;

using System.Text.Json.Serialization;

/// <summary>Represents the configuration for a User-Agent blocking rule.</summary>
/// <param name="Target">The target of the configuration, always "ua".</param>
/// <param name="Value">The User-Agent string to match.</param>
public record UaRuleConfiguration(
  [property: JsonPropertyName("target")]
  string Target,
  [property: JsonPropertyName("value")]
  string Value
);

/// <summary>Defines the request payload for creating a new User-Agent blocking rule.</summary>
public record CreateUaRuleRequest(
  [property: JsonPropertyName("mode")]
  UaRuleMode Mode,
  [property: JsonPropertyName("configuration")]
  UaRuleConfiguration Configuration,
  [property: JsonPropertyName("paused")]
  bool? Paused = false,
  [property: JsonPropertyName("description")]
  string? Description = null
);

/// <summary>Defines the request payload for updating a User-Agent blocking rule.</summary>
public record UpdateUaRuleRequest(
  [property: JsonPropertyName("mode")]
  UaRuleMode? Mode = null,
  [property: JsonPropertyName("configuration")]
  UaRuleConfiguration? Configuration = null,
  [property: JsonPropertyName("paused")]
  bool? Paused = null,
  [property: JsonPropertyName("description")]
  string? Description = null
);

/// <summary>Represents a User-Agent blocking rule returned by the Cloudflare API.</summary>
public record UaRule(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("mode")]
  UaRuleMode Mode,
  [property: JsonPropertyName("configuration")]
  UaRuleConfiguration Configuration,
  [property: JsonPropertyName("paused")]
  bool Paused,
  [property: JsonPropertyName("description")]
  string? Description
);
