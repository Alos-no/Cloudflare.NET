namespace Cloudflare.NET.AuditLogs.Models;

using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Models;
using Security.Firewall.Models;


/// <summary>
///   Represents an audit log entry recording an action taken on account resources.
///   <para>
///     Audit logs are retained for 30 days. Use filtering parameters to narrow
///     results when querying large datasets.
///   </para>
/// </summary>
/// <param name="Id">Unique identifier for this audit log entry.</param>
/// <param name="Account">Account where the action occurred.</param>
/// <param name="Action">Details about the action performed.</param>
/// <param name="Actor">Information about who performed the action.</param>
/// <param name="Raw">Raw HTTP request details.</param>
/// <param name="Resource">The resource that was acted upon.</param>
/// <param name="Zone">Zone context if the action was zone-scoped.</param>
public record AuditLog(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("account")]
  AuditLogAccount Account,
  [property: JsonPropertyName("action")]
  AuditLogAction Action,
  [property: JsonPropertyName("actor")]
  AuditLogActor Actor,
  [property: JsonPropertyName("raw")]
  AuditLogRaw? Raw = null,
  [property: JsonPropertyName("resource")]
  AuditLogResource? Resource = null,
  [property: JsonPropertyName("zone")]
  AuditLogZone? Zone = null
);


/// <summary>Account context for an audit log entry.</summary>
/// <param name="Id">Account identifier.</param>
/// <param name="Name">Account name.</param>
public record AuditLogAccount(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("name")]
  string? Name = null
);


/// <summary>Action details for an audit log entry.</summary>
/// <param name="Description">Human-readable description of the action.</param>
/// <param name="Result">Result of the action (e.g., "success", "failure").</param>
/// <param name="Time">When the action occurred.</param>
/// <param name="Type">Type of action (e.g., "create", "update", "delete").</param>
public record AuditLogAction(
  [property: JsonPropertyName("description")]
  string? Description,
  [property: JsonPropertyName("result")]
  string Result,
  [property: JsonPropertyName("time")]
  DateTime Time,
  [property: JsonPropertyName("type")]
  string Type
);


/// <summary>Actor (who performed the action) for an audit log entry.</summary>
/// <param name="Id">Actor identifier (user or service ID).</param>
/// <param name="Context">Context of the action (e.g., "dashboard", "api").</param>
/// <param name="Email">Email address of the actor (for user actions).</param>
/// <param name="IpAddress">IP address from which the action was performed.</param>
/// <param name="TokenId">API token identifier used for the action.</param>
/// <param name="TokenName">Name of the API token used.</param>
/// <param name="Type">Type of actor (e.g., "user", "system", "service").</param>
public record AuditLogActor(
  [property: JsonPropertyName("id")]
  string? Id,
  [property: JsonPropertyName("context")]
  string? Context = null,
  [property: JsonPropertyName("email")]
  string? Email = null,
  [property: JsonPropertyName("ip_address")]
  string? IpAddress = null,
  [property: JsonPropertyName("token_id")]
  string? TokenId = null,
  [property: JsonPropertyName("token_name")]
  string? TokenName = null,
  [property: JsonPropertyName("type")]
  string? Type = null
);


/// <summary>Raw HTTP request details for an audit log entry.</summary>
/// <param name="CfRayId">Cloudflare Ray ID for the request.</param>
/// <param name="Method">HTTP method (GET, POST, PUT, DELETE, etc.).</param>
/// <param name="StatusCode">HTTP status code returned.</param>
/// <param name="Uri">Request URI path.</param>
/// <param name="UserAgent">User agent string of the client.</param>
public record AuditLogRaw(
  [property: JsonPropertyName("cf_ray_id")]
  string? CfRayId = null,
  [property: JsonPropertyName("method")]
  string? Method = null,
  [property: JsonPropertyName("status_code")]
  int? StatusCode = null,
  [property: JsonPropertyName("uri")]
  string? Uri = null,
  [property: JsonPropertyName("user_agent")]
  string? UserAgent = null
);


/// <summary>Resource details for an audit log entry.</summary>
/// <param name="Id">Resource identifier.</param>
/// <param name="Product">Cloudflare product the resource belongs to.</param>
/// <param name="Request">Request payload (may be redacted).</param>
/// <param name="Response">Response payload (may be redacted).</param>
/// <param name="Scope">Scope of the resource (e.g., "account", "zone").</param>
/// <param name="Type">Type of resource (e.g., "dns_record", "firewall_rule").</param>
public record AuditLogResource(
  [property: JsonPropertyName("id")]
  string? Id = null,
  [property: JsonPropertyName("product")]
  string? Product = null,
  [property: JsonPropertyName("request")]
  JsonElement? Request = null,
  [property: JsonPropertyName("response")]
  JsonElement? Response = null,
  [property: JsonPropertyName("scope")]
  string? Scope = null,
  [property: JsonPropertyName("type")]
  string? Type = null
);


/// <summary>Zone context for an audit log entry.</summary>
/// <param name="Id">Zone identifier.</param>
/// <param name="Name">Zone name (domain).</param>
public record AuditLogZone(
  [property: JsonPropertyName("id")]
  string? Id = null,
  [property: JsonPropertyName("name")]
  string? Name = null
);


/// <summary>
///   Filtering and pagination options for listing audit logs.
///   <para>
///     All filter arrays support both inclusion and exclusion. Use the main array
///     for inclusion, and the corresponding <c>Not</c> array for exclusion.
///   </para>
/// </summary>
/// <param name="Cursor">Pagination cursor from previous response.</param>
/// <param name="Limit">Maximum results per request (1-1000, default 100).</param>
/// <param name="Direction">Sort direction.</param>
/// <param name="Before">Return logs from before this date (RFC3339).</param>
/// <param name="Since">Return logs from after this date (RFC3339).</param>
/// <param name="Ids">Filter by specific audit log IDs.</param>
/// <param name="IdsNot">Exclude specific audit log IDs.</param>
/// <param name="ActorEmails">Filter by actor email addresses.</param>
/// <param name="ActorEmailsNot">Exclude specific actor email addresses.</param>
/// <param name="ActorIds">Filter by actor IDs.</param>
/// <param name="ActorIdsNot">Exclude specific actor IDs.</param>
/// <param name="ActorIpAddresses">Filter by actor IP addresses.</param>
/// <param name="ActorIpAddressesNot">Exclude specific actor IP addresses.</param>
/// <param name="ActorTokenIds">Filter by actor token IDs.</param>
/// <param name="ActorTokenIdsNot">Exclude specific actor token IDs.</param>
/// <param name="ActorTokenNames">Filter by actor token names.</param>
/// <param name="ActorTokenNamesNot">Exclude specific actor token names.</param>
/// <param name="ActorContexts">Filter by actor contexts (e.g., "dashboard", "api").</param>
/// <param name="ActorContextsNot">Exclude specific actor contexts.</param>
/// <param name="ActorTypes">Filter by actor types (e.g., "user", "system").</param>
/// <param name="ActorTypesNot">Exclude specific actor types.</param>
/// <param name="ActionTypes">Filter by action types (e.g., "create", "update", "delete").</param>
/// <param name="ActionTypesNot">Exclude specific action types.</param>
/// <param name="ActionResults">Filter by action results (e.g., "success", "failure").</param>
/// <param name="ActionResultsNot">Exclude specific action results.</param>
/// <param name="ResourceIds">Filter by resource IDs.</param>
/// <param name="ResourceIdsNot">Exclude specific resource IDs.</param>
/// <param name="ResourceProducts">Filter by resource products.</param>
/// <param name="ResourceProductsNot">Exclude specific resource products.</param>
/// <param name="ResourceTypes">Filter by resource types.</param>
/// <param name="ResourceTypesNot">Exclude specific resource types.</param>
/// <param name="ResourceScopes">Filter by resource scopes.</param>
/// <param name="ResourceScopesNot">Exclude specific resource scopes.</param>
/// <param name="ZoneIds">Filter by zone IDs.</param>
/// <param name="ZoneIdsNot">Exclude specific zone IDs.</param>
/// <param name="ZoneNames">Filter by zone names.</param>
/// <param name="ZoneNamesNot">Exclude specific zone names.</param>
/// <param name="RawCfRayIds">Filter by Cloudflare Ray IDs.</param>
/// <param name="RawCfRayIdsNot">Exclude specific Cloudflare Ray IDs.</param>
/// <param name="RawMethods">Filter by HTTP methods.</param>
/// <param name="RawMethodsNot">Exclude specific HTTP methods.</param>
/// <param name="RawStatusCodes">Filter by HTTP status codes.</param>
/// <param name="RawStatusCodesNot">Exclude specific HTTP status codes.</param>
/// <param name="RawUris">Filter by request URIs.</param>
/// <param name="RawUrisNot">Exclude specific request URIs.</param>
/// <param name="AccountNames">Filter by account names.</param>
/// <param name="AccountNamesNot">Exclude specific account names.</param>
/// <example>
///   <code>
///   // Get logs for specific action types, excluding failures
///   var filters = new ListAuditLogsFilters(
///     ActionTypes: new[] { "create", "update" },
///     ActionResultsNot: new[] { "failure" }
///   );
///   </code>
/// </example>
public record ListAuditLogsFilters(
  string?                  Cursor               = null,
  int?                     Limit                = null,
  ListOrderDirection?      Direction            = null,
  DateTime?                Before               = null,
  DateTime?                Since                = null,
  IReadOnlyList<string>?   Ids                  = null,
  IReadOnlyList<string>?   IdsNot               = null,
  IReadOnlyList<string>?   ActorEmails          = null,
  IReadOnlyList<string>?   ActorEmailsNot       = null,
  IReadOnlyList<string>?   ActorIds             = null,
  IReadOnlyList<string>?   ActorIdsNot          = null,
  IReadOnlyList<string>?   ActorIpAddresses     = null,
  IReadOnlyList<string>?   ActorIpAddressesNot  = null,
  IReadOnlyList<string>?   ActorTokenIds        = null,
  IReadOnlyList<string>?   ActorTokenIdsNot     = null,
  IReadOnlyList<string>?   ActorTokenNames      = null,
  IReadOnlyList<string>?   ActorTokenNamesNot   = null,
  IReadOnlyList<string>?   ActorContexts        = null,
  IReadOnlyList<string>?   ActorContextsNot     = null,
  IReadOnlyList<string>?   ActorTypes           = null,
  IReadOnlyList<string>?   ActorTypesNot        = null,
  IReadOnlyList<string>?   ActionTypes          = null,
  IReadOnlyList<string>?   ActionTypesNot       = null,
  IReadOnlyList<string>?   ActionResults        = null,
  IReadOnlyList<string>?   ActionResultsNot     = null,
  IReadOnlyList<string>?   ResourceIds          = null,
  IReadOnlyList<string>?   ResourceIdsNot       = null,
  IReadOnlyList<string>?   ResourceProducts     = null,
  IReadOnlyList<string>?   ResourceProductsNot  = null,
  IReadOnlyList<string>?   ResourceTypes        = null,
  IReadOnlyList<string>?   ResourceTypesNot     = null,
  IReadOnlyList<string>?   ResourceScopes       = null,
  IReadOnlyList<string>?   ResourceScopesNot    = null,
  IReadOnlyList<string>?   ZoneIds              = null,
  IReadOnlyList<string>?   ZoneIdsNot           = null,
  IReadOnlyList<string>?   ZoneNames            = null,
  IReadOnlyList<string>?   ZoneNamesNot         = null,
  IReadOnlyList<string>?   RawCfRayIds          = null,
  IReadOnlyList<string>?   RawCfRayIdsNot       = null,
  IReadOnlyList<string>?   RawMethods           = null,
  IReadOnlyList<string>?   RawMethodsNot        = null,
  IReadOnlyList<int>?      RawStatusCodes       = null,
  IReadOnlyList<int>?      RawStatusCodesNot    = null,
  IReadOnlyList<string>?   RawUris              = null,
  IReadOnlyList<string>?   RawUrisNot           = null,
  IReadOnlyList<string>?   AccountNames         = null,
  IReadOnlyList<string>?   AccountNamesNot      = null
);
