namespace Cloudflare.NET.Security.Rulesets.Models;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>Defines the actions that can be performed by a rule in the Ruleset Engine.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum RulesetAction
{
  [EnumMember(Value = "block")]     Block,
  [EnumMember(Value = "challenge")] Challenge,
  [EnumMember(Value = "managed_challenge")]
  ManagedChallenge,
  [EnumMember(Value = "log")]        Log,
  [EnumMember(Value = "skip")]       Skip,
  [EnumMember(Value = "execute")]    Execute,
  [EnumMember(Value = "rewrite")]    Rewrite,
  [EnumMember(Value = "redirect")]   Redirect,
  [EnumMember(Value = "route")]      Route,
  [EnumMember(Value = "set_config")] SetConfig
}

/// <summary>Defines the actions allowed when overriding rules in a Managed WAF Ruleset.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum ManagedWafOverrideAction
{
  [EnumMember(Value = "managed_challenge")]
  ManagedChallenge,
  [EnumMember(Value = "challenge")]    Challenge,
  [EnumMember(Value = "js_challenge")] JsChallenge,
  [EnumMember(Value = "block")]        Block,
  [EnumMember(Value = "log")]          Log,
  /// <summary>Removes a previously set override.</summary>
  [EnumMember(Value = "default")] Default
}
