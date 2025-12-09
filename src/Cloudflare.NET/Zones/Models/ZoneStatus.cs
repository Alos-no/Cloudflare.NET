namespace Cloudflare.NET.Zones.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents the status of a Cloudflare Zone.
///   <para>
///     Zone statuses indicate the current state of a zone in Cloudflare's system,
///     such as whether it's active, pending verification, or has been moved/deleted.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing custom values
///     for new statuses that may be added to the Cloudflare API in the future.
///   </para>
///   <para>
///     Use the static properties for known statuses (e.g., <see cref="Active" />, <see cref="Pending" />)
///     or create custom values using the constructor or implicit string conversion.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known status with IntelliSense
///   var status = ZoneStatus.Active;
///
///   // Checking zone status
///   if (zone.Status == ZoneStatus.Active) { ... }
///
///   // Using implicit conversion from string
///   ZoneStatus customStatus = "new-status";
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/list/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<ZoneStatus>))]
public readonly struct ZoneStatus : IExtensibleEnum<ZoneStatus>, IEquatable<ZoneStatus>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this status.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this status.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>Active status - the zone is fully operational and serving traffic.</summary>
  /// <remarks>
  ///   This is the normal status for a zone that has completed setup and is working correctly.
  /// </remarks>
  public static ZoneStatus Active { get; } = new("active");

  /// <summary>Pending status - the zone is awaiting nameserver verification.</summary>
  /// <remarks>
  ///   A zone enters this status when first added to Cloudflare. The domain's nameservers
  ///   must be changed to Cloudflare's nameservers for verification to complete.
  /// </remarks>
  public static ZoneStatus Pending { get; } = new("pending");

  /// <summary>Initializing status - the zone is being set up.</summary>
  /// <remarks>
  ///   This is a transitional status that occurs during zone creation.
  /// </remarks>
  public static ZoneStatus Initializing { get; } = new("initializing");

  /// <summary>Moved status - the zone has been transferred to another account.</summary>
  /// <remarks>
  ///   This status indicates the zone is no longer accessible in this account
  ///   because it has been moved to a different Cloudflare account.
  /// </remarks>
  public static ZoneStatus Moved { get; } = new("moved");

  /// <summary>Deleted status - the zone has been removed.</summary>
  /// <remarks>
  ///   This status indicates the zone has been deleted from Cloudflare.
  /// </remarks>
  public static ZoneStatus Deleted { get; } = new("deleted");

  /// <summary>Deactivated status - the zone has been deactivated.</summary>
  /// <remarks>
  ///   This status indicates the zone is no longer active, possibly due to
  ///   billing issues or manual deactivation.
  /// </remarks>
  public static ZoneStatus Deactivated { get; } = new("deactivated");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="ZoneStatus" /> with the specified value.</summary>
  /// <param name="value">The string value representing the status.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public ZoneStatus(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static ZoneStatus Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="ZoneStatus" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator ZoneStatus(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="ZoneStatus" /> to its string value.</summary>
  /// <param name="status">The status to convert.</param>
  public static implicit operator string(ZoneStatus status) => status.Value;

  /// <summary>Determines whether two <see cref="ZoneStatus" /> values are equal.</summary>
  public static bool operator ==(ZoneStatus left, ZoneStatus right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="ZoneStatus" /> values are not equal.</summary>
  public static bool operator !=(ZoneStatus left, ZoneStatus right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(ZoneStatus other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is ZoneStatus other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
