namespace Cloudflare.NET.Zones.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents the type of a Cloudflare Zone setup.
///   <para>
///     Zone types determine how DNS and traffic handling are configured:
///     <list type="bullet">
///       <item>
///         <term>Full</term>
///         <description>DNS is fully hosted by Cloudflare</description>
///       </item>
///       <item>
///         <term>Partial (CNAME)</term>
///         <description>DNS remains with the original provider; only specific records are proxied</description>
///       </item>
///       <item>
///         <term>Secondary</term>
///         <description>Cloudflare acts as a secondary DNS provider</description>
///       </item>
///     </list>
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing
///     custom values for new zone types that may be added to the Cloudflare API in the future.
///   </para>
///   <para>
///     Use the static properties for known types (e.g., <see cref="Full" />, <see cref="Partial" />)
///     or create custom values using the constructor or implicit string conversion.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known type with IntelliSense
///   var zoneType = ZoneType.Full;
///
///   // Checking zone type
///   if (zone.Type == ZoneType.Partial) { ... }
///
///   // Using implicit conversion from string
///   ZoneType customType = "new-type";
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/list/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<ZoneType>))]
public readonly struct ZoneType : IExtensibleEnum<ZoneType>, IEquatable<ZoneType>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this zone type.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this zone type.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>
  ///   Full zone setup - DNS is fully hosted by Cloudflare.
  ///   <para>
  ///     This is the standard setup where all DNS records are managed through Cloudflare
  ///     and all traffic flows through Cloudflare's network.
  ///   </para>
  /// </summary>
  public static ZoneType Full { get; } = new("full");

  /// <summary>
  ///   Partial (CNAME) zone setup - DNS remains with the original provider.
  ///   <para>
  ///     In this setup, the domain's authoritative DNS remains with the original provider.
  ///     Only specific subdomains are proxied through Cloudflare using CNAME records.
  ///     This is commonly used for SaaS providers or when full DNS migration isn't possible.
  ///   </para>
  /// </summary>
  public static ZoneType Partial { get; } = new("partial");

  /// <summary>
  ///   Secondary zone setup - Cloudflare acts as a secondary DNS provider.
  ///   <para>
  ///     In this setup, Cloudflare receives zone transfers from a primary DNS provider
  ///     and acts as a secondary authoritative server. This provides DNS redundancy.
  ///   </para>
  /// </summary>
  public static ZoneType Secondary { get; } = new("secondary");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="ZoneType" /> with the specified value.</summary>
  /// <param name="value">The string value representing the zone type.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public ZoneType(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static ZoneType Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="ZoneType" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator ZoneType(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="ZoneType" /> to its string value.</summary>
  /// <param name="type">The zone type to convert.</param>
  public static implicit operator string(ZoneType type) => type.Value;

  /// <summary>Determines whether two <see cref="ZoneType" /> values are equal.</summary>
  public static bool operator ==(ZoneType left, ZoneType right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="ZoneType" /> values are not equal.</summary>
  public static bool operator !=(ZoneType left, ZoneType right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(ZoneType other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is ZoneType other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
