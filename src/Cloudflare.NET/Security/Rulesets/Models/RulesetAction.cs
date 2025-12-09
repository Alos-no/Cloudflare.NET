namespace Cloudflare.NET.Security.Rulesets.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;

/// <summary>
///   Represents an action that can be performed by a rule in the Cloudflare Ruleset Engine.
///   <para>
///     Actions define what happens when a rule's expression matches an incoming request.
///     Different actions are available depending on the ruleset phase being configured.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing custom values
///     for new actions that may be added to the Cloudflare API in the future.
///   </para>
///   <para>
///     Use the static properties for known actions (e.g., <see cref="Block" />, <see cref="Challenge" />)
///     or create custom values using the constructor or implicit string conversion.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known action with IntelliSense
///   var action = RulesetAction.Block;
///
///   // Using implicit conversion from string
///   RulesetAction customAction = "new-action";
///
///   // Comparison
///   if (rule.Action == RulesetAction.ManagedChallenge) { ... }
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/ruleset-engine/rules-language/actions/" />
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<RulesetAction>))]
public readonly struct RulesetAction : IExtensibleEnum<RulesetAction>, IEquatable<RulesetAction>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this action.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this action.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values - Terminating Actions

  /// <summary>Block action - denies matching requests.</summary>
  /// <remarks>
  ///   Returns a 403 Forbidden status code to the client. For rate limiting rules,
  ///   returns a 429 Too Many Requests status code.
  /// </remarks>
  public static RulesetAction Block { get; } = new("block");

  /// <summary>Interactive Challenge action - presents a challenge page to the visitor.</summary>
  /// <remarks>
  ///   Requires the visitor to complete an interactive challenge before proceeding.
  ///   Useful for ensuring that the visitor accessing the site is human.
  /// </remarks>
  public static RulesetAction Challenge { get; } = new("challenge");

  /// <summary>JavaScript Challenge action - presents an Under Attack mode interstitial page.</summary>
  /// <remarks>
  ///   The visitor or client must support JavaScript. The challenge is automatically
  ///   satisfied by browsers but blocks most bots. Useful for blocking DDoS attacks.
  /// </remarks>
  public static RulesetAction JsChallenge { get; } = new("js_challenge");

  /// <summary>Managed Challenge action - dynamically chooses the appropriate challenge type.</summary>
  /// <remarks>
  ///   Cloudflare will choose between a non-interactive challenge, managed challenge, or
  ///   interactive challenge based on the characteristics of the request. This helps reduce
  ///   the time humans spend solving CAPTCHAs while maintaining security.
  /// </remarks>
  public static RulesetAction ManagedChallenge { get; } = new("managed_challenge");

  /// <summary>Redirect action - navigates the user from a source URL to a target URL.</summary>
  /// <remarks>
  ///   Returns an HTTP redirect response (301 or 302) to the client.
  ///   Requires action parameters specifying the redirect target.
  /// </remarks>
  public static RulesetAction Redirect { get; } = new("redirect");

  /// <summary>Serve Error action - delivers custom error content to the client.</summary>
  /// <remarks>
  ///   Returns a custom error page with the specified content and status code.
  ///   Available in specific phases and plans.
  /// </remarks>
  public static RulesetAction ServeError { get; } = new("serve_error");

  #endregion


  #region Known Values - Non-Terminating Actions

  /// <summary>Log action - records matching requests without taking any other action.</summary>
  /// <remarks>
  ///   Only records the request in logs. Useful for testing rules before enforcing them.
  ///   Available to Enterprise customers only.
  /// </remarks>
  public static RulesetAction Log { get; } = new("log");

  /// <summary>Skip action - allows users to dynamically skip security features or products.</summary>
  /// <remarks>
  ///   Can skip specific products, rulesets, or rules. Requires action parameters
  ///   specifying what to skip.
  /// </remarks>
  public static RulesetAction Skip { get; } = new("skip");

  /// <summary>Execute action - executes a specified managed or custom ruleset.</summary>
  /// <remarks>
  ///   Used to deploy rulesets by adding a rule with this action to a phase entry point.
  ///   Requires action parameters specifying the ruleset ID to execute.
  /// </remarks>
  public static RulesetAction Execute { get; } = new("execute");

  /// <summary>Rewrite action - modifies the URI path, query string, or HTTP headers.</summary>
  /// <remarks>
  ///   Rewrites various parts of the request before it continues processing.
  ///   Used in Transform Rules.
  /// </remarks>
  public static RulesetAction Rewrite { get; } = new("rewrite");

  /// <summary>Route action - adjusts Host header, SNI, hostname, and destination port.</summary>
  /// <remarks>
  ///   Used in Origin Rules to modify how requests are routed to origin servers.
  /// </remarks>
  public static RulesetAction Route { get; } = new("route");

  /// <summary>Set Configuration action - changes Cloudflare product settings.</summary>
  /// <remarks>
  ///   Modifies the configuration of various Cloudflare features for matching requests.
  ///   Used in Configuration Rules.
  /// </remarks>
  public static RulesetAction SetConfig { get; } = new("set_config");

  /// <summary>Compress Response action - defines compression settings for responses.</summary>
  /// <remarks>
  ///   Configures compression algorithms and settings for the HTTP response.
  ///   Used in Compression Rules.
  /// </remarks>
  public static RulesetAction CompressResponse { get; } = new("compress_response");

  /// <summary>Set Cache Settings action - customizes cache behavior.</summary>
  /// <remarks>
  ///   Configures caching behavior including TTL, bypass conditions, and cache keys.
  ///   Used in Cache Rules.
  /// </remarks>
  public static RulesetAction SetCacheSettings { get; } = new("set_cache_settings");

  /// <summary>Log Custom Field action - configures Logpush custom fields.</summary>
  /// <remarks>
  ///   Adds custom fields to Logpush output for matching requests.
  ///   Available to Enterprise customers with Logpush enabled.
  /// </remarks>
  public static RulesetAction LogCustomField { get; } = new("log_custom_field");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="RulesetAction" /> with the specified value.</summary>
  /// <param name="value">The string value representing the action.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public RulesetAction(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static RulesetAction Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="RulesetAction" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator RulesetAction(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="RulesetAction" /> to its string value.</summary>
  /// <param name="action">The action to convert.</param>
  public static implicit operator string(RulesetAction action) => action.Value;

  /// <summary>Determines whether two <see cref="RulesetAction" /> values are equal.</summary>
  public static bool operator ==(RulesetAction left, RulesetAction right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="RulesetAction" /> values are not equal.</summary>
  public static bool operator !=(RulesetAction left, RulesetAction right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(RulesetAction other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is RulesetAction other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}
