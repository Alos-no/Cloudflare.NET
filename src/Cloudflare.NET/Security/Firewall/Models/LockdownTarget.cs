namespace Cloudflare.NET.Security.Firewall.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents the target type for a Zone Lockdown rule's configuration.
///   <para>
///     Zone Lockdown targets define what type of IP entity the rule applies to,
///     such as a single IP address or IP range.
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
///   var target = LockdownTarget.Ip;
///
///   // Creating a configuration with a specific target
///   var config = new LockdownConfiguration(LockdownTarget.IpRange, "192.0.2.0/24");
///
///   // Using implicit conversion from string
///   LockdownTarget customTarget = "new-target-type";
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/waf/tools/zone-lockdown/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<LockdownTarget>))]
public readonly struct LockdownTarget : IExtensibleEnum<LockdownTarget>, IEquatable<LockdownTarget>
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
  ///   Used to allow a single IP address through the zone lockdown.
  ///   Example value: "192.0.2.1" or "2001:db8::1"
  /// </remarks>
  public static LockdownTarget Ip { get; } = new("ip");

  /// <summary>Targets an IP range in CIDR notation.</summary>
  /// <remarks>
  ///   Used to allow a range of IP addresses through the zone lockdown.
  ///   Example value: "192.0.2.0/24" or "2001:db8::/32"
  /// </remarks>
  public static LockdownTarget IpRange { get; } = new("ip_range");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="LockdownTarget" /> with the specified value.</summary>
  /// <param name="value">The string value representing the target.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public LockdownTarget(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static LockdownTarget Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="LockdownTarget" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator LockdownTarget(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="LockdownTarget" /> to its string value.</summary>
  /// <param name="target">The target to convert.</param>
  public static implicit operator string(LockdownTarget target) => target.Value;

  /// <summary>Determines whether two <see cref="LockdownTarget" /> values are equal.</summary>
  public static bool operator ==(LockdownTarget left, LockdownTarget right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="LockdownTarget" /> values are not equal.</summary>
  public static bool operator !=(LockdownTarget left, LockdownTarget right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(LockdownTarget other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is LockdownTarget other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
