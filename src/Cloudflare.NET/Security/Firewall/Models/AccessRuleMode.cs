namespace Cloudflare.NET.Security.Firewall.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents the action to take for an IP Access Rule.
///   <para>
///     Access rule modes define the behavior when a request matches the rule's configuration.
///     Options range from blocking to allowing, with various challenge types in between.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing custom values
///     for new modes that may be added to the Cloudflare API in the future.
///   </para>
///   <para>
///     Use the static properties for known modes (e.g., <see cref="Block" />, <see cref="Challenge" />)
///     or create custom values using the constructor or implicit string conversion.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known mode with IntelliSense
///   var mode = AccessRuleMode.Block;
///
///   // Using implicit conversion from string
///   AccessRuleMode customMode = "new-mode";
///
///   // Comparison
///   if (rule.Mode == AccessRuleMode.Whitelist) { ... }
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/waf/tools/ip-access-rules/actions/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<AccessRuleMode>))]
public readonly struct AccessRuleMode : IExtensibleEnum<AccessRuleMode>, IEquatable<AccessRuleMode>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this mode.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this mode.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>Block action - denies access to matching requests.</summary>
  /// <remarks>
  ///   Prevents a visitor from visiting your site. Returns a 403 Forbidden status code.
  /// </remarks>
  public static AccessRuleMode Block { get; } = new("block");

  /// <summary>Interactive Challenge action - presents a challenge page to the visitor.</summary>
  /// <remarks>
  ///   Requires the visitor to complete an interactive challenge before visiting your site.
  ///   Prevents bots from accessing the site.
  /// </remarks>
  public static AccessRuleMode Challenge { get; } = new("challenge");

  /// <summary>JavaScript Challenge action - presents an Under Attack mode interstitial page.</summary>
  /// <remarks>
  ///   The visitor or client must support JavaScript. The challenge is automatically
  ///   satisfied by browsers but blocks most bots. Useful for blocking DDoS attacks
  ///   with minimal impact to legitimate visitors.
  /// </remarks>
  public static AccessRuleMode JsChallenge { get; } = new("js_challenge");

  /// <summary>Managed Challenge action - dynamically chooses the appropriate challenge type.</summary>
  /// <remarks>
  ///   Cloudflare will dynamically choose the appropriate type of challenge based on
  ///   the characteristics of a request. This helps reduce the time humans spend solving
  ///   CAPTCHAs while maintaining security.
  /// </remarks>
  public static AccessRuleMode ManagedChallenge { get; } = new("managed_challenge");

  /// <summary>Whitelist (Allow) action - excludes visitors from all security checks.</summary>
  /// <remarks>
  ///   Excludes visitors from all security checks, including Browser Integrity Check,
  ///   Under Attack mode, and the WAF. Use this option when a trusted visitor is being
  ///   blocked by Cloudflare's default security features. The Allow action takes
  ///   precedence over the Block action.
  /// </remarks>
  public static AccessRuleMode Whitelist { get; } = new("whitelist");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="AccessRuleMode" /> with the specified value.</summary>
  /// <param name="value">The string value representing the mode.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public AccessRuleMode(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static AccessRuleMode Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to an <see cref="AccessRuleMode" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator AccessRuleMode(string value) => new(value);

  /// <summary>Implicitly converts an <see cref="AccessRuleMode" /> to its string value.</summary>
  /// <param name="mode">The mode to convert.</param>
  public static implicit operator string(AccessRuleMode mode) => mode.Value;

  /// <summary>Determines whether two <see cref="AccessRuleMode" /> values are equal.</summary>
  public static bool operator ==(AccessRuleMode left, AccessRuleMode right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="AccessRuleMode" /> values are not equal.</summary>
  public static bool operator !=(AccessRuleMode left, AccessRuleMode right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(AccessRuleMode other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is AccessRuleMode other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
