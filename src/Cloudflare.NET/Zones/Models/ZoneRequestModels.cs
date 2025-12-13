namespace Cloudflare.NET.Zones.Models;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Core.Json;
using Security.Firewall.Models;

/// <summary>
///   Request to create a new Zone in Cloudflare.
///   <para>
///     Creates a zone for a domain and associates it with an account.
///     The domain must not already be added to Cloudflare in the same or another account.
///   </para>
/// </summary>
/// <param name="Name">The domain name to add (e.g., "example.com").</param>
/// <param name="Type">
///   The type of zone setup:
///   <see cref="ZoneType.Full" /> for Cloudflare-hosted DNS,
///   <see cref="ZoneType.Partial" /> for CNAME setup,
///   <see cref="ZoneType.Secondary" /> for secondary DNS.
/// </param>
/// <param name="Account">The account to associate the zone with.</param>
/// <param name="JumpStart">
///   Whether to automatically fetch existing DNS records from the domain.
///   Defaults to false. This is a best-effort operation; failure does not prevent zone creation.
/// </param>
/// <example>
///   <code>
///   var request = new CreateZoneRequest(
///     Name: "example.com",
///     Type: ZoneType.Full,
///     Account: new ZoneAccountReference("account-id-here"),
///     JumpStart: true
///   );
///   var zone = await zonesApi.CreateZoneAsync(request);
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/create/" />
public record CreateZoneRequest(
  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("type")]
  ZoneType Type,

  [property: JsonPropertyName("account")]
  ZoneAccountReference Account,

  [property: JsonPropertyName("jump_start")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? JumpStart = null
);


/// <summary>
///   Account reference for zone creation.
///   <para>
///     Specifies which Cloudflare account the zone should be created under.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier of the account.</param>
public record ZoneAccountReference(
  [property: JsonPropertyName("id")]
  string Id
);


/// <summary>
///   Request to edit a Zone's properties.
///   <para>
///     <strong>Important:</strong> Only one property can be changed per API call.
///     Setting multiple properties in a single request may result in an error.
///   </para>
/// </summary>
/// <param name="Paused">Whether to pause the zone. When paused, Cloudflare stops proxying traffic.</param>
/// <param name="Type">The zone type to change to (requires Enterprise plan for some transitions).</param>
/// <param name="VanityNameServers">Custom nameservers for the zone (Business/Enterprise only).</param>
/// <param name="Plan">The plan to change to. Requires appropriate subscription.</param>
/// <example>
///   <code>
///   // Pause a zone
///   var request = new EditZoneRequest(Paused: true);
///   var zone = await zonesApi.EditZoneAsync(zoneId, request);
///
///   // Change zone type
///   var request = new EditZoneRequest(Type: ZoneType.Partial);
///   var zone = await zonesApi.EditZoneAsync(zoneId, request);
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/edit/" />
public record EditZoneRequest(
  [property: JsonPropertyName("paused")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? Paused = null,

  [property: JsonPropertyName("type")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  ZoneType? Type = null,

  [property: JsonPropertyName("vanity_name_servers")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<string>? VanityNameServers = null,

  [property: JsonPropertyName("plan")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  ZonePlanReference? Plan = null
);


/// <summary>
///   Plan reference for zone editing.
///   <para>
///     Used to specify a plan when editing zone properties.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier of the plan to change to.</param>
public record ZonePlanReference(
  [property: JsonPropertyName("id")]
  string Id
);


/// <summary>
///   Result of a zone activation check operation.
///   <para>
///     Returned by <c>TriggerActivationCheckAsync</c> when verifying zone activation.
///     The API only returns the zone ID; use <c>GetZoneDetailsAsync</c> to fetch the updated status.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier of the zone.</param>
/// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/activation-check/" />
public record ActivationCheckResult(
  [property: JsonPropertyName("id")]
  string Id
);


/// <summary>
///   Filters for listing Zones.
///   <para>
///     All filter properties are optional. When multiple filters are specified,
///     they are combined according to the <see cref="Match" /> parameter.
///   </para>
/// </summary>
/// <param name="Name">Filter by exact domain name match.</param>
/// <param name="Status">Filter by zone status (e.g., active, pending).</param>
/// <param name="AccountId">Filter by account ID. Use this for multi-account scenarios.</param>
/// <param name="AccountName">Filter by account name.</param>
/// <param name="Page">The page number for pagination (1-based).</param>
/// <param name="PerPage">The number of results per page (1-50, default 20).</param>
/// <param name="Order">The field to order results by.</param>
/// <param name="Direction">The sort direction (ascending or descending).</param>
/// <param name="Match">How to combine multiple filters: "all" (AND) or "any" (OR).</param>
/// <example>
///   <code>
///   // List active zones
///   var filters = new ListZonesFilters(Status: ZoneStatus.Active);
///
///   // List zones with pagination
///   var filters = new ListZonesFilters(Page: 1, PerPage: 50);
///
///   // List zones for a specific account, sorted by name
///   var filters = new ListZonesFilters(
///     AccountId: "account-id",
///     Order: ZoneOrderField.Name,
///     Direction: ListOrderDirection.Ascending
///   );
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/list/" />
public record ListZonesFilters(
  string? Name = null,
  ZoneStatus? Status = null,
  string? AccountId = null,
  string? AccountName = null,
  int? Page = null,
  int? PerPage = null,
  ZoneOrderField? Order = null,
  ListOrderDirection? Direction = null,
  FilterMatch? Match = null
);


/// <summary>
///   Fields by which zones can be ordered in list operations.
/// </summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum ZoneOrderField
{
  /// <summary>Order by the zone's domain name.</summary>
  [EnumMember(Value = "name")] Name,

  /// <summary>Order by the zone's status.</summary>
  [EnumMember(Value = "status")] Status,

  /// <summary>Order by the account ID.</summary>
  [EnumMember(Value = "account.id")] AccountId,

  /// <summary>Order by the account name.</summary>
  [EnumMember(Value = "account.name")] AccountName,

  /// <summary>Order by the plan ID.</summary>
  [EnumMember(Value = "plan.id")] PlanId
}
