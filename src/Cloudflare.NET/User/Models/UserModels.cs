namespace Cloudflare.NET.User.Models;

using System.Text.Json.Serialization;

/// <summary>
///   Represents the authenticated user's profile.
///   <para>
///     Contains all user information including email, name, contact details,
///     security settings, subscription tier flags, and organization memberships.
///   </para>
/// </summary>
/// <param name="Id">User identifier (32 hexadecimal characters).</param>
/// <param name="Email">User's email address.</param>
/// <param name="FirstName">User's first name.</param>
/// <param name="LastName">User's last name.</param>
/// <param name="Telephone">User's telephone number (international format, e.g., "+1-555-555-5555").</param>
/// <param name="Country">User's country (ISO 3166-1 alpha-2 code, e.g., "US", "GB", "DE").</param>
/// <param name="Zipcode">User's postal/zip code.</param>
/// <param name="Suspended">Whether the user account is suspended.</param>
/// <param name="TwoFactorAuthenticationEnabled">Whether two-factor authentication is enabled for this user.</param>
/// <param name="TwoFactorAuthenticationLocked">
///   Whether two-factor authentication is locked (cannot be disabled).
///   This is typically set by enterprise security policies.
/// </param>
/// <param name="HasProZones">Whether the user has any zones on the Pro plan.</param>
/// <param name="HasBusinessZones">Whether the user has any zones on the Business plan.</param>
/// <param name="HasEnterpriseZones">Whether the user has any zones on the Enterprise plan.</param>
/// <param name="Betas">Beta features the user has access to.</param>
/// <param name="Organizations">Organizations the user belongs to (deprecated, use Accounts instead).</param>
/// <param name="CreatedOn">When the user account was created.</param>
/// <param name="ModifiedOn">When the user profile was last modified.</param>
/// <remarks>
///   <para>
///     Only a subset of fields can be edited via the API: FirstName, LastName, Country, Telephone, and Zipcode.
///     Email, suspension status, and 2FA settings are managed through separate flows.
///   </para>
/// </remarks>
/// <seealso href="https://developers.cloudflare.com/api/resources/user/methods/get/" />
public record User(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("email")]
  string Email,
  [property: JsonPropertyName("first_name")]
  string? FirstName,
  [property: JsonPropertyName("last_name")]
  string? LastName,
  [property: JsonPropertyName("telephone")]
  string? Telephone,
  [property: JsonPropertyName("country")]
  string? Country,
  [property: JsonPropertyName("zipcode")]
  string? Zipcode,
  [property: JsonPropertyName("suspended")]
  bool Suspended,
  [property: JsonPropertyName("two_factor_authentication_enabled")]
  bool TwoFactorAuthenticationEnabled,
  [property: JsonPropertyName("two_factor_authentication_locked")]
  bool TwoFactorAuthenticationLocked = false,
  [property: JsonPropertyName("has_pro_zones")]
  bool HasProZones = false,
  [property: JsonPropertyName("has_business_zones")]
  bool HasBusinessZones = false,
  [property: JsonPropertyName("has_enterprise_zones")]
  bool HasEnterpriseZones = false,
  [property: JsonPropertyName("betas")]
  IReadOnlyList<string>? Betas = null,
  [property: JsonPropertyName("organizations")]
  IReadOnlyList<UserOrganization>? Organizations = null,
  [property: JsonPropertyName("created_on")]
  DateTime? CreatedOn = null,
  [property: JsonPropertyName("modified_on")]
  DateTime? ModifiedOn = null
);


/// <summary>
///   Organization membership information nested within a User.
///   <para>
///     <b>Note:</b> The Organization APIs are deprecated in favor of Accounts.
///     This type is retained for backward compatibility with existing user profiles.
///   </para>
/// </summary>
/// <param name="Id">Organization identifier.</param>
/// <param name="Name">Organization name.</param>
/// <param name="Status">User's status in the organization (e.g., "active", "pending").</param>
/// <param name="Permissions">User's permissions in the organization.</param>
/// <param name="Roles">User's roles in the organization.</param>
/// <remarks>
///   Organizations are a legacy concept in Cloudflare. New implementations should use Accounts
///   and account memberships instead.
/// </remarks>
public record UserOrganization(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("status")]
  string? Status = null,
  [property: JsonPropertyName("permissions")]
  IReadOnlyList<string>? Permissions = null,
  [property: JsonPropertyName("roles")]
  IReadOnlyList<string>? Roles = null
);


/// <summary>
///   Request to edit the authenticated user's profile.
///   <para>
///     Only the fields that are set (non-null) will be updated.
///     All fields are optional for partial updates.
///   </para>
/// </summary>
/// <param name="FirstName">Updated first name.</param>
/// <param name="LastName">Updated last name.</param>
/// <param name="Country">Updated country (ISO 3166-1 alpha-2 code, e.g., "US", "GB", "DE").</param>
/// <param name="Telephone">Updated telephone number (international format, e.g., "+1-555-555-5555").</param>
/// <param name="Zipcode">Updated postal/zip code.</param>
/// <remarks>
///   <para>
///     <b>Note:</b> Email address, 2FA settings, and suspension status cannot be changed via this API.
///     Those are managed through separate Cloudflare flows.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Update only first name
///   var request = new EditUserRequest(FirstName: "John");
///
///   // Update multiple fields
///   var request = new EditUserRequest(
///     FirstName: "John",
///     LastName: "Doe",
///     Country: "US");
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/api/resources/user/methods/edit/" />
public record EditUserRequest(
  [property: JsonPropertyName("first_name")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? FirstName = null,
  [property: JsonPropertyName("last_name")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? LastName = null,
  [property: JsonPropertyName("country")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Country = null,
  [property: JsonPropertyName("telephone")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Telephone = null,
  [property: JsonPropertyName("zipcode")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Zipcode = null
);
