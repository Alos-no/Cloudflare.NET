namespace Cloudflare.NET.Security.Rulesets.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents an action that can be used when overriding rules in a Managed WAF Ruleset.
///   <para>
///     Override actions allow you to customize the behavior of individual rules or categories
///     within a managed ruleset without modifying the ruleset itself.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing custom values
///     for new actions that may be added to the Cloudflare API in the future.
///   </para>
///   <para>
///     Use the static properties for known actions (e.g., <see cref="Block" />, <see cref="Log" />)
///     or create custom values using the constructor or implicit string conversion.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known action with IntelliSense
///   var override = new RuleOverride("rule-id", ManagedWafOverrideAction.Log);
///
///   // Using Default to remove a previously set override
///   var removeOverride = new RuleOverride("rule-id", ManagedWafOverrideAction.Default);
///
///   // Using implicit conversion from string for future actions
///   ManagedWafOverrideAction customAction = "new-action";
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/waf/managed-rules/reference/cloudflare-managed-ruleset/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<ManagedWafOverrideAction>))]
public readonly struct ManagedWafOverrideAction : IExtensibleEnum<ManagedWafOverrideAction>, IEquatable<ManagedWafOverrideAction>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this action.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this action.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>Managed Challenge action - dynamically chooses the appropriate challenge type.</summary>
  /// <remarks>
  ///   Cloudflare will choose between a non-interactive challenge, managed challenge, or
  ///   interactive challenge based on the characteristics of the request.
  /// </remarks>
  public static ManagedWafOverrideAction ManagedChallenge { get; } = new("managed_challenge");

  /// <summary>Interactive Challenge action - presents a challenge page to the visitor.</summary>
  /// <remarks>
  ///   Requires the visitor to complete an interactive challenge before proceeding.
  /// </remarks>
  public static ManagedWafOverrideAction Challenge { get; } = new("challenge");

  /// <summary>JavaScript Challenge action - presents an Under Attack mode interstitial page.</summary>
  /// <remarks>
  ///   The visitor or client must support JavaScript. The challenge is automatically
  ///   satisfied by browsers but blocks most bots.
  /// </remarks>
  public static ManagedWafOverrideAction JsChallenge { get; } = new("js_challenge");

  /// <summary>Block action - denies matching requests.</summary>
  /// <remarks>Returns a 403 Forbidden status code to the client.</remarks>
  public static ManagedWafOverrideAction Block { get; } = new("block");

  /// <summary>Log action - records matching requests without taking any other action.</summary>
  /// <remarks>
  ///   Only records the request in logs. Useful for testing rules before enforcing them.
  ///   Available to Enterprise customers only.
  /// </remarks>
  public static ManagedWafOverrideAction Log { get; } = new("log");

  /// <summary>Default action - removes a previously set override.</summary>
  /// <remarks>
  ///   Use this to revert a rule or category back to its default behavior
  ///   as defined in the managed ruleset.
  /// </remarks>
  public static ManagedWafOverrideAction Default { get; } = new("default");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="ManagedWafOverrideAction" /> with the specified value.</summary>
  /// <param name="value">The string value representing the action.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public ManagedWafOverrideAction(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static ManagedWafOverrideAction Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="ManagedWafOverrideAction" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator ManagedWafOverrideAction(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="ManagedWafOverrideAction" /> to its string value.</summary>
  /// <param name="action">The action to convert.</param>
  public static implicit operator string(ManagedWafOverrideAction action) => action.Value;

  /// <summary>Determines whether two <see cref="ManagedWafOverrideAction" /> values are equal.</summary>
  public static bool operator ==(ManagedWafOverrideAction left, ManagedWafOverrideAction right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="ManagedWafOverrideAction" /> values are not equal.</summary>
  public static bool operator !=(ManagedWafOverrideAction left, ManagedWafOverrideAction right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(ManagedWafOverrideAction other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is ManagedWafOverrideAction other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
