namespace Cloudflare.NET.Security.Firewall.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents the target type for an IP Access Rule's configuration.
///   <para>
///     Access rule targets define what type of network entity the rule applies to,
///     such as a single IP address, IP range, ASN, or country.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing custom values
///     for new target types that may be added to the Cloudflare API in the future.
///   </para>
///   <para>
///     Use the static properties for known targets (e.g., <see cref="Ip" />, <see cref="IpRange" />)
///     or create custom values using the constructor or implicit string conversion.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known target with IntelliSense
///   var target = AccessRuleTarget.Ip;
///
///   // Creating a configuration with a specific target
///   var config = new IpConfiguration("192.0.2.1");
///
///   // Using implicit conversion from string
///   AccessRuleTarget customTarget = "new-target-type";
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/waf/tools/ip-access-rules/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<AccessRuleTarget>))]
public readonly struct AccessRuleTarget : IExtensibleEnum<AccessRuleTarget>, IEquatable<AccessRuleTarget>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this target.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this target.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>Targets an IPv4 or IPv6 address.</summary>
  /// <remarks>
  ///   Used to block or allow a single IP address.
  ///   Example value: "192.0.2.1" or "2001:db8::1"
  /// </remarks>
  public static AccessRuleTarget Ip { get; } = new("ip");

  /// <summary>Targets an IP range in CIDR notation.</summary>
  /// <remarks>
  ///   Used to block or allow a range of IP addresses.
  ///   Example value: "192.0.2.0/24" or "2001:db8::/32"
  /// </remarks>
  public static AccessRuleTarget IpRange { get; } = new("ip_range");

  /// <summary>Targets an Autonomous System Number (ASN).</summary>
  /// <remarks>
  ///   Used to block or allow traffic from a specific ASN.
  ///   Example value: "AS13335"
  /// </remarks>
  public static AccessRuleTarget Asn { get; } = new("asn");

  /// <summary>Targets a two-letter country code.</summary>
  /// <remarks>
  ///   Used to block or allow traffic from a specific country.
  ///   Example value: "US", "GB", "DE"
  /// </remarks>
  public static AccessRuleTarget Country { get; } = new("country");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="AccessRuleTarget" /> with the specified value.</summary>
  /// <param name="value">The string value representing the target.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public AccessRuleTarget(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static AccessRuleTarget Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="AccessRuleTarget" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator AccessRuleTarget(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="AccessRuleTarget" /> to its string value.</summary>
  /// <param name="target">The target to convert.</param>
  public static implicit operator string(AccessRuleTarget target) => target.Value;

  /// <summary>Determines whether two <see cref="AccessRuleTarget" /> values are equal.</summary>
  public static bool operator ==(AccessRuleTarget left, AccessRuleTarget right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="AccessRuleTarget" /> values are not equal.</summary>
  public static bool operator !=(AccessRuleTarget left, AccessRuleTarget right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(AccessRuleTarget other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is AccessRuleTarget other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
