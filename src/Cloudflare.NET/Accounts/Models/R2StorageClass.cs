namespace Cloudflare.NET.Accounts.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents a storage class for R2 objects.
///   <para>
///     Storage classes determine the cost and access characteristics of stored objects.
///     Different classes offer trade-offs between storage cost and retrieval latency/cost.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing custom values
///     for new storage classes that may be added to the Cloudflare API in the future.
///   </para>
///   <para>
///     The storage class can be set at the bucket level (as a default for new objects) or specified
///     per-object during upload. Lifecycle rules can also transition objects between storage classes.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known storage class with IntelliSense
///   var storageClass = R2StorageClass.InfrequentAccess;
///
///   // Using implicit conversion from string
///   R2StorageClass customClass = "new-storage-class";
///
///   // In lifecycle transitions
///   new StorageClassTransition(
///     LifecycleCondition.AfterDays(30),
///     R2StorageClass.InfrequentAccess
///   );
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/r2/api/workers/workers-api-reference/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<R2StorageClass>))]
public readonly struct R2StorageClass : IExtensibleEnum<R2StorageClass>, IEquatable<R2StorageClass>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this storage class.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this storage class.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>Standard storage class for frequently accessed data.</summary>
  /// <remarks>
  ///   <para>
  ///     This is the default storage class offering low-latency access with no minimum storage duration.
  ///     Ideal for frequently accessed data, active content, and data requiring immediate retrieval.
  ///   </para>
  ///   <para>Pricing: Standard storage rates with no retrieval fees.</para>
  /// </remarks>
  public static R2StorageClass Standard { get; } = new("Standard");

  /// <summary>Infrequent Access storage class for less frequently accessed data.</summary>
  /// <remarks>
  ///   <para>
  ///     Offers lower storage costs compared to Standard, but with retrieval fees and a minimum
  ///     storage duration. Ideal for backup data, disaster recovery, and data accessed less than
  ///     once per month.
  ///   </para>
  ///   <para>
  ///     Pricing: Lower storage rates with per-GB retrieval fees. Objects have a minimum billable
  ///     storage duration.
  ///   </para>
  /// </remarks>
  public static R2StorageClass InfrequentAccess { get; } = new("InfrequentAccess");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="R2StorageClass" /> with the specified value.</summary>
  /// <param name="value">The string value representing the storage class.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public R2StorageClass(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static R2StorageClass Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to an <see cref="R2StorageClass" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator R2StorageClass(string value) => new(value);

  /// <summary>Implicitly converts an <see cref="R2StorageClass" /> to its string value.</summary>
  /// <param name="storageClass">The storage class to convert.</param>
  public static implicit operator string(R2StorageClass storageClass) => storageClass.Value;

  /// <summary>Determines whether two <see cref="R2StorageClass" /> values are equal.</summary>
  public static bool operator ==(R2StorageClass left, R2StorageClass right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="R2StorageClass" /> values are not equal.</summary>
  public static bool operator !=(R2StorageClass left, R2StorageClass right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(R2StorageClass other) =>
    string.Equals(Value, other.Value, StringComparison.Ordinal);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is R2StorageClass other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
