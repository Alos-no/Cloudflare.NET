namespace Cloudflare.NET.Security.Rulesets.Models;

using System.Text.Json.Serialization;

/// <summary>Action parameters for an 'execute' rule, used to deploy another ruleset.</summary>
/// <param name="Id">The ID of the ruleset to execute.</param>
/// <param name="Version">The version of the ruleset to execute (e.g., "latest").</param>
/// <param name="Overrides">Optional overrides for the executed ruleset.</param>
public record ExecuteParameters(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("version")]
  string Version,
  [property: JsonPropertyName("overrides")]
  ExecuteOverrides? Overrides = null
);

/// <summary>Defines overrides for a managed or custom ruleset being executed.</summary>
/// <param name="Rules">A list of rule-specific overrides.</param>
/// <param name="Categories">A list of category-specific overrides.</param>
public record ExecuteOverrides(
  [property: JsonPropertyName("rules")]
  IReadOnlyList<RuleOverride>? Rules = null,
  [property: JsonPropertyName("categories")]
  IReadOnlyList<CategoryOverride>? Categories = null
);

/// <summary>An override for a specific rule within an executed ruleset.</summary>
/// <param name="Id">The ID of the rule to override.</param>
/// <param name="Action">The new action to take (e.g., Log, Block).</param>
/// <param name="Enabled">Whether the rule should be enabled.</param>
public record RuleOverride(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("action")]
  ManagedWafOverrideAction? Action = null,
  [property: JsonPropertyName("enabled")]
  bool? Enabled = null
);

/// <summary>An override for a category of rules within a managed ruleset.</summary>
/// <param name="Category">The name of the category (tag) to override.</param>
/// <param name="Action">The new action to take for all rules in this category.</param>
/// <param name="Enabled">Whether rules in this category should be enabled.</param>
public record CategoryOverride(
  [property: JsonPropertyName("category")]
  string Category,
  [property: JsonPropertyName("action")]
  ManagedWafOverrideAction? Action = null,
  [property: JsonPropertyName("enabled")]
  bool? Enabled = null
);

/// <summary>Action parameters for a 'skip' rule, used to create exceptions.</summary>
/// <param name="Phases">A list of phases to skip.</param>
/// <param name="Rulesets">A list of ruleset IDs to skip.</param>
/// <param name="Rules">A list of rule IDs to skip.</param>
public record SkipParameters(
  [property: JsonPropertyName("phases")]
  IReadOnlyList<string>? Phases = null,
  [property: JsonPropertyName("rulesets")]
  IReadOnlyList<string>? Rulesets = null,
  [property: JsonPropertyName("rules")]
  IReadOnlyList<string>? Rules = null
);

/// <summary>A generic container for action parameters that include a custom response.</summary>
/// <param name="Response">The custom response to send to the client.</param>
public record ActionParametersWithResponse(
  [property: JsonPropertyName("response")]
  Response? Response
);

/// <summary>Defines a custom HTTP response for a rule action.</summary>
/// <param name="StatusCode">The HTTP status code.</param>
/// <param name="Content">The body of the response.</param>
/// <param name="ContentType">The content type of the response.</param>
public record Response(
  [property: JsonPropertyName("status_code")]
  int StatusCode,
  [property: JsonPropertyName("content")]
  string? Content = null,
  [property: JsonPropertyName("content_type")]
  string? ContentType = null
);
