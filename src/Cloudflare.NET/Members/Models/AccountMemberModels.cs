namespace Cloudflare.NET.Members.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using Core.Json;
using Roles.Models;
using Security.Firewall.Models;


#region MemberStatus Extensible Enum

/// <summary>
///   Represents the status of an account member.
///   <para>
///     Member status indicates the state of the membership:
///     <list type="bullet">
///       <item>
///         <term>Accepted</term>
///         <description>The user has accepted the membership invitation</description>
///       </item>
///       <item>
///         <term>Pending</term>
///         <description>The invitation is awaiting acceptance</description>
///       </item>
///       <item>
///         <term>Rejected</term>
///         <description>The user has rejected the invitation</description>
///       </item>
///     </list>
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This is an extensible enum that provides IntelliSense for known values while allowing
///     custom values for new statuses that may be added to the Cloudflare API in the future.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Using a known status with IntelliSense
///   var status = MemberStatus.Accepted;
///
///   // Checking member status
///   if (member.Status == MemberStatus.Pending) { ... }
///
///   // Using implicit conversion from string
///   MemberStatus customStatus = "expired";
///   </code>
/// </example>
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<MemberStatus>))]
public readonly struct MemberStatus : IExtensibleEnum<MemberStatus>, IEquatable<MemberStatus>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this member status.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this member status.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>
  ///   The user has accepted the membership invitation.
  ///   <para>
  ///     The member is active and can access account resources according to their roles.
  ///   </para>
  /// </summary>
  public static MemberStatus Accepted { get; } = new("accepted");

  /// <summary>
  ///   The invitation is awaiting acceptance.
  ///   <para>
  ///     The user has been invited but has not yet accepted or rejected the invitation.
  ///   </para>
  /// </summary>
  public static MemberStatus Pending { get; } = new("pending");

  /// <summary>
  ///   The user has rejected the invitation.
  ///   <para>
  ///     The membership was declined by the invited user.
  ///   </para>
  /// </summary>
  public static MemberStatus Rejected { get; } = new("rejected");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="MemberStatus"/> with the specified value.</summary>
  /// <param name="value">The string value representing the member status.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
  public MemberStatus(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static MemberStatus Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="MemberStatus"/>.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator MemberStatus(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="MemberStatus"/> to its string value.</summary>
  /// <param name="status">The member status to convert.</param>
  public static implicit operator string(MemberStatus status) => status.Value;

  /// <summary>Determines whether two <see cref="MemberStatus"/> values are equal.</summary>
  public static bool operator ==(MemberStatus left, MemberStatus right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="MemberStatus"/> values are not equal.</summary>
  public static bool operator !=(MemberStatus left, MemberStatus right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(MemberStatus other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is MemberStatus other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}

#endregion


#region AccountMember Model

/// <summary>
///   Represents a member of a Cloudflare account.
///   <para>
///     Account members are users who have been granted access to an account's resources.
///     Each member has one or more roles that define their permissions.
///   </para>
/// </summary>
/// <param name="Id">Member identifier (membership ID, not user ID).</param>
/// <param name="Status">Current membership status.</param>
/// <param name="User">User information for the member.</param>
/// <param name="Roles">Roles assigned to this member.</param>
/// <param name="Policies">Optional policies for fine-grained access control.</param>
/// <example>
///   <code>
///   var members = await client.Members.ListAccountMembersAsync(accountId);
///   foreach (var member in members.Items)
///   {
///     Console.WriteLine($"{member.User.Email} - {member.Status}");
///     foreach (var role in member.Roles)
///       Console.WriteLine($"  Role: {role.Name}");
///   }
///   </code>
/// </example>
public record AccountMember(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("status")]
  MemberStatus Status,

  [property: JsonPropertyName("user")]
  MemberUser User,

  [property: JsonPropertyName("roles")]
  IReadOnlyList<AccountRole> Roles,

  [property: JsonPropertyName("policies")]
  IReadOnlyList<MemberPolicy>? Policies = null
);

#endregion


#region MemberUser Model

/// <summary>
///   User information associated with an account member.
/// </summary>
/// <param name="Id">User identifier.</param>
/// <param name="Email">User's email address.</param>
/// <param name="FirstName">User's first name (may be null for pending invitations).</param>
/// <param name="LastName">User's last name (may be null for pending invitations).</param>
/// <param name="TwoFactorAuthenticationEnabled">Whether 2FA is enabled for the user.</param>
public record MemberUser(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("email")]
  string Email,

  [property: JsonPropertyName("first_name")]
  string? FirstName = null,

  [property: JsonPropertyName("last_name")]
  string? LastName = null,

  [property: JsonPropertyName("two_factor_authentication_enabled")]
  bool TwoFactorAuthenticationEnabled = false
);

#endregion


#region MemberPolicy Model

/// <summary>
///   Access policy for fine-grained member permissions.
///   <para>
///     Policies provide more granular control than roles by specifying
///     permission groups and the resources they apply to.
///   </para>
/// </summary>
/// <param name="Id">Policy identifier.</param>
/// <param name="Access">Access level: "allow" or "deny".</param>
/// <param name="PermissionGroups">Permission groups included in this policy.</param>
/// <param name="ResourceGroups">Resource groups this policy applies to.</param>
public record MemberPolicy(
  [property: JsonPropertyName("id")]
  string? Id,

  [property: JsonPropertyName("access")]
  string Access,

  [property: JsonPropertyName("permission_groups")]
  IReadOnlyList<MemberPermissionGroupReference> PermissionGroups,

  [property: JsonPropertyName("resource_groups")]
  IReadOnlyList<MemberResourceGroupReference> ResourceGroups
);


/// <summary>Reference to a permission group in a member policy.</summary>
/// <param name="Id">Permission group identifier.</param>
public record MemberPermissionGroupReference(
  [property: JsonPropertyName("id")]
  string Id
);


/// <summary>Reference to a resource group in a member policy.</summary>
/// <param name="Id">Resource group identifier.</param>
public record MemberResourceGroupReference(
  [property: JsonPropertyName("id")]
  string Id
);

#endregion


#region Request Models

/// <summary>
///   Request to invite a new member to an account.
///   <para>
///     An invitation email will be sent to the specified email address.
///     The user must accept the invitation to become an active member.
///   </para>
/// </summary>
/// <param name="Email">Email address of the user to invite.</param>
/// <param name="Roles">Role IDs to assign to the member upon acceptance.</param>
/// <param name="Policies">Optional policies for fine-grained access control.</param>
/// <param name="Status">Optional status to set (typically omitted for new invitations).</param>
/// <example>
///   <code>
///   var request = new CreateAccountMemberRequest(
///     Email: "newuser@example.com",
///     Roles: new[] { "dns-admin-role-id", "viewer-role-id" }
///   );
///   var member = await client.Members.CreateAccountMemberAsync(accountId, request);
///   </code>
/// </example>
public record CreateAccountMemberRequest(
  [property: JsonPropertyName("email")]
  string Email,

  [property: JsonPropertyName("roles")]
  IReadOnlyList<string> Roles,

  [property: JsonPropertyName("policies")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<CreateMemberPolicyRequest>? Policies = null,

  [property: JsonPropertyName("status")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  MemberStatus? Status = null
);


/// <summary>
///   Request to update an existing account member.
///   <para>
///     Only the roles and policies can be updated. The user's email
///     and status cannot be changed through this endpoint.
///   </para>
/// </summary>
/// <param name="Roles">Updated role IDs for the member.</param>
/// <param name="Policies">Updated policies for fine-grained access control.</param>
public record UpdateAccountMemberRequest(
  [property: JsonPropertyName("roles")]
  IReadOnlyList<string> Roles,

  [property: JsonPropertyName("policies")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<CreateMemberPolicyRequest>? Policies = null
);


/// <summary>
///   Policy definition for creating or updating a member.
/// </summary>
/// <param name="Access">Access level: "allow" or "deny".</param>
/// <param name="PermissionGroups">Permission groups to include.</param>
/// <param name="ResourceGroups">Resource groups this policy applies to.</param>
public record CreateMemberPolicyRequest(
  [property: JsonPropertyName("access")]
  string Access,

  [property: JsonPropertyName("permission_groups")]
  IReadOnlyList<MemberPermissionGroupReference> PermissionGroups,

  [property: JsonPropertyName("resource_groups")]
  IReadOnlyList<MemberResourceGroupReference> ResourceGroups
);

#endregion


#region Filter Models

/// <summary>
///   Filtering and pagination options for listing account members.
/// </summary>
/// <param name="Status">Filter by membership status.</param>
/// <param name="Page">Page number (minimum 1, default 1).</param>
/// <param name="PerPage">Results per page (5-50, default 20).</param>
/// <param name="Direction">Sort direction.</param>
/// <param name="Order">Field to sort by.</param>
public record ListAccountMembersFilters(
  MemberStatus? Status = null,
  int? Page = null,
  int? PerPage = null,
  ListOrderDirection? Direction = null,
  MemberOrderField? Order = null
);


/// <summary>
///   Fields by which account members can be ordered.
/// </summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum MemberOrderField
{
  /// <summary>Order by user ID.</summary>
  [System.Runtime.Serialization.EnumMember(Value = "user.id")]
  UserId,

  /// <summary>Order by user email.</summary>
  [System.Runtime.Serialization.EnumMember(Value = "user.email")]
  UserEmail,

  /// <summary>Order by user's first name.</summary>
  [System.Runtime.Serialization.EnumMember(Value = "user.first_name")]
  UserFirstName,

  /// <summary>Order by user's last name.</summary>
  [System.Runtime.Serialization.EnumMember(Value = "user.last_name")]
  UserLastName,

  /// <summary>Order by membership status.</summary>
  [System.Runtime.Serialization.EnumMember(Value = "status")]
  Status
}

#endregion


#region Result Models

/// <summary>
///   Result of deleting an account member.
/// </summary>
/// <param name="Id">The ID of the deleted membership.</param>
public record DeleteAccountMemberResult(
  [property: JsonPropertyName("id")]
  string Id
);

#endregion
