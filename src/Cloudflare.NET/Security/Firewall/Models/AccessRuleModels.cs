namespace Cloudflare.NET.Security.Firewall.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Represents a single IP Access Rule.</summary>
/// <param name="Id">The unique identifier of the rule.</param>
/// <param name="Mode">The action to take when the rule matches.</param>
/// <param name="Configuration">The configuration that defines the rule's target.</param>
/// <param name="Notes">Optional notes about the rule.</param>
/// <param name="AllowedModes">The modes that are allowed for this rule.</param>
/// <param name="Scope">The scope of the rule (e.g., account, zone).</param>
/// <param name="CreatedOn">When the rule was created.</param>
/// <param name="ModifiedOn">When the rule was last modified.</param>
public record AccessRule(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("mode")]
  AccessRuleMode Mode,
  [property: JsonPropertyName("configuration")]
  AccessRuleConfiguration Configuration,
  [property: JsonPropertyName("notes")]
  string? Notes,
  [property: JsonPropertyName("allowed_modes")]
  IReadOnlyList<AccessRuleMode> AllowedModes,
  [property: JsonPropertyName("scope")]
  Scope Scope,
  [property: JsonPropertyName("created_on")]
  DateTimeOffset? CreatedOn,
  [property: JsonPropertyName("modified_on")]
  DateTimeOffset? ModifiedOn);

/// <summary>Represents the scope of an access rule.</summary>
/// <param name="Id">The ID of the scope (e.g., account or zone ID).</param>
/// <param name="Type">The type of scope (e.g., "account", "zone").</param>
public record Scope(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("type")]
  string Type);

/// <summary>
///   Represents the abstract configuration for an access rule, defining what to match
///   against.
/// </summary>
[JsonConverter(typeof(AccessRuleConfigurationConverter))]
public abstract record AccessRuleConfiguration(
  [property: JsonPropertyName("target")]
  AccessRuleTarget Target,
  [property: JsonPropertyName("value")]
  string Value);

/// <summary>An access rule configuration that targets a single IPv4 or IPv6 address.</summary>
public record IpConfiguration(string Value) : AccessRuleConfiguration(AccessRuleTarget.Ip, Value);

/// <summary>An access rule configuration that targets an IP range in CIDR notation.</summary>
public record CidrConfiguration(string Value) : AccessRuleConfiguration(AccessRuleTarget.IpRange, Value);

/// <summary>An access rule configuration that targets an Autonomous System Number (ASN).</summary>
public record AsnConfiguration(string Value) : AccessRuleConfiguration(AccessRuleTarget.Asn, Value);

/// <summary>An access rule configuration that targets a two-letter country code.</summary>
public record CountryConfiguration(string Value) : AccessRuleConfiguration(AccessRuleTarget.Country, Value);

/// <summary>
///   Custom JSON converter for deserializing the polymorphic
///   <see cref="AccessRuleConfiguration" />.
/// </summary>
public class AccessRuleConfigurationConverter : JsonConverter<AccessRuleConfiguration>
{
  #region Methods Impl

  /// <inheritdoc />
  public override AccessRuleConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    using var jsonDoc = JsonDocument.ParseValue(ref reader);
    var       root    = jsonDoc.RootElement;

    if (!root.TryGetProperty("target", out var targetElement) || !root.TryGetProperty("value", out var valueElement))
      throw new JsonException("AccessRuleConfiguration must have 'target' and 'value' properties.");

    var targetString = targetElement.GetString();
    var value        = valueElement.GetString() ?? throw new JsonException("Value cannot be null.");

    var namingPolicy = options.PropertyNamingPolicy ?? JsonNamingPolicy.SnakeCaseLower;
    var targetEnum = Enum.GetValues<AccessRuleTarget>()
                         .FirstOrDefault(e =>
                         {
                           var member = typeof(AccessRuleTarget).GetMember(e.ToString()).FirstOrDefault();
                           var enumMemberAttr = member?.GetCustomAttributes(typeof(System.Runtime.Serialization.EnumMemberAttribute), false)
                                                      .Cast<System.Runtime.Serialization.EnumMemberAttribute>()
                                                      .FirstOrDefault();
                           return string.Equals(enumMemberAttr?.Value, targetString, StringComparison.OrdinalIgnoreCase);
                         });

    return targetEnum switch
    {
      AccessRuleTarget.Ip => new IpConfiguration(value),
      AccessRuleTarget.IpRange => new CidrConfiguration(value),
      AccessRuleTarget.Asn => new AsnConfiguration(value),
      AccessRuleTarget.Country => new CountryConfiguration(value),
      _ => throw new JsonException($"Unknown AccessRuleConfiguration target: {targetString}")
    };
  }

  /// <inheritdoc />
  public override void Write(Utf8JsonWriter writer, AccessRuleConfiguration value, JsonSerializerOptions options)
  {
    // Use default serialization which works correctly for the record hierarchy.
    // We need to cast to the concrete type to ensure the correct properties are serialized.
    JsonSerializer.Serialize(writer, value, value.GetType(), options);
  }

  #endregion
}
