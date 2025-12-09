namespace Cloudflare.NET.Security.Firewall.Models;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Core.Json;

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
