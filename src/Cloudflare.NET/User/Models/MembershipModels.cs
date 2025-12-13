namespace Cloudflare.NET.User.Models;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Accounts.Models;
using Core.Json;
using Members.Models;
using Roles.Models;
using Security.Firewall.Models;


#region Main Membership Model

/// <summary>
///   Represents a user's membership in a Cloudflare account.
///   <para>
///     Memberships show which accounts the user has access to and their
///     permissions within each account.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     <b>User-Scoped:</b> This is the user's view of their account relationships.
///     Each membership grants access to a specific account with defined permissions.
///   </para>
///   <para>
///     <b>Invitation Workflow:</b> Pending memberships represent invitations that
///     can be accepted or rejected via <see cref="IUserApi.UpdateMembershipAsync"/>.
///   </para>
/// </remarks>
/// <param name="Id">Membership identifier.</param>
/// <param name="Status">Current membership status (pending, accepted, rejected).</param>
/// <param name="Account">The account this membership grants access to.</param>
/// <param name="ApiAccessEnabled">Whether API access is enabled for this membership.</param>
/// <param name="Permissions">Permissions the user has in this account.</param>
/// <param name="Roles">Role IDs assigned to this membership.</param>
/// <param name="Policies">Fine-grained policies for this membership.</param>
public record Membership(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("status")]
  MemberStatus Status,

  [property: JsonPropertyName("account")]
  Account Account,

  [property: JsonPropertyName("api_access_enabled")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? ApiAccessEnabled = null,

  [property: JsonPropertyName("permissions")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  MembershipPermissions? Permissions = null,

  [property: JsonPropertyName("roles")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<string>? Roles = null,

  [property: JsonPropertyName("policies")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<MembershipPolicy>? Policies = null
);

#endregion


#region Permissions Model

/// <summary>
///   Permissions granted to a user through their membership.
///   <para>
///     Each permission category uses <see cref="PermissionGrant"/> to indicate
///     read and write access levels.
///   </para>
/// </summary>
/// <param name="Analytics">Analytics access permissions.</param>
/// <param name="Billing">Billing access permissions.</param>
/// <param name="CachePurge">Cache purge access permissions.</param>
/// <param name="Dns">DNS access permissions.</param>
/// <param name="LoadBalancer">Load balancer access permissions.</param>
/// <param name="Logs">Logs access permissions.</param>
/// <param name="Organization">Organization access permissions.</param>
/// <param name="Ssl">SSL/TLS access permissions.</param>
/// <param name="Waf">WAF access permissions.</param>
/// <param name="Zones">Zones access permissions.</param>
public record MembershipPermissions(
  [property: JsonPropertyName("analytics")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  PermissionGrant? Analytics = null,

  [property: JsonPropertyName("billing")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  PermissionGrant? Billing = null,

  [property: JsonPropertyName("cache_purge")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  PermissionGrant? CachePurge = null,

  [property: JsonPropertyName("dns")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  PermissionGrant? Dns = null,

  [property: JsonPropertyName("lb")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  PermissionGrant? LoadBalancer = null,

  [property: JsonPropertyName("logs")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  PermissionGrant? Logs = null,

  [property: JsonPropertyName("organization")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  PermissionGrant? Organization = null,

  [property: JsonPropertyName("ssl")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  PermissionGrant? Ssl = null,

  [property: JsonPropertyName("waf")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  PermissionGrant? Waf = null,

  [property: JsonPropertyName("zones")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  PermissionGrant? Zones = null
);

#endregion


#region Policy Model

/// <summary>
///   Fine-grained policy for a membership.
///   <para>
///     Policies provide granular access control beyond simple role-based permissions.
///   </para>
/// </summary>
/// <param name="Id">Policy identifier.</param>
/// <param name="Access">Access level for this policy.</param>
/// <param name="PermissionGroups">Permission groups included in this policy.</param>
/// <param name="ResourceGroups">Resource groups this policy applies to.</param>
public record MembershipPolicy(
  [property: JsonPropertyName("id")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Id = null,

  [property: JsonPropertyName("access")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Access = null,

  [property: JsonPropertyName("permission_groups")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<MemberPermissionGroupReference>? PermissionGroups = null,

  [property: JsonPropertyName("resource_groups")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<MemberResourceGroupReference>? ResourceGroups = null
);

#endregion


#region Request Models

/// <summary>
///   Request to update a membership status (accept or reject invitation).
///   <para>
///     Use this to accept or reject a pending invitation.
///     Once accepted, the user gains access to the account.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     <b>Accepting:</b> Use <see cref="MemberStatus.Accepted"/> to accept the invitation.
///     This grants the user access to the account with the assigned permissions.
///   </para>
///   <para>
///     <b>Rejecting:</b> Use <see cref="MemberStatus.Rejected"/> to decline the invitation.
///     The user will not be added to the account.
///   </para>
/// </remarks>
/// <param name="Status">
///   New status for the membership.
///   Use <see cref="MemberStatus.Accepted"/> to accept an invitation
///   or <see cref="MemberStatus.Rejected"/> to decline.
/// </param>
/// <example>
///   <code>
///   // Accept an invitation
///   var accept = new UpdateMembershipRequest(MemberStatus.Accepted);
///   await client.User.UpdateMembershipAsync(membershipId, accept);
///
///   // Reject an invitation
///   var reject = new UpdateMembershipRequest(MemberStatus.Rejected);
///   await client.User.UpdateMembershipAsync(membershipId, reject);
///   </code>
/// </example>
public record UpdateMembershipRequest(
  [property: JsonPropertyName("status")]
  MemberStatus Status
);

#endregion


#region Filter Models

/// <summary>
///   Filtering and pagination options for listing memberships.
/// </summary>
/// <param name="Status">Filter by membership status.</param>
/// <param name="AccountName">Filter by account name (partial match).</param>
/// <param name="Order">Field to order results by.</param>
/// <param name="Direction">Sort direction.</param>
/// <param name="Page">Page number (minimum 1, default 1).</param>
/// <param name="PerPage">Results per page (5-50, default 20).</param>
public record ListMembershipsFilters(
  MemberStatus? Status = null,
  string? AccountName = null,
  MembershipOrderField? Order = null,
  ListOrderDirection? Direction = null,
  int? Page = null,
  int? PerPage = null
);


/// <summary>
///   Fields available for ordering membership results.
/// </summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum MembershipOrderField
{
  /// <summary>Order by membership ID.</summary>
  [EnumMember(Value = "id")]
  Id,

  /// <summary>Order by account name.</summary>
  [EnumMember(Value = "account.name")]
  AccountName,

  /// <summary>Order by membership status.</summary>
  [EnumMember(Value = "status")]
  Status
}

#endregion
