namespace Cloudflare.NET.Accounts.D1.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents a jurisdictional restriction for D1 database data storage.
///   <para>
///     Jurisdictions provide a guarantee that data stored in the database will remain within a specific
///     geographic or regulatory boundary. Unlike location hints, jurisdictions are enforced constraints.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing custom values
///     for new jurisdictions that may be added to the Cloudflare API in the future.
///   </para>
///   <para>
///     When creating a D1 database with a jurisdiction, the <c>primary_location_hint</c> parameter is ignored.
///     The jurisdiction takes precedence and guarantees data residency within the specified region.
///   </para>
///   <para>
///     The <see cref="FedRamp" /> jurisdiction requires Enterprise customer status. Contact your Cloudflare
///     account team for access.
///   </para>
/// </remarks>
/// <seealso href="https://developers.cloudflare.com/d1/configuration/data-location/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<D1Jurisdiction>))]
public readonly struct D1Jurisdiction : IExtensibleEnum<D1Jurisdiction>, IEquatable<D1Jurisdiction>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this jurisdiction.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this jurisdiction.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>European Union jurisdiction.</summary>
  /// <remarks>
  ///   Data is guaranteed to be stored within the European Union, helping meet GDPR
  ///   and other EU data residency requirements.
  /// </remarks>
  public static D1Jurisdiction EuropeanUnion { get; } = new("eu");

  /// <summary>FedRAMP jurisdiction.</summary>
  /// <remarks>
  ///   <para>
  ///     Data is stored in compliance with FedRAMP (Federal Risk and Authorization Management Program)
  ///     requirements for U.S. federal government data.
  ///   </para>
  ///   <para>
  ///     This jurisdiction requires Enterprise customer status. Contact your Cloudflare account team
  ///     or Cloudflare Support for access.
  ///   </para>
  /// </remarks>
  public static D1Jurisdiction FedRamp { get; } = new("fedramp");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="D1Jurisdiction" /> with the specified value.</summary>
  /// <param name="value">The string value representing the jurisdiction.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public D1Jurisdiction(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static D1Jurisdiction Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="D1Jurisdiction" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator D1Jurisdiction(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="D1Jurisdiction" /> to its string value.</summary>
  /// <param name="jurisdiction">The jurisdiction to convert.</param>
  public static implicit operator string(D1Jurisdiction jurisdiction) => jurisdiction.Value;

  /// <summary>Determines whether two <see cref="D1Jurisdiction" /> values are equal.</summary>
  public static bool operator ==(D1Jurisdiction left, D1Jurisdiction right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="D1Jurisdiction" /> values are not equal.</summary>
  public static bool operator !=(D1Jurisdiction left, D1Jurisdiction right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(D1Jurisdiction other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is D1Jurisdiction other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
