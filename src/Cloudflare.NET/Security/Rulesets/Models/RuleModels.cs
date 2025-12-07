namespace Cloudflare.NET.Security.Rulesets.Models;

using System.Text.Json.Serialization;

/// <summary>Represents a single rule within a Ruleset.</summary>
/// <param name="Id">The unique identifier of the rule.</param>
/// <param name="Version">The version of the rule.</param>
/// <param name="Action">The action to perform when the rule matches.</param>
/// <param name="Expression">The filter expression that triggers the rule.</param>
/// <param name="Enabled">Whether the rule is enabled.</param>
/// <param name="LastUpdated">When the rule was last updated.</param>
/// <param name="Description">An optional description of the rule.</param>
/// <param name="ActionParameters">Parameters for the rule's action.</param>
/// <param name="Logging">Configuration for logging.</param>
/// <param name="Ratelimit">Rate limiting configuration for the rule.</param>
public record Rule(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("version")]
  string Version,
  [property: JsonPropertyName("action")]
  RulesetAction Action,
  [property: JsonPropertyName("expression")]
  string Expression,
  [property: JsonPropertyName("enabled")]
  bool Enabled,
  [property: JsonPropertyName("last_updated")]
  DateTime LastUpdated,
  [property: JsonPropertyName("description")]
  string? Description = null,
  [property: JsonPropertyName("action_parameters")]
  object? ActionParameters = null,
  [property: JsonPropertyName("logging")]
  Logging? Logging = null,
  [property: JsonPropertyName("ratelimit")]
  RateLimitParameters? Ratelimit = null
);

/// <summary>Defines the request payload for creating a new rule within a ruleset.</summary>
/// <param name="Action">The action to perform when the rule matches.</param>
/// <param name="Expression">The filter expression that triggers the rule.</param>
/// <param name="Description">An optional description of the rule.</param>
/// <param name="Enabled">Whether the rule is enabled.</param>
/// <param name="ActionParameters">Parameters for the rule's action.</param>
/// <param name="Logging">Configuration for logging.</param>
/// <param name="Ratelimit">Rate limiting configuration for the rule.</param>
public record CreateRuleRequest(
  [property: JsonPropertyName("action")]
  RulesetAction Action,
  [property: JsonPropertyName("expression")]
  string Expression,
  [property: JsonPropertyName("description")]
  string? Description = null,
  [property: JsonPropertyName("enabled")]
  bool? Enabled = true,
  [property: JsonPropertyName("action_parameters")]
  object? ActionParameters = null,
  [property: JsonPropertyName("logging")]
  Logging? Logging = null,
  [property: JsonPropertyName("ratelimit")]
  RateLimitParameters? Ratelimit = null
);

/// <summary>Defines logging configuration for a rule.</summary>
/// <param name="Enabled">Whether logging is enabled for the rule.</param>
public record Logging(
  [property: JsonPropertyName("enabled")]
  bool Enabled
);

/// <summary>Defines the configuration for a rate limiting rule.</summary>
/// <param name="Characteristics">The properties to track for rate limiting (e.g., "ip.src").</param>
/// <param name="Period">
///   The time period in seconds. Only specific values are allowed. It is highly recommended to use the
///   constants defined in <see cref="SecurityConstants.RateLimiting.Periods" /> to avoid errors.
/// </param>
/// <param name="RequestsPerPeriod">The number of requests allowed in the period for standard rate limiting.</param>
/// <param name="ScorePerPeriod">The score threshold for complexity-based rate limiting.</param>
/// <param name="MitigationTimeout">The duration in seconds to apply the mitigation action.</param>
/// <param name="RequestsToOrigin">Whether to count requests to origin (ignores cache).</param>
/// <param name="CountingExpression">A filter expression to specify which requests to count.</param>
/// <param name="ScoreResponseHeaderName">For complexity-based limiting, the name of the response header to send the score.</param>
/// <param name="Simulate">If true, the rule will be logged but not actioned.</param>
public record RateLimitParameters(
  [property: JsonPropertyName("characteristics")]
  IReadOnlyList<string> Characteristics,
  [property: JsonPropertyName("period")]
  int Period,
  [property: JsonPropertyName("mitigation_timeout")]
  int MitigationTimeout,
  [property: JsonPropertyName("requests_per_period")]
  int? RequestsPerPeriod = null,
  [property: JsonPropertyName("score_per_period")]
  int? ScorePerPeriod = null,
  [property: JsonPropertyName("requests_to_origin")]
  bool? RequestsToOrigin = null,
  [property: JsonPropertyName("counting_expression")]
  string? CountingExpression = null,
  [property: JsonPropertyName("score_response_header_name")]
  string? ScoreResponseHeaderName = null,
  [property: JsonPropertyName("simulate")]
  bool? Simulate = null
);
