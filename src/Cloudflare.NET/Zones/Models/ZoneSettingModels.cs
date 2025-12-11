namespace Cloudflare.NET.Zones.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///   Represents a zone configuration setting.
///   <para>
///     The <see cref="Value"/> property contains the setting's current value, which varies by setting type.
///     Use JsonElement methods to extract the appropriate type (GetString, GetInt32, etc.).
///   </para>
/// </summary>
/// <example>
///   <code>
///   var setting = await zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.MinTlsVersion);
///   string tlsVersion = setting.Value.GetString(); // "1.2"
///
///   var cacheTtl = await zones.GetZoneSettingAsync(zoneId, ZoneSettingIds.BrowserCacheTtl);
///   int ttlSeconds = cacheTtl.Value.GetInt32(); // 14400
///   </code>
/// </example>
/// <param name="Id">The setting identifier (e.g., "min_tls_version", "ssl").</param>
/// <param name="Value">
///   The current value of the setting. Type varies by setting:
///   <list type="bullet">
///     <item>Toggle settings: string "on" or "off"</item>
///     <item>Numeric settings: integer (e.g., TTL in seconds)</item>
///     <item>Selection settings: enumerated string values</item>
///     <item>Complex settings: nested JSON object</item>
///   </list>
/// </param>
/// <param name="Editable">Whether this setting can be modified for the current zone/plan.</param>
/// <param name="ModifiedOn">When the setting was last modified.</param>
/// <seealso href="https://developers.cloudflare.com/api/resources/zones/subresources/settings/" />
public record ZoneSetting(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("value")]
  JsonElement Value,

  [property: JsonPropertyName("editable")]
  bool Editable,

  [property: JsonPropertyName("modified_on")]
  DateTime? ModifiedOn
);


/// <summary>
///   Request to update a zone setting.
/// </summary>
/// <typeparam name="T">The value type (string, int, or complex object).</typeparam>
/// <param name="Value">The new value for the setting.</param>
public record UpdateZoneSettingRequest<T>(
  [property: JsonPropertyName("value")]
  T Value
);
