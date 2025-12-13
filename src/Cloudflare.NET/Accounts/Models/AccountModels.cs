namespace Cloudflare.NET.Accounts.Models;

using System.Text.Json.Serialization;
using Security.Firewall.Models;

/// <summary>
///   Represents a Cloudflare account, the top-level container for zones and resources.
///   <para>
///     Accounts own zones, Workers, R2 buckets, and other Cloudflare resources.
///     Multiple users can be members of the same account with different permission levels.
///   </para>
/// </summary>
/// <param name="Id">Account identifier tag (32 hexadecimal characters).</param>
/// <param name="Name">The name of the account, typically the company or organization name.</param>
/// <param name="Type">The type of account (e.g., standard, enterprise).</param>
/// <param name="CreatedOn">When the account was created.</param>
/// <param name="ManagedBy">
///   Parent organization information for managed accounts.
///   Null for standalone accounts that are not part of an organizational hierarchy.
/// </param>
/// <param name="Settings">
///   Account settings including security and administrative configurations.
///   May be null if settings have not been configured or are not returned by the API.
/// </param>
/// <remarks>
///   <para>
///     Accounts can be managed within organizational hierarchies for enterprise customers.
///     The <see cref="ManagedBy" /> property contains parent organization information for managed accounts.
///   </para>
/// </remarks>
/// <seealso href="https://developers.cloudflare.com/api/resources/accounts/" />
public record Account(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("type")]
  AccountType Type,
  [property: JsonPropertyName("created_on")]
  DateTime CreatedOn,
  [property: JsonPropertyName("managed_by")]
  AccountManagedBy? ManagedBy = null,
  [property: JsonPropertyName("settings")]
  AccountSettings? Settings = null
);


/// <summary>
///   Parent organization information for accounts managed within an organizational hierarchy.
///   <para>
///     This is used by enterprise customers to organize multiple accounts under a parent organization.
///   </para>
/// </summary>
/// <param name="ParentOrgId">
///   The identifier of the parent organization.
///   May be null if the account is not part of an organization.
/// </param>
/// <param name="ParentOrgName">
///   The name of the parent organization.
///   May be null if the account is not part of an organization.
/// </param>
public record AccountManagedBy(
  [property: JsonPropertyName("parent_org_id")]
  string? ParentOrgId,
  [property: JsonPropertyName("parent_org_name")]
  string? ParentOrgName
);


/// <summary>
///   Account settings including security and administrative configurations.
///   <para>
///     These settings apply to all members and resources within the account.
///   </para>
/// </summary>
/// <param name="AbuseContactEmail">
///   Email address for abuse-related communications.
///   Cloudflare uses this email to report issues such as phishing or malware
///   hosted on domains in the account.
/// </param>
/// <param name="EnforceTwofactor">
///   Whether two-factor authentication is enforced for all account members.
///   When enabled, all members must set up 2FA before they can access the account.
/// </param>
public record AccountSettings(
  [property: JsonPropertyName("abuse_contact_email")]
  string? AbuseContactEmail = null,
  [property: JsonPropertyName("enforce_twofactor")]
  bool EnforceTwofactor = false
);


/// <summary>
///   Request to create a new Cloudflare account.
///   <para>
///     <b>Note:</b> This operation is only available to tenant administrators.
///     Standard users cannot create accounts via the API.
///   </para>
/// </summary>
/// <param name="Name">The name for the new account (typically a company or project name).</param>
/// <remarks>
///   This endpoint is restricted to users with tenant admin privileges.
///   Most users should create accounts through the Cloudflare dashboard.
/// </remarks>
public record CreateAccountRequest(
  [property: JsonPropertyName("name")]
  string Name
);


/// <summary>
///   Request to update an existing Cloudflare account.
///   <para>
///     Both the name and settings can be updated in a single request.
///   </para>
/// </summary>
/// <param name="Name">The new name for the account.</param>
/// <param name="Settings">
///   Updated account settings.
///   If null, settings are not modified. To update settings, provide an <see cref="AccountSettings" /> object.
/// </param>
public record UpdateAccountRequest(
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("settings")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  AccountSettings? Settings = null
);


/// <summary>
///   Filtering and pagination options for listing accounts.
///   <para>
///     All properties are optional. When not specified, API defaults are used.
///   </para>
/// </summary>
/// <param name="Name">
///   Filter accounts by name (partial match).
///   Returns accounts whose name contains this value (case-insensitive).
/// </param>
/// <param name="Page">
///   Page number of paginated results.
///   Minimum value is 1. Default is 1 if not specified.
/// </param>
/// <param name="PerPage">
///   Number of accounts per page.
///   Valid range: 5 to 50. Default is 20 if not specified.
/// </param>
/// <param name="Direction">
///   Direction to order results.
///   When specified, results are sorted in the given direction (ascending or descending).
/// </param>
/// <remarks>
///   <para>Pagination defaults: page = 1, per_page = 20</para>
///   <para>Per page limits: minimum 5, maximum 50</para>
/// </remarks>
public record ListAccountsFilters(
  string? Name = null,
  int? Page = null,
  int? PerPage = null,
  ListOrderDirection? Direction = null
);


/// <summary>
///   Result of deleting an account.
///   <para>
///     Contains the identifier of the deleted account to confirm successful deletion.
///   </para>
/// </summary>
/// <param name="Id">The identifier of the deleted account.</param>
/// <remarks>
///   <b>Warning:</b> Deleting an account is a permanent operation that removes all zones,
///   Workers, R2 buckets, and other resources under the account.
/// </remarks>
public record DeleteAccountResult(
  [property: JsonPropertyName("id")]
  string Id
);
