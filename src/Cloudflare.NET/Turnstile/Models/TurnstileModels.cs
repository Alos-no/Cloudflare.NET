namespace Cloudflare.NET.Turnstile.Models;

using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Core.Json;
using Security.Firewall.Models;


#region Extensible Enums


/// <summary>
///   Turnstile widget mode determining user interaction level.
///   <para>
///     Widget modes control how the Turnstile challenge is presented to users,
///     ranging from fully invisible to requiring explicit user interaction.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing
///     custom values for new modes that may be added to the Cloudflare API in the future.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known mode
///   var mode = WidgetMode.Invisible;
///
///   // Checking widget mode
///   if (widget.Mode == WidgetMode.Managed) { ... }
///
///   // Using implicit conversion from string
///   WidgetMode customMode = "future-mode";
///   </code>
/// </example>
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<WidgetMode>))]
public readonly struct WidgetMode : IExtensibleEnum<WidgetMode>, IEquatable<WidgetMode>
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

  /// <summary>No visible challenge - automatic verification.</summary>
  /// <remarks>
  ///   The widget runs completely in the background without any user interaction.
  ///   Best for maintaining user experience while still providing bot protection.
  /// </remarks>
  public static WidgetMode Invisible { get; } = new("invisible");

  /// <summary>Cloudflare manages when to show challenge.</summary>
  /// <remarks>
  ///   Cloudflare dynamically decides whether to show a challenge based on
  ///   various signals. This provides a balance between security and user experience.
  /// </remarks>
  public static WidgetMode Managed { get; } = new("managed");

  /// <summary>Always requires user interaction.</summary>
  /// <remarks>
  ///   The widget always displays a challenge that requires user action.
  ///   Use when you need explicit user verification for high-security scenarios.
  /// </remarks>
  public static WidgetMode NonInteractive { get; } = new("non-interactive");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="WidgetMode" /> with the specified value.</summary>
  /// <param name="value">The string value representing the mode.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public WidgetMode(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static WidgetMode Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="WidgetMode" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator WidgetMode(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="WidgetMode" /> to its string value.</summary>
  /// <param name="mode">The mode to convert.</param>
  public static implicit operator string(WidgetMode mode) => mode.Value;

  /// <summary>Determines whether two <see cref="WidgetMode" /> values are equal.</summary>
  public static bool operator ==(WidgetMode left, WidgetMode right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="WidgetMode" /> values are not equal.</summary>
  public static bool operator !=(WidgetMode left, WidgetMode right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(WidgetMode other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is WidgetMode other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}


/// <summary>
///   Clearance level determining challenge difficulty.
///   <para>
///     Clearance levels control the complexity of the challenge presented
///     to users based on security requirements.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing
///     custom values for new levels that may be added to the Cloudflare API in the future.
///   </para>
/// </remarks>
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<ClearanceLevel>))]
public readonly struct ClearanceLevel : IExtensibleEnum<ClearanceLevel>, IEquatable<ClearanceLevel>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this clearance level.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this clearance level.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>No challenge - simply checks for bot behavior.</summary>
  /// <remarks>
  ///   The least intrusive level that only monitors for obvious bot patterns.
  /// </remarks>
  public static ClearanceLevel NoClearance { get; } = new("no_clearance");

  /// <summary>JavaScript challenge execution.</summary>
  /// <remarks>
  ///   Requires the client to execute JavaScript to prove it's a real browser.
  /// </remarks>
  public static ClearanceLevel JsChallenge { get; } = new("jschallenge");

  /// <summary>Managed challenge level.</summary>
  /// <remarks>
  ///   Cloudflare manages the challenge difficulty based on risk assessment.
  /// </remarks>
  public static ClearanceLevel Managed { get; } = new("managed");

  /// <summary>Interactive challenge requiring user action.</summary>
  /// <remarks>
  ///   The most secure level that requires explicit user interaction.
  /// </remarks>
  public static ClearanceLevel Interactive { get; } = new("interactive");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="ClearanceLevel" /> with the specified value.</summary>
  /// <param name="value">The string value representing the clearance level.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public ClearanceLevel(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static ClearanceLevel Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="ClearanceLevel" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator ClearanceLevel(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="ClearanceLevel" /> to its string value.</summary>
  /// <param name="level">The level to convert.</param>
  public static implicit operator string(ClearanceLevel level) => level.Value;

  /// <summary>Determines whether two <see cref="ClearanceLevel" /> values are equal.</summary>
  public static bool operator ==(ClearanceLevel left, ClearanceLevel right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="ClearanceLevel" /> values are not equal.</summary>
  public static bool operator !=(ClearanceLevel left, ClearanceLevel right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(ClearanceLevel other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is ClearanceLevel other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}


#endregion


#region Entity Models


/// <summary>
///   Represents a Turnstile widget configuration.
///   <para>
///     Turnstile widgets provide bot protection without user friction. Each widget
///     is identified by a public sitekey used in frontend integration, and has a
///     secret key for server-side verification.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     The <see cref="Secret" /> property is only populated when the widget is created
///     or when the secret is rotated. Subsequent GET requests will return null for this field.
///   </para>
/// </remarks>
/// <param name="Sitekey">Widget public key used in frontend integration.</param>
/// <param name="Name">Widget name for identification.</param>
/// <param name="Mode">Widget mode determining user interaction level.</param>
/// <param name="Domains">Allowed domains/hostnames for this widget.</param>
/// <param name="CreatedOn">When the widget was created.</param>
/// <param name="ModifiedOn">When the widget was last modified.</param>
/// <param name="BotFightMode">Whether bot fight mode is enabled.</param>
/// <param name="ClearanceLevel">Challenge clearance level.</param>
/// <param name="EphemeralId">Whether to use ephemeral IDs.</param>
/// <param name="Offlabel">Whether off-label use is allowed.</param>
/// <param name="Region">Geographic region for the widget.</param>
/// <param name="Secret">Widget secret key. Only returned on create or rotate.</param>
public record TurnstileWidget(
  [property: JsonPropertyName("sitekey")]
  string Sitekey,

  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("mode")]
  WidgetMode Mode,

  [property: JsonPropertyName("domains")]
  IReadOnlyList<string> Domains,

  [property: JsonPropertyName("created_on")]
  DateTime CreatedOn,

  [property: JsonPropertyName("modified_on")]
  DateTime ModifiedOn,

  [property: JsonPropertyName("bot_fight_mode")]
  bool BotFightMode = false,

  [property: JsonPropertyName("clearance_level")]
  ClearanceLevel? ClearanceLevel = null,

  [property: JsonPropertyName("ephemeral_id")]
  bool EphemeralId = false,

  [property: JsonPropertyName("offlabel")]
  bool Offlabel = false,

  [property: JsonPropertyName("region")]
  string? Region = null,

  [property: JsonPropertyName("secret")]
  string? Secret = null
);


#endregion


#region Request Models


/// <summary>
///   Request to create a new Turnstile widget.
///   <para>
///     The created widget will have a unique sitekey (public key) and secret (private key).
///     The secret is only returned in the create response - store it securely.
///   </para>
/// </summary>
/// <param name="Name">Widget name for identification.</param>
/// <param name="Domains">Allowed domains for this widget.</param>
/// <param name="Mode">Widget mode (invisible, managed, or non-interactive).</param>
/// <param name="BotFightMode">Enable bot fight mode. Defaults to false if not specified.</param>
/// <param name="ClearanceLevel">Challenge clearance level.</param>
/// <param name="EphemeralId">Use ephemeral IDs. Defaults to false if not specified.</param>
/// <param name="Offlabel">Allow off-label use. Defaults to false if not specified.</param>
/// <param name="Region">Geographic region for the widget.</param>
public record CreateTurnstileWidgetRequest(
  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("domains")]
  IReadOnlyList<string> Domains,

  [property: JsonPropertyName("mode")]
  WidgetMode Mode,

  [property: JsonPropertyName("bot_fight_mode")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? BotFightMode = null,

  [property: JsonPropertyName("clearance_level")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  ClearanceLevel? ClearanceLevel = null,

  [property: JsonPropertyName("ephemeral_id")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? EphemeralId = null,

  [property: JsonPropertyName("offlabel")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? Offlabel = null,

  [property: JsonPropertyName("region")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Region = null
);


/// <summary>
///   Request to update an existing Turnstile widget.
///   <para>
///     Updates replace the widget configuration. All required fields must be provided.
///   </para>
/// </summary>
/// <param name="Name">Updated widget name.</param>
/// <param name="Domains">Updated allowed domains.</param>
/// <param name="Mode">Updated widget mode.</param>
/// <param name="BotFightMode">Updated bot fight mode setting.</param>
/// <param name="ClearanceLevel">Updated clearance level.</param>
public record UpdateTurnstileWidgetRequest(
  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("domains")]
  IReadOnlyList<string> Domains,

  [property: JsonPropertyName("mode")]
  WidgetMode Mode,

  [property: JsonPropertyName("bot_fight_mode")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? BotFightMode = null,

  [property: JsonPropertyName("clearance_level")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  ClearanceLevel? ClearanceLevel = null
);


/// <summary>
///   Request to rotate a widget's secret key.
///   <para>
///     By default, the old secret remains valid for 2 hours to allow graceful migration.
///   </para>
/// </summary>
/// <param name="InvalidateImmediately">
///   If true, invalidates the previous secret immediately.
///   If false (default), previous secret remains valid for 2 hours.
/// </param>
public record RotateWidgetSecretRequest(
  [property: JsonPropertyName("invalidate_immediately")]
  bool InvalidateImmediately = false
);


/// <summary>
///   Result of rotating a widget's secret.
/// </summary>
/// <param name="Secret">The new secret key. Store this securely.</param>
public record RotateWidgetSecretResult(
  [property: JsonPropertyName("secret")]
  string Secret
);


#endregion


#region Filter Models


/// <summary>
///   Filtering and pagination options for listing Turnstile widgets.
/// </summary>
/// <param name="Order">Field to order results by.</param>
/// <param name="Direction">Sort direction (asc or desc).</param>
/// <param name="Page">Page number (minimum 1, default 1).</param>
/// <param name="PerPage">Results per page (5-1000, default 25).</param>
public record ListTurnstileWidgetsFilters(
  TurnstileOrderField? Order = null,
  ListOrderDirection? Direction = null,
  int? Page = null,
  int? PerPage = null
);


/// <summary>
///   Fields available for ordering Turnstile widget results.
/// </summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum TurnstileOrderField
{
  /// <summary>Order by widget ID.</summary>
  [EnumMember(Value = "id")] Id,

  /// <summary>Order by sitekey (public key).</summary>
  [EnumMember(Value = "sitekey")] Sitekey,

  /// <summary>Order by widget name.</summary>
  [EnumMember(Value = "name")] Name,

  /// <summary>Order by creation date.</summary>
  [EnumMember(Value = "created_on")] CreatedOn,

  /// <summary>Order by last modification date.</summary>
  [EnumMember(Value = "modified_on")] ModifiedOn
}


#endregion
