namespace Cloudflare.NET.Subscriptions.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;


#region SubscriptionFrequency Extensible Enum

/// <summary>
///   Represents the billing frequency of a subscription.
///   <para>
///     Subscription frequencies define how often billing occurs:
///     <list type="bullet">
///       <item>
///         <term>Weekly</term>
///         <description>Billing occurs every week</description>
///       </item>
///       <item>
///         <term>Monthly</term>
///         <description>Billing occurs every month</description>
///       </item>
///       <item>
///         <term>Quarterly</term>
///         <description>Billing occurs every three months</description>
///       </item>
///       <item>
///         <term>Yearly</term>
///         <description>Billing occurs once per year</description>
///       </item>
///     </list>
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing
///     custom values for new frequencies that may be added to the Cloudflare API in the future.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known frequency with IntelliSense
///   var frequency = SubscriptionFrequency.Monthly;
///
///   // Checking subscription frequency
///   if (subscription.Frequency == SubscriptionFrequency.Yearly) { ... }
///
///   // Using implicit conversion from string
///   SubscriptionFrequency customFrequency = "biannual";
///   </code>
/// </example>
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<SubscriptionFrequency>))]
public readonly struct SubscriptionFrequency : IExtensibleEnum<SubscriptionFrequency>, IEquatable<SubscriptionFrequency>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this subscription frequency.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this subscription frequency.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>
  ///   Billing occurs every week.
  /// </summary>
  public static SubscriptionFrequency Weekly { get; } = new("weekly");

  /// <summary>
  ///   Billing occurs every month.
  ///   <para>
  ///     This is the most common billing frequency for Cloudflare subscriptions.
  ///   </para>
  /// </summary>
  public static SubscriptionFrequency Monthly { get; } = new("monthly");

  /// <summary>
  ///   Billing occurs every three months.
  /// </summary>
  public static SubscriptionFrequency Quarterly { get; } = new("quarterly");

  /// <summary>
  ///   Billing occurs once per year.
  ///   <para>
  ///     Annual subscriptions often come with a discount compared to monthly billing.
  ///   </para>
  /// </summary>
  public static SubscriptionFrequency Yearly { get; } = new("yearly");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="SubscriptionFrequency"/> with the specified value.</summary>
  /// <param name="value">The string value representing the subscription frequency.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
  public SubscriptionFrequency(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static SubscriptionFrequency Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="SubscriptionFrequency"/>.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator SubscriptionFrequency(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="SubscriptionFrequency"/> to its string value.</summary>
  /// <param name="frequency">The subscription frequency to convert.</param>
  public static implicit operator string(SubscriptionFrequency frequency) => frequency.Value;

  /// <summary>Determines whether two <see cref="SubscriptionFrequency"/> values are equal.</summary>
  public static bool operator ==(SubscriptionFrequency left, SubscriptionFrequency right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="SubscriptionFrequency"/> values are not equal.</summary>
  public static bool operator !=(SubscriptionFrequency left, SubscriptionFrequency right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(SubscriptionFrequency other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is SubscriptionFrequency other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}

#endregion


#region SubscriptionState Extensible Enum

/// <summary>
///   Represents the state of a subscription.
///   <para>
///     Subscription states indicate the lifecycle stage of the subscription:
///     <list type="bullet">
///       <item>
///         <term>Trial</term>
///         <description>The subscription is in a trial period</description>
///       </item>
///       <item>
///         <term>Provisioned</term>
///         <description>The subscription has been provisioned but not yet paid</description>
///       </item>
///       <item>
///         <term>Paid</term>
///         <description>The subscription is active and paid</description>
///       </item>
///       <item>
///         <term>AwaitingPayment</term>
///         <description>The subscription is awaiting payment</description>
///       </item>
///       <item>
///         <term>Cancelled</term>
///         <description>The subscription has been cancelled</description>
///       </item>
///       <item>
///         <term>Failed</term>
///         <description>A payment or provisioning failure occurred</description>
///       </item>
///       <item>
///         <term>Expired</term>
///         <description>The subscription has expired</description>
///       </item>
///     </list>
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing
///     custom values for new states that may be added to the Cloudflare API in the future.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known state with IntelliSense
///   var state = SubscriptionState.Paid;
///
///   // Checking subscription state
///   if (subscription.State == SubscriptionState.AwaitingPayment) { ... }
///
///   // Using implicit conversion from string
///   SubscriptionState customState = "suspended";
///   </code>
/// </example>
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<SubscriptionState>))]
public readonly struct SubscriptionState : IExtensibleEnum<SubscriptionState>, IEquatable<SubscriptionState>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this subscription state.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this subscription state.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>
  ///   The subscription is in a trial period.
  ///   <para>
  ///     Trial subscriptions provide temporary access to features before payment is required.
  ///   </para>
  /// </summary>
  public static SubscriptionState Trial { get; } = new("Trial");

  /// <summary>
  ///   The subscription has been provisioned but not yet paid.
  ///   <para>
  ///     The subscription is active but awaiting initial payment confirmation.
  ///   </para>
  /// </summary>
  public static SubscriptionState Provisioned { get; } = new("Provisioned");

  /// <summary>
  ///   The subscription is active and paid.
  ///   <para>
  ///     This is the normal operational state for an active subscription.
  ///   </para>
  /// </summary>
  public static SubscriptionState Paid { get; } = new("Paid");

  /// <summary>
  ///   The subscription is awaiting payment.
  ///   <para>
  ///     Payment has not been received; the subscription may be suspended if payment is not made.
  ///   </para>
  /// </summary>
  public static SubscriptionState AwaitingPayment { get; } = new("AwaitingPayment");

  /// <summary>
  ///   The subscription has been cancelled.
  ///   <para>
  ///     The subscription is no longer active and features have been disabled.
  ///   </para>
  /// </summary>
  public static SubscriptionState Cancelled { get; } = new("Cancelled");

  /// <summary>
  ///   A payment or provisioning failure occurred.
  ///   <para>
  ///     The subscription could not be activated or renewed due to a failure.
  ///   </para>
  /// </summary>
  public static SubscriptionState Failed { get; } = new("Failed");

  /// <summary>
  ///   The subscription has expired.
  ///   <para>
  ///     The subscription term has ended and needs to be renewed.
  ///   </para>
  /// </summary>
  public static SubscriptionState Expired { get; } = new("Expired");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="SubscriptionState"/> with the specified value.</summary>
  /// <param name="value">The string value representing the subscription state.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
  public SubscriptionState(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static SubscriptionState Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="SubscriptionState"/>.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator SubscriptionState(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="SubscriptionState"/> to its string value.</summary>
  /// <param name="state">The subscription state to convert.</param>
  public static implicit operator string(SubscriptionState state) => state.Value;

  /// <summary>Determines whether two <see cref="SubscriptionState"/> values are equal.</summary>
  public static bool operator ==(SubscriptionState left, SubscriptionState right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="SubscriptionState"/> values are not equal.</summary>
  public static bool operator !=(SubscriptionState left, SubscriptionState right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(SubscriptionState other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is SubscriptionState other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}

#endregion


#region Subscription Model

/// <summary>
///   Represents a subscription for Cloudflare services.
///   <para>
///     Subscriptions manage billing plans and add-ons at account, user, or zone level.
///     Each subscription has a rate plan that defines the features included and their pricing.
///   </para>
/// </summary>
/// <param name="Id">Subscription identifier.</param>
/// <param name="State">Current state of the subscription.</param>
/// <param name="Price">Subscription price in the specified currency.</param>
/// <param name="Currency">Currency code (e.g., "USD").</param>
/// <param name="Frequency">Billing frequency (weekly, monthly, quarterly, yearly).</param>
/// <param name="RatePlan">The rate plan associated with this subscription.</param>
/// <param name="CurrentPeriodStart">Start of the current billing period.</param>
/// <param name="CurrentPeriodEnd">End of the current billing period.</param>
/// <param name="ComponentValues">Optional component values for configurable features.</param>
/// <example>
///   <code>
///   var subscriptions = await client.Subscriptions.ListAccountSubscriptionsAsync(accountId);
///   foreach (var sub in subscriptions)
///   {
///     Console.WriteLine($"{sub.RatePlan?.PublicName}: {sub.Price} {sub.Currency}/{sub.Frequency}");
///     Console.WriteLine($"  State: {sub.State}");
///     Console.WriteLine($"  Period: {sub.CurrentPeriodStart} to {sub.CurrentPeriodEnd}");
///   }
///   </code>
/// </example>
public record Subscription(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("state")]
  SubscriptionState State,

  [property: JsonPropertyName("price")]
  decimal Price,

  [property: JsonPropertyName("currency")]
  string Currency,

  [property: JsonPropertyName("frequency")]
  SubscriptionFrequency Frequency,

  [property: JsonPropertyName("rate_plan")]
  RatePlan? RatePlan = null,

  [property: JsonPropertyName("current_period_start")]
  DateTime? CurrentPeriodStart = null,

  [property: JsonPropertyName("current_period_end")]
  DateTime? CurrentPeriodEnd = null,

  [property: JsonPropertyName("component_values")]
  IReadOnlyList<SubscriptionComponent>? ComponentValues = null
);

#endregion


#region RatePlan Model

/// <summary>
///   Represents a rate plan for a subscription.
///   <para>
///     Rate plans define the features and pricing for Cloudflare subscriptions.
///     Each plan has a scope (account, zone, etc.) and may be externally managed.
///   </para>
/// </summary>
/// <param name="Id">Rate plan identifier (used when creating subscriptions).</param>
/// <param name="PublicName">Human-readable name of the rate plan.</param>
/// <param name="Currency">Currency code for pricing (e.g., "USD").</param>
/// <param name="Scope">Scope of the rate plan (e.g., "account", "zone").</param>
/// <param name="ExternallyManaged">Whether the plan is managed outside of Cloudflare (e.g., through partners).</param>
/// <example>
///   <code>
///   if (subscription.RatePlan is not null)
///   {
///     Console.WriteLine($"Plan: {subscription.RatePlan.PublicName}");
///     if (subscription.RatePlan.ExternallyManaged)
///       Console.WriteLine("  (Managed by partner)");
///   }
///   </code>
/// </example>
public record RatePlan(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("public_name")]
  string PublicName,

  [property: JsonPropertyName("currency")]
  string Currency,

  [property: JsonPropertyName("scope")]
  string? Scope = null,

  [property: JsonPropertyName("externally_managed")]
  bool ExternallyManaged = false
);

#endregion


#region SubscriptionComponent Model

/// <summary>
///   Represents a component value in a subscription.
///   <para>
///     Components are configurable features within a subscription that can have
///     variable quantities or settings, such as additional page rules or workers.
///   </para>
/// </summary>
/// <param name="Name">Component name (e.g., "page_rules", "workers").</param>
/// <param name="Value">Current value/quantity for this component.</param>
/// <param name="Default">Default value for this component (if applicable).</param>
/// <param name="Price">Additional price for this component (if applicable).</param>
public record SubscriptionComponent(
  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("value")]
  int Value,

  [property: JsonPropertyName("default")]
  int? Default = null,

  [property: JsonPropertyName("price")]
  decimal? Price = null
);

#endregion


#region Request Models

/// <summary>
///   Request to create a new account subscription.
///   <para>
///     Creating subscriptions may incur charges. Ensure you have the appropriate
///     billing permissions and understand the pricing of the selected rate plan.
///   </para>
/// </summary>
/// <param name="RatePlan">Reference to the rate plan for this subscription.</param>
/// <param name="Frequency">Optional billing frequency (defaults to plan default).</param>
/// <param name="ComponentValues">Optional component values for configurable features.</param>
/// <example>
///   <code>
///   var request = new CreateAccountSubscriptionRequest(
///     RatePlan: new RatePlanReference("enterprise_plan_id"),
///     Frequency: SubscriptionFrequency.Yearly,
///     ComponentValues: new[]
///     {
///       new SubscriptionComponentValue("page_rules", 100)
///     });
///
///   var subscription = await client.Subscriptions.CreateAccountSubscriptionAsync(accountId, request);
///   </code>
/// </example>
public record CreateAccountSubscriptionRequest(
  [property: JsonPropertyName("rate_plan")]
  RatePlanReference RatePlan,

  [property: JsonPropertyName("frequency")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  SubscriptionFrequency? Frequency = null,

  [property: JsonPropertyName("component_values")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<SubscriptionComponentValue>? ComponentValues = null
);


/// <summary>
///   Reference to a rate plan by ID.
///   <para>
///     Used when creating or updating subscriptions to specify which rate plan to use.
///   </para>
/// </summary>
/// <param name="Id">Rate plan identifier.</param>
public record RatePlanReference(
  [property: JsonPropertyName("id")]
  string Id
);


/// <summary>
///   Component value for subscription requests.
///   <para>
///     Used to set the value of configurable components when creating or updating subscriptions.
///   </para>
/// </summary>
/// <param name="Name">Component name (e.g., "page_rules", "workers").</param>
/// <param name="Value">Desired value/quantity for this component.</param>
public record SubscriptionComponentValue(
  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("value")]
  int Value
);


/// <summary>
///   Request to update an existing account subscription.
///   <para>
///     Updates may affect billing. Only specified fields will be changed;
///     null fields are left unchanged.
///   </para>
/// </summary>
/// <param name="RatePlan">Optional new rate plan reference.</param>
/// <param name="Frequency">Optional new billing frequency.</param>
/// <param name="ComponentValues">Optional new component values.</param>
/// <example>
///   <code>
///   // Update to yearly billing
///   var request = new UpdateAccountSubscriptionRequest(
///     Frequency: SubscriptionFrequency.Yearly);
///
///   var updated = await client.Subscriptions.UpdateAccountSubscriptionAsync(
///     accountId, subscriptionId, request);
///   </code>
/// </example>
public record UpdateAccountSubscriptionRequest(
  [property: JsonPropertyName("rate_plan")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  RatePlanReference? RatePlan = null,

  [property: JsonPropertyName("frequency")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  SubscriptionFrequency? Frequency = null,

  [property: JsonPropertyName("component_values")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<SubscriptionComponentValue>? ComponentValues = null
);


/// <summary>
///   Request to update an existing user subscription.
///   <para>
///     Updates may affect billing. Only specified fields will be changed;
///     null fields are left unchanged.
///   </para>
/// </summary>
/// <param name="Frequency">Optional new billing frequency.</param>
/// <param name="RatePlan">Optional new rate plan reference.</param>
/// <param name="ComponentValues">Optional new component values.</param>
/// <example>
///   <code>
///   // Update to yearly billing
///   var request = new UpdateUserSubscriptionRequest(
///     Frequency: SubscriptionFrequency.Yearly);
///
///   var updated = await client.Subscriptions.UpdateUserSubscriptionAsync(
///     subscriptionId, request);
///   </code>
/// </example>
public record UpdateUserSubscriptionRequest(
  [property: JsonPropertyName("frequency")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  SubscriptionFrequency? Frequency = null,

  [property: JsonPropertyName("rate_plan")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  RatePlanReference? RatePlan = null,

  [property: JsonPropertyName("component_values")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<SubscriptionComponentValue>? ComponentValues = null
);


/// <summary>
///   Result of deleting a user subscription.
/// </summary>
/// <param name="SubscriptionId">The deleted subscription identifier.</param>
public record DeleteUserSubscriptionResult(
  [property: JsonPropertyName("subscription_id")]
  string SubscriptionId
);


/// <summary>
///   Request to create a zone subscription (upgrade zone plan).
///   <para>
///     Use this to upgrade a zone from Free to a paid plan (Pro, Business, Enterprise).
///     Creating subscriptions may incur charges.
///   </para>
/// </summary>
/// <param name="RatePlan">Reference to the rate plan for this subscription.</param>
/// <param name="Frequency">Optional billing frequency (defaults to plan default).</param>
/// <param name="ComponentValues">Optional component values for configurable features.</param>
/// <example>
///   <code>
///   // Upgrade to Pro plan
///   var request = new CreateZoneSubscriptionRequest(
///     RatePlan: new RatePlanReference("pro"),
///     Frequency: SubscriptionFrequency.Monthly);
///
///   var subscription = await client.Subscriptions.CreateZoneSubscriptionAsync(zoneId, request);
///   </code>
/// </example>
public record CreateZoneSubscriptionRequest(
  [property: JsonPropertyName("rate_plan")]
  RatePlanReference RatePlan,

  [property: JsonPropertyName("frequency")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  SubscriptionFrequency? Frequency = null,

  [property: JsonPropertyName("component_values")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<SubscriptionComponentValue>? ComponentValues = null
);


/// <summary>
///   Request to update a zone subscription.
///   <para>
///     Updates may affect billing. Only specified fields will be changed;
///     null fields are left unchanged.
///   </para>
/// </summary>
/// <param name="RatePlan">Optional new rate plan reference.</param>
/// <param name="Frequency">Optional new billing frequency.</param>
/// <param name="ComponentValues">Optional new component values.</param>
/// <example>
///   <code>
///   // Downgrade to Pro from Business
///   var request = new UpdateZoneSubscriptionRequest(
///     RatePlan: new RatePlanReference("pro"));
///
///   var updated = await client.Subscriptions.UpdateZoneSubscriptionAsync(zoneId, request);
///   </code>
/// </example>
public record UpdateZoneSubscriptionRequest(
  [property: JsonPropertyName("rate_plan")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  RatePlanReference? RatePlan = null,

  [property: JsonPropertyName("frequency")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  SubscriptionFrequency? Frequency = null,

  [property: JsonPropertyName("component_values")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<SubscriptionComponentValue>? ComponentValues = null
);

#endregion


#region Zone Rate Plan Models

/// <summary>
///   Represents an available rate plan for a zone.
///   <para>
///     Rate plans define the pricing and features available for zone subscriptions.
///     Use <see cref="ISubscriptionsApi.ListAvailableRatePlansAsync"/> to discover available plans.
///   </para>
/// </summary>
/// <param name="Id">Rate plan identifier (e.g., "free", "pro", "business", "enterprise").</param>
/// <param name="Name">Human-readable plan name.</param>
/// <param name="Currency">Billing currency.</param>
/// <param name="Duration">Subscription duration in billing periods.</param>
/// <param name="Frequency">Billing frequency (e.g., monthly, yearly).</param>
/// <param name="Components">Plan components with pricing details.</param>
/// <example>
///   <code>
///   var plans = await client.Subscriptions.ListAvailableRatePlansAsync(zoneId);
///   foreach (var plan in plans)
///   {
///     Console.WriteLine($"{plan.Name}: {plan.Currency} {plan.Frequency}");
///     if (plan.Components != null)
///     {
///       foreach (var component in plan.Components)
///       {
///         Console.WriteLine($"  - {component.Name}: {component.UnitPrice}/unit");
///       }
///     }
///   }
///   </code>
/// </example>
public record ZoneRatePlan(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("currency")]
  string Currency,

  [property: JsonPropertyName("duration")]
  int Duration,

  [property: JsonPropertyName("frequency")]
  SubscriptionFrequency Frequency,

  [property: JsonPropertyName("components")]
  IReadOnlyList<RatePlanComponent>? Components = null
);


/// <summary>
///   Component pricing details for a rate plan.
///   <para>
///     Components define additional features or resources that can be added
///     to a subscription, along with their pricing information.
///   </para>
/// </summary>
/// <param name="Name">Component name (e.g., "page_rules", "dedicated_certificates").</param>
/// <param name="Default">Default quantity included in the plan.</param>
/// <param name="UnitPrice">Price per additional unit beyond the default.</param>
public record RatePlanComponent(
  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("default")]
  int Default,

  [property: JsonPropertyName("unit_price")]
  decimal UnitPrice
);

#endregion
