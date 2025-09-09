namespace Cloudflare.NET.Security.Firewall.Models;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>Defines the action to take for an IP Access Rule.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum AccessRuleMode
{
  /// <summary>Denies access.</summary>
  [EnumMember(Value = "block")] Block,
  /// <summary>Presents an interactive challenge.</summary>
  [EnumMember(Value = "challenge")] Challenge,
  /// <summary>Presents a JavaScript challenge (legacy).</summary>
  [EnumMember(Value = "js_challenge")] JsChallenge,
  /// <summary>Uses Cloudflare's heuristics to decide if a challenge is needed.</summary>
  [EnumMember(Value = "managed_challenge")]
  ManagedChallenge,
  /// <summary>Allows access, bypassing other rules. Maps to "allow" in the UI.</summary>
  [EnumMember(Value = "whitelist")] Whitelist
}

/// <summary>Defines the target of an IP Access Rule's configuration.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum AccessRuleTarget
{
  /// <summary>Targets an IPv4 or IPv6 address.</summary>
  [EnumMember(Value = "ip")] Ip,
  /// <summary>Targets an IP range in CIDR notation.</summary>
  [EnumMember(Value = "ip_range")] IpRange,
  /// <summary>Targets an Autonomous System Number (ASN).</summary>
  [EnumMember(Value = "asn")] Asn,
  /// <summary>Targets a two-letter country code.</summary>
  [EnumMember(Value = "country")] Country
}

/// <summary>Defines the direction for sorting lists.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum ListOrderDirection
{
  /// <summary>Sorts in ascending order.</summary>
  [EnumMember(Value = "asc")] Ascending,
  /// <summary>Sorts in descending order.</summary>
  [EnumMember(Value = "desc")] Descending
}

/// <summary>Defines the fields by which IP Access Rules can be ordered.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum AccessRuleOrderField
{
  /// <summary>Orders by the rule's mode (action).</summary>
  [EnumMember(Value = "mode")] Mode,
  /// <summary>Orders by the rule's configuration target.</summary>
  [EnumMember(Value = "configuration.target")]
  ConfigurationTarget,
  /// <summary>Orders by the rule's configuration value.</summary>
  [EnumMember(Value = "configuration.value")]
  ConfigurationValue
}

/// <summary>Defines how multiple filters are combined when listing resources.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum FilterMatch
{
  /// <summary>Returns results that match any of the provided filters.</summary>
  [EnumMember(Value = "any")] Any,
  /// <summary>Returns results that match all of the provided filters (default).</summary>
  [EnumMember(Value = "all")] All
}
