namespace Cloudflare.NET.Zones.Models;

using System.Text.Json.Serialization;

/// <summary>
///   Represents the detailed information for a Cloudflare Zone.
///   <para>
///     A zone represents a domain and its configuration in Cloudflare. This record captures
///     all properties returned by the Cloudflare API for a zone resource.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier for the zone (32-character hexadecimal string).</param>
/// <param name="Name">The domain name of the zone (e.g., "example.com").</param>
/// <param name="Status">The current status of the zone (e.g., active, pending, initializing).</param>
/// <param name="Account">The account that owns this zone.</param>
/// <param name="ActivatedOn">The timestamp when the zone was activated. Null if not yet activated.</param>
/// <param name="CreatedOn">The timestamp when the zone was created.</param>
/// <param name="ModifiedOn">The timestamp when the zone was last modified.</param>
/// <param name="DevelopmentMode">
///   Seconds remaining in development mode. 0 means development mode is off.
///   When enabled, Cloudflare caching is bypassed.
/// </param>
/// <param name="NameServers">The Cloudflare-assigned nameservers for the zone.</param>
/// <param name="OriginalNameServers">The nameservers that were on the domain before moving to Cloudflare.</param>
/// <param name="OriginalRegistrar">The registrar that manages the domain.</param>
/// <param name="OriginalDnsHost">The DNS host that was hosting the domain before moving to Cloudflare.</param>
/// <param name="Owner">Information about the user who owns the zone.</param>
/// <param name="Plan">The billing plan for the zone.</param>
/// <param name="Meta">Additional metadata about the zone.</param>
/// <param name="Paused">Whether the zone is paused. When paused, Cloudflare stops proxying traffic.</param>
/// <param name="Permissions">The permissions the current API token has on this zone.</param>
/// <param name="Type">The type of zone setup (full, partial, or secondary).</param>
/// <param name="VanityNameServers">Custom nameservers configured for the zone (Business/Enterprise only).</param>
/// <param name="CnameSuffix">The CNAME suffix for partial zones.</param>
/// <param name="VerificationKey">The verification key for the zone.</param>
/// <param name="Tenant">Tenant information for multi-tenant configurations.</param>
/// <param name="TenantUnit">Tenant unit information for multi-tenant configurations.</param>
/// <seealso href="https://developers.cloudflare.com/api/resources/zones/methods/list/" />
public record Zone(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("status")]
  ZoneStatus Status,

  [property: JsonPropertyName("account")]
  ZoneAccount Account,

  [property: JsonPropertyName("activated_on")]
  DateTime? ActivatedOn,

  [property: JsonPropertyName("created_on")]
  DateTime CreatedOn,

  [property: JsonPropertyName("modified_on")]
  DateTime ModifiedOn,

  [property: JsonPropertyName("development_mode")]
  int DevelopmentMode,

  [property: JsonPropertyName("name_servers")]
  IReadOnlyList<string> NameServers,

  [property: JsonPropertyName("original_name_servers")]
  IReadOnlyList<string>? OriginalNameServers,

  [property: JsonPropertyName("original_registrar")]
  string? OriginalRegistrar,

  [property: JsonPropertyName("original_dnshost")]
  string? OriginalDnsHost,

  [property: JsonPropertyName("owner")]
  ZoneOwner Owner,

  [property: JsonPropertyName("plan")]
  ZonePlan Plan,

  [property: JsonPropertyName("meta")]
  ZoneMeta? Meta,

  [property: JsonPropertyName("paused")]
  bool Paused,

  [property: JsonPropertyName("permissions")]
  IReadOnlyList<string>? Permissions,

  [property: JsonPropertyName("type")]
  ZoneType Type,

  [property: JsonPropertyName("vanity_name_servers")]
  IReadOnlyList<string>? VanityNameServers,

  [property: JsonPropertyName("cname_suffix")]
  string? CnameSuffix,

  [property: JsonPropertyName("verification_key")]
  string? VerificationKey,

  [property: JsonPropertyName("tenant")]
  ZoneTenant? Tenant,

  [property: JsonPropertyName("tenant_unit")]
  ZoneTenantUnit? TenantUnit
);


/// <summary>
///   Account information nested within a Zone.
///   <para>
///     This represents the Cloudflare account that owns the zone.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier of the account.</param>
/// <param name="Name">The name of the account.</param>
public record ZoneAccount(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("name")]
  string Name
);


/// <summary>
///   Owner information for a Zone.
///   <para>
///     This represents the user or organization that owns the zone.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier of the owner. May be null for certain zone types.</param>
/// <param name="Name">The name of the owner. May be null.</param>
/// <param name="Type">The type of owner (e.g., "user", "organization"). May be null.</param>
public record ZoneOwner(
  [property: JsonPropertyName("id")]
  string? Id,

  [property: JsonPropertyName("name")]
  string? Name,

  [property: JsonPropertyName("type")]
  string? Type
);


/// <summary>
///   Plan information for a Zone.
///   <para>
///     This represents the Cloudflare billing plan associated with the zone.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier of the plan.</param>
/// <param name="Name">The name of the plan (e.g., "Free", "Pro", "Business", "Enterprise").</param>
/// <param name="Price">The monthly price of the plan in the specified currency.</param>
/// <param name="Currency">The currency for the plan price (e.g., "USD").</param>
/// <param name="Frequency">The billing frequency (e.g., "monthly", "yearly").</param>
/// <param name="IsSubscribed">Whether the zone is currently subscribed to this plan.</param>
/// <param name="CanSubscribe">Whether the zone can subscribe to this plan.</param>
/// <param name="LegacyId">The legacy identifier for the plan.</param>
/// <param name="LegacyDiscount">Whether a legacy discount applies.</param>
/// <param name="ExternallyManaged">Whether the plan is managed externally (e.g., through a partner).</param>
public record ZonePlan(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("price")]
  decimal Price,

  [property: JsonPropertyName("currency")]
  string Currency,

  [property: JsonPropertyName("frequency")]
  string? Frequency,

  [property: JsonPropertyName("is_subscribed")]
  bool IsSubscribed,

  [property: JsonPropertyName("can_subscribe")]
  bool CanSubscribe,

  [property: JsonPropertyName("legacy_id")]
  string? LegacyId,

  [property: JsonPropertyName("legacy_discount")]
  bool LegacyDiscount,

  [property: JsonPropertyName("externally_managed")]
  bool ExternallyManaged
);


/// <summary>
///   Metadata for a Zone.
///   <para>
///     This contains additional configuration and quota information for the zone.
///   </para>
/// </summary>
/// <param name="Step">The current setup step for the zone.</param>
/// <param name="CustomCertificateQuota">The number of custom certificates allowed for the zone.</param>
/// <param name="PageRuleQuota">The number of page rules allowed for the zone.</param>
/// <param name="PhishingDetected">Whether phishing has been detected on the zone.</param>
/// <param name="CdnOnly">Whether the zone is CDN-only (no security features).</param>
/// <param name="DnsOnly">Whether the zone is DNS-only (no proxy).</param>
/// <param name="FoundationDns">Whether the zone uses Foundation DNS.</param>
public record ZoneMeta(
  [property: JsonPropertyName("step")]
  int? Step,

  [property: JsonPropertyName("custom_certificate_quota")]
  int? CustomCertificateQuota,

  [property: JsonPropertyName("page_rule_quota")]
  int? PageRuleQuota,

  [property: JsonPropertyName("phishing_detected")]
  bool? PhishingDetected,

  [property: JsonPropertyName("cdn_only")]
  bool? CdnOnly,

  [property: JsonPropertyName("dns_only")]
  bool? DnsOnly,

  [property: JsonPropertyName("foundation_dns")]
  bool? FoundationDns
);


/// <summary>
///   Tenant information for a Zone.
///   <para>
///     This represents the tenant in a multi-tenant Cloudflare configuration.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier of the tenant.</param>
/// <param name="Name">The name of the tenant.</param>
public record ZoneTenant(
  [property: JsonPropertyName("id")]
  string? Id,

  [property: JsonPropertyName("name")]
  string? Name
);


/// <summary>
///   Tenant unit information for a Zone.
///   <para>
///     This represents the tenant unit in a multi-tenant Cloudflare configuration.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier of the tenant unit.</param>
public record ZoneTenantUnit(
  [property: JsonPropertyName("id")]
  string? Id
);
