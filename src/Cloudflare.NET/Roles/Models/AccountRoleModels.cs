namespace Cloudflare.NET.Roles.Models;

using System.Text.Json.Serialization;


#region AccountRole Model

/// <summary>
///   Represents a role that can be assigned to account members.
///   <para>
///     Roles are predefined by Cloudflare and define sets of permissions
///     for various features. Use <see cref="IRolesApi.ListAccountRolesAsync"/>
///     to discover available roles.
///   </para>
/// </summary>
/// <param name="Id">Role identifier (used when assigning roles to members).</param>
/// <param name="Name">Human-readable role name.</param>
/// <param name="Description">Description of the role's purpose.</param>
/// <param name="Permissions">Feature permissions granted by this role.</param>
/// <example>
///   <code>
///   // Get available roles and check permissions
///   var roles = await client.Roles.ListAccountRolesAsync(accountId);
///
///   foreach (var role in roles.Items)
///   {
///     Console.WriteLine($"{role.Name}: {role.Description}");
///     if (role.Permissions.Dns?.Write == true)
///       Console.WriteLine("  - Can modify DNS");
///   }
///   </code>
/// </example>
public record AccountRole(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("description")]
  string Description,

  [property: JsonPropertyName("permissions")]
  RolePermissions Permissions
);

#endregion


#region RolePermissions Model

/// <summary>
///   Feature permissions for a role.
///   <para>
///     Each property represents a Cloudflare feature with read and write access flags.
///     Null properties indicate the role does not grant any permissions for that feature.
///   </para>
/// </summary>
/// <param name="Analytics">Analytics access permissions.</param>
/// <param name="Billing">Billing access permissions.</param>
/// <param name="CachePurge">Cache purge permissions.</param>
/// <param name="Dns">DNS settings permissions.</param>
/// <param name="DnsRecords">DNS records permissions.</param>
/// <param name="LoadBalancer">Load balancer permissions.</param>
/// <param name="Logs">Logs access permissions.</param>
/// <param name="Organization">Organization management permissions.</param>
/// <param name="Ssl">SSL/TLS permissions.</param>
/// <param name="Waf">WAF permissions.</param>
/// <param name="ZoneSettings">Zone settings permissions.</param>
/// <param name="Zones">Zone management permissions.</param>
public record RolePermissions(
  [property: JsonPropertyName("analytics")]
  PermissionGrant? Analytics = null,

  [property: JsonPropertyName("billing")]
  PermissionGrant? Billing = null,

  [property: JsonPropertyName("cache_purge")]
  PermissionGrant? CachePurge = null,

  [property: JsonPropertyName("dns")]
  PermissionGrant? Dns = null,

  [property: JsonPropertyName("dns_records")]
  PermissionGrant? DnsRecords = null,

  [property: JsonPropertyName("lb")]
  PermissionGrant? LoadBalancer = null,

  [property: JsonPropertyName("logs")]
  PermissionGrant? Logs = null,

  [property: JsonPropertyName("organization")]
  PermissionGrant? Organization = null,

  [property: JsonPropertyName("ssl")]
  PermissionGrant? Ssl = null,

  [property: JsonPropertyName("waf")]
  PermissionGrant? Waf = null,

  [property: JsonPropertyName("zone_settings")]
  PermissionGrant? ZoneSettings = null,

  [property: JsonPropertyName("zones")]
  PermissionGrant? Zones = null
);

#endregion


#region PermissionGrant Model

/// <summary>
///   Read/write permission pair for a feature.
///   <para>
///     Represents the access level granted for a specific feature area.
///     Both read and write can be independently enabled or disabled.
///   </para>
/// </summary>
/// <param name="Read">Whether read access is granted.</param>
/// <param name="Write">Whether write access is granted.</param>
/// <example>
///   <code>
///   // Check if role can modify DNS
///   if (role.Permissions.DnsRecords is { Write: true })
///   {
///     Console.WriteLine("Role can modify DNS records");
///   }
///   </code>
/// </example>
public record PermissionGrant(
  [property: JsonPropertyName("read")]
  bool Read = false,

  [property: JsonPropertyName("write")]
  bool Write = false
);

#endregion


#region Filter Models

/// <summary>
///   Pagination options for listing account roles.
/// </summary>
/// <param name="Page">Page number (minimum 1, default 1).</param>
/// <param name="PerPage">Results per page (5-50, default 20).</param>
public record ListAccountRolesFilters(
  int? Page = null,
  int? PerPage = null
);

#endregion
