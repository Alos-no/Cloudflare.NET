namespace Cloudflare.NET.Security.Firewall.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents the action to take for a User-Agent blocking rule.
///   <para>
///     User-Agent blocking modes define what happens when a request's User-Agent header
///     matches the rule's configured pattern.
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
///   var mode = UaRuleMode.Block;
///
///   // Creating a rule with a specific mode
///   var rule = new CreateUaRuleRequest(UaRuleMode.ManagedChallenge, config);
///
///   // Using implicit conversion from string
///   UaRuleMode customMode = "new-mode";
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/waf/tools/user-agent-blocking/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<UaRuleMode>))]
public readonly struct UaRuleMode : IExtensibleEnum<UaRuleMode>, IEquatable<UaRuleMode>
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

  /// <summary>Block action - prevents access from matching User-Agents.</summary>
  /// <remarks>
  ///   Prevents a visitor from visiting your site. Returns a 403 Forbidden status code.
  /// </remarks>
  public static UaRuleMode Block { get; } = new("block");

  /// <summary>Interactive Challenge action - presents a challenge page to the visitor.</summary>
  /// <remarks>
  ///   Requires the visitor to complete an interactive challenge before visiting your site.
  ///   Prevents bots from accessing the site.
  /// </remarks>
  public static UaRuleMode Challenge { get; } = new("challenge");

  /// <summary>JavaScript Challenge action - presents an Under Attack mode interstitial page.</summary>
  /// <remarks>
  ///   The visitor or client must support JavaScript. The challenge is automatically
  ///   satisfied by browsers but blocks most bots.
  /// </remarks>
  public static UaRuleMode JsChallenge { get; } = new("js_challenge");

  /// <summary>Managed Challenge action - dynamically chooses the appropriate challenge type.</summary>
  /// <remarks>
  ///   Cloudflare will dynamically choose the appropriate type of challenge based on
  ///   the characteristics of a request.
  /// </remarks>
  public static UaRuleMode ManagedChallenge { get; } = new("managed_challenge");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="UaRuleMode" /> with the specified value.</summary>
  /// <param name="value">The string value representing the mode.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public UaRuleMode(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static UaRuleMode Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="UaRuleMode" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator UaRuleMode(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="UaRuleMode" /> to its string value.</summary>
  /// <param name="mode">The mode to convert.</param>
  public static implicit operator string(UaRuleMode mode) => mode.Value;

  /// <summary>Determines whether two <see cref="UaRuleMode" /> values are equal.</summary>
  public static bool operator ==(UaRuleMode left, UaRuleMode right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="UaRuleMode" /> values are not equal.</summary>
  public static bool operator !=(UaRuleMode left, UaRuleMode right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(UaRuleMode other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is UaRuleMode other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
