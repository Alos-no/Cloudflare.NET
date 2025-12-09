namespace Cloudflare.NET.Accounts.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents a location hint for R2 bucket placement.
///   <para>
///     Location hints provide a suggestion for the geographic region where the bucket's data should be stored.
///     Cloudflare will attempt to honor this hint, but data may be placed in a nearby region if the specified
///     location is unavailable.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing custom values
///     for new regions that may be added to the Cloudflare API in the future.
///   </para>
///   <para>
///     Use the static properties for known regions (e.g., <see cref="EastNorthAmerica" />) or create custom
///     values using the constructor or implicit string conversion.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known location with IntelliSense
///   var location = R2LocationHint.EastNorthAmerica;
///
///   // Using implicit conversion from string
///   R2LocationHint customLocation = "new-region";
///
///   // Comparison
///   if (bucket.LocationHint == R2LocationHint.WestEurope) { ... }
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/r2/reference/data-location/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<R2LocationHint>))]
public readonly struct R2LocationHint : IExtensibleEnum<R2LocationHint>, IEquatable<R2LocationHint>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this location hint.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this location hint.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>Western North America region.</summary>
  /// <remarks>Suitable for users primarily in the western United States, Canada, or Mexico.</remarks>
  public static R2LocationHint WestNorthAmerica { get; } = new("wnam");

  /// <summary>Eastern North America region.</summary>
  /// <remarks>Suitable for users primarily in the eastern United States or eastern Canada.</remarks>
  public static R2LocationHint EastNorthAmerica { get; } = new("enam");

  /// <summary>Western Europe region.</summary>
  /// <remarks>Suitable for users primarily in Western European countries.</remarks>
  public static R2LocationHint WestEurope { get; } = new("weur");

  /// <summary>Eastern Europe region.</summary>
  /// <remarks>Suitable for users primarily in Eastern European countries.</remarks>
  public static R2LocationHint EastEurope { get; } = new("eeur");

  /// <summary>Asia-Pacific region.</summary>
  /// <remarks>Suitable for users primarily in Asian countries, including East Asia, Southeast Asia, and South Asia.</remarks>
  public static R2LocationHint AsiaPacific { get; } = new("apac");

  /// <summary>Oceania region.</summary>
  /// <remarks>Suitable for users primarily in Australia, New Zealand, and Pacific Island nations.</remarks>
  public static R2LocationHint Oceania { get; } = new("oc");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="R2LocationHint" /> with the specified value.</summary>
  /// <param name="value">The string value representing the location hint.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public R2LocationHint(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static R2LocationHint Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to an <see cref="R2LocationHint" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator R2LocationHint(string value) => new(value);

  /// <summary>Implicitly converts an <see cref="R2LocationHint" /> to its string value.</summary>
  /// <param name="location">The location hint to convert.</param>
  public static implicit operator string(R2LocationHint location) => location.Value;

  /// <summary>Determines whether two <see cref="R2LocationHint" /> values are equal.</summary>
  public static bool operator ==(R2LocationHint left, R2LocationHint right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="R2LocationHint" /> values are not equal.</summary>
  public static bool operator !=(R2LocationHint left, R2LocationHint right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(R2LocationHint other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is R2LocationHint other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
