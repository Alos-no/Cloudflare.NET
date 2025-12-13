namespace Cloudflare.NET.Accounts.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents the type of a Cloudflare account.
///   <para>
///     Account types indicate the tier or category of the account,
///     such as standard or enterprise.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing custom values
///     for new account types that may be added to the Cloudflare API in the future.
///   </para>
///   <para>
///     Use the static properties for known types (e.g., <see cref="Standard" />, <see cref="Enterprise" />)
///     or create custom values using the constructor or implicit string conversion.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known type with IntelliSense
///   var accountType = AccountType.Standard;
///
///   // Checking account type
///   if (account.Type == AccountType.Enterprise) { ... }
///
///   // Using implicit conversion from string
///   AccountType customType = "new-type";
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/api/resources/accounts/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<AccountType>))]
public readonly struct AccountType : IExtensibleEnum<AccountType>, IEquatable<AccountType>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this account type.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this account type.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>Standard Cloudflare account type.</summary>
  /// <remarks>
  ///   This is the most common account type used by individual users and small businesses.
  /// </remarks>
  public static AccountType Standard { get; } = new("standard");

  /// <summary>Enterprise Cloudflare account type.</summary>
  /// <remarks>
  ///   Enterprise accounts have access to additional features, support options,
  ///   and higher resource limits.
  /// </remarks>
  public static AccountType Enterprise { get; } = new("enterprise");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="AccountType" /> with the specified value.</summary>
  /// <param name="value">The string value representing the account type.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public AccountType(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static AccountType Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to an <see cref="AccountType" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator AccountType(string value) => new(value);

  /// <summary>Implicitly converts an <see cref="AccountType" /> to its string value.</summary>
  /// <param name="type">The account type to convert.</param>
  public static implicit operator string(AccountType type) => type.Value;

  /// <summary>Determines whether two <see cref="AccountType" /> values are equal.</summary>
  public static bool operator ==(AccountType left, AccountType right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="AccountType" /> values are not equal.</summary>
  public static bool operator !=(AccountType left, AccountType right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(AccountType other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is AccountType other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
