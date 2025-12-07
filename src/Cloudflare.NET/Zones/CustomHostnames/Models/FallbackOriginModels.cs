namespace Cloudflare.NET.Zones.CustomHostnames.Models;

using System.Text.Json.Serialization;

/// <summary>Represents the fallback origin for custom hostnames in a zone.</summary>
/// <remarks>
///   The fallback origin is the default origin server used when a custom hostname does not have a specific
///   <c>custom_origin_server</c> configured.
/// </remarks>
/// <param name="Origin">The fallback origin hostname.</param>
/// <param name="Status">The current status of the fallback origin configuration.</param>
public record FallbackOrigin(
  [property: JsonPropertyName("origin")]
  string Origin,
  [property: JsonPropertyName("status")]
  string? Status = null
);

/// <summary>Represents the request payload for updating the fallback origin.</summary>
/// <param name="Origin">The new fallback origin hostname.</param>
public record UpdateFallbackOriginRequest(
  [property: JsonPropertyName("origin")]
  string Origin
);
