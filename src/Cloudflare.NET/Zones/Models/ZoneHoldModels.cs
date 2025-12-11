namespace Cloudflare.NET.Zones.Models;

using System.Text.Json.Serialization;

/// <summary>
///   Represents the zone hold status and configuration.
///   <para>
///     A zone hold prevents creation and activation of zones with the same hostname.
///     When include_subdomains is enabled, it also blocks all subdomains and SSL4SaaS Custom Hostnames.
///   </para>
/// </summary>
/// <param name="Hold">Whether the zone hold is currently active.</param>
/// <param name="HoldAfter">
///   The date-time when the hold becomes/became active.
///   If this value is in the past, the hold is active.
/// </param>
/// <param name="IncludeSubdomains">
///   Whether the hold extends to all subdomains.
///   When true, a hold on "example.com" also blocks "staging.example.com", etc.
/// </param>
/// <seealso href="https://developers.cloudflare.com/api/resources/zones/subresources/holds/" />
public record ZoneHold(
  [property: JsonPropertyName("hold")]
  bool Hold,

  [property: JsonPropertyName("hold_after")]
  DateTime? HoldAfter,

  [property: JsonPropertyName("include_subdomains")]
  bool? IncludeSubdomains
);


/// <summary>
///   Request to update an existing zone hold.
///   Both fields are optional; update only the fields you want to change.
/// </summary>
/// <param name="HoldAfter">
///   The date-time when the hold should become active.
///   Set to a past date to immediately activate the hold.
/// </param>
/// <param name="IncludeSubdomains">Whether to extend the hold to all subdomains.</param>
public record UpdateZoneHoldRequest(
  [property: JsonPropertyName("hold_after")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  DateTime? HoldAfter = null,

  [property: JsonPropertyName("include_subdomains")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? IncludeSubdomains = null
);
