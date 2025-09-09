namespace Cloudflare.NET.Security.Firewall.Models;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>Defines the configuration target for a Zone Lockdown rule.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum LockdownTarget
{
  [EnumMember(Value = "ip")]       Ip,
  [EnumMember(Value = "ip_range")] IpRange
}

/// <summary>Represents a single IP configuration within a Zone Lockdown rule.</summary>
/// <param name="Target">The type of target, either "ip" or "ip_range".</param>
/// <param name="Value">The IP address or CIDR range.</param>
public record LockdownConfiguration(
  [property: JsonPropertyName("target")]
  LockdownTarget Target,
  [property: JsonPropertyName("value")]
  string Value
);

/// <summary>Defines the request payload for creating a new Zone Lockdown rule.</summary>
public record CreateLockdownRequest(
  [property: JsonPropertyName("urls")]
  IReadOnlyList<string> Urls,
  [property: JsonPropertyName("configurations")]
  IReadOnlyList<LockdownConfiguration> Configurations,
  [property: JsonPropertyName("id")]
  string? Id = null,
  [property: JsonPropertyName("paused")]
  bool? Paused = false,
  [property: JsonPropertyName("description")]
  string? Description = null
);

/// <summary>Defines the request payload for updating a Zone Lockdown rule.</summary>
public record UpdateLockdownRequest(
  [property: JsonPropertyName("urls")]
  IReadOnlyList<string> Urls,
  [property: JsonPropertyName("configurations")]
  IReadOnlyList<LockdownConfiguration> Configurations,
  [property: JsonPropertyName("paused")]
  bool? Paused = null,
  [property: JsonPropertyName("description")]
  string? Description = null
);

/// <summary>Represents a Zone Lockdown rule returned by the Cloudflare API.</summary>
public record Lockdown(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("urls")]
  IReadOnlyList<string> Urls,
  [property: JsonPropertyName("configurations")]
  IReadOnlyList<LockdownConfiguration> Configurations,
  [property: JsonPropertyName("paused")]
  bool Paused,
  [property: JsonPropertyName("description")]
  string? Description,
  [property: JsonPropertyName("created_on")]
  DateTimeOffset CreatedOn,
  [property: JsonPropertyName("modified_on")]
  DateTimeOffset ModifiedOn
);
