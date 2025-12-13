namespace Cloudflare.NET.ApiTokens.Models;

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Json;
using Security.Firewall.Models;


#region TokenStatus Extensible Enum

/// <summary>
///   Represents the status of an API token.
///   <para>
///     Token status indicates whether the token is currently usable for API operations.
///     <list type="bullet">
///       <item>
///         <term>Active</term>
///         <description>Token is active and can be used for API requests</description>
///       </item>
///       <item>
///         <term>Disabled</term>
///         <description>Token has been manually disabled</description>
///       </item>
///       <item>
///         <term>Expired</term>
///         <description>Token has passed its expiration date</description>
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
///   var status = TokenStatus.Active;
///
///   // Checking token status
///   if (token.Status == TokenStatus.Disabled) { ... }
///
///   // Using implicit conversion from string
///   TokenStatus customStatus = "pending";
///   </code>
/// </example>
[DebuggerDisplay("{Value}")]
[JsonConverter(typeof(ExtensibleEnumConverter<TokenStatus>))]
public readonly struct TokenStatus : IExtensibleEnum<TokenStatus>, IEquatable<TokenStatus>
{
  #region Properties & Fields - Non-Public

  /// <summary>The underlying string value of this token status.</summary>
  private readonly string? _value;

  #endregion


  #region Properties & Fields - Public

  /// <summary>Gets the underlying string value of this token status.</summary>
  public string Value => _value ?? string.Empty;

  #endregion


  #region Known Values

  /// <summary>
  ///   Token is active and can be used for API requests.
  ///   <para>
  ///     This is the default status for newly created tokens. Active tokens
  ///     will successfully authenticate API requests.
  ///   </para>
  /// </summary>
  public static TokenStatus Active { get; } = new("active");

  /// <summary>
  ///   Token has been manually disabled.
  ///   <para>
  ///     Disabled tokens cannot be used for API requests until re-enabled.
  ///     Use the update endpoint to change the status back to active.
  ///   </para>
  /// </summary>
  public static TokenStatus Disabled { get; } = new("disabled");

  /// <summary>
  ///   Token has passed its expiration date.
  ///   <para>
  ///     Expired tokens cannot be used for API requests. Create a new token
  ///     or update the expiration date to restore access.
  ///   </para>
  /// </summary>
  public static TokenStatus Expired { get; } = new("expired");

  #endregion


  #region Constructors

  /// <summary>Creates a new <see cref="TokenStatus" /> with the specified value.</summary>
  /// <param name="value">The string value representing the token status.</param>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
  public TokenStatus(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    _value = value;
  }

  #endregion


  #region Static Factory

  /// <inheritdoc />
  public static TokenStatus Create(string value) => new(value);

  #endregion


  #region Operators

  /// <summary>Implicitly converts a string to a <see cref="TokenStatus" />.</summary>
  /// <param name="value">The string value to convert.</param>
  public static implicit operator TokenStatus(string value) => new(value);

  /// <summary>Implicitly converts a <see cref="TokenStatus" /> to its string value.</summary>
  /// <param name="status">The token status to convert.</param>
  public static implicit operator string(TokenStatus status) => status.Value;

  /// <summary>Determines whether two <see cref="TokenStatus" /> values are equal.</summary>
  public static bool operator ==(TokenStatus left, TokenStatus right) => left.Equals(right);

  /// <summary>Determines whether two <see cref="TokenStatus" /> values are not equal.</summary>
  public static bool operator !=(TokenStatus left, TokenStatus right) => !left.Equals(right);

  #endregion


  #region Equality

  /// <inheritdoc />
  public bool Equals(TokenStatus other) =>
    string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

  /// <inheritdoc />
  public override bool Equals(object? obj) => obj is TokenStatus other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

  #endregion


  #region Methods

  /// <inheritdoc />
  public override string ToString() => Value;

  #endregion
}

#endregion


#region ApiToken Model

/// <summary>
///   Represents an account-owned API token.
///   <para>
///     API tokens provide fine-grained access control for Cloudflare API operations.
///     Each token has policies that define what resources and actions are allowed.
///   </para>
/// </summary>
/// <param name="Id">Token identifier.</param>
/// <param name="Name">Human-readable token name.</param>
/// <param name="Status">Current token status.</param>
/// <param name="IssuedOn">When the token was created.</param>
/// <param name="ModifiedOn">When the token was last modified.</param>
/// <param name="Policies">Token policies defining permissions.</param>
/// <param name="ExpiresOn">When the token expires. Null if no expiration.</param>
/// <param name="NotBefore">Token is not valid before this time.</param>
/// <param name="LastUsedOn">When the token was last used.</param>
/// <param name="Condition">IP-based access restrictions.</param>
public record ApiToken(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("status")]
  TokenStatus Status,
  [property: JsonPropertyName("issued_on")]
  DateTime IssuedOn,
  [property: JsonPropertyName("modified_on")]
  DateTime ModifiedOn,
  [property: JsonPropertyName("policies")]
  IReadOnlyList<TokenPolicy> Policies,
  [property: JsonPropertyName("expires_on")]
  DateTime? ExpiresOn = null,
  [property: JsonPropertyName("not_before")]
  DateTime? NotBefore = null,
  [property: JsonPropertyName("last_used_on")]
  DateTime? LastUsedOn = null,
  [property: JsonPropertyName("condition")]
  TokenCondition? Condition = null
);


/// <summary>
///   A token policy defining allowed or denied permissions and resources.
///   <para>
///     Each policy specifies an effect (allow or deny), the permission groups that
///     define what actions are permitted, and the resources the policy applies to.
///   </para>
/// </summary>
/// <param name="Id">Policy identifier. May be null for new policies.</param>
/// <param name="Effect">Policy effect: "allow" or "deny".</param>
/// <param name="PermissionGroups">Permission groups granted by this policy.</param>
/// <param name="Resources">Resources this policy applies to. Keys are resource types, values are resource identifiers or wildcards.</param>
public record TokenPolicy(
  [property: JsonPropertyName("id")]
  string? Id,
  [property: JsonPropertyName("effect")]
  string Effect,
  [property: JsonPropertyName("permission_groups")]
  IReadOnlyList<TokenPermissionGroupReference> PermissionGroups,
  [property: JsonPropertyName("resources")]
  IReadOnlyDictionary<string, string> Resources
);


/// <summary>Reference to a permission group within a token policy.</summary>
/// <param name="Id">Permission group identifier.</param>
/// <param name="Meta">Optional metadata for the permission group.</param>
public record TokenPermissionGroupReference(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("meta")]
  IReadOnlyDictionary<string, JsonElement>? Meta = null
);


/// <summary>
///   IP-based access conditions for a token.
///   <para>
///     Conditions allow restricting token usage based on the source IP address
///     of the API request.
///   </para>
/// </summary>
/// <param name="RequestIp">IP address restrictions.</param>
public record TokenCondition(
  [property: JsonPropertyName("request_ip")]
  TokenIpCondition? RequestIp = null
);


/// <summary>
///   IP address inclusion/exclusion rules for token access.
///   <para>
///     Supports both allowlist (in) and denylist (not_in) patterns using CIDR notation.
///   </para>
/// </summary>
/// <param name="In">CIDR ranges from which the token can be used. Example: ["192.168.1.0/24", "10.0.0.0/8"]</param>
/// <param name="NotIn">CIDR ranges from which the token cannot be used. Example: ["192.168.1.100/32"]</param>
public record TokenIpCondition(
  [property: JsonPropertyName("in")]
  IReadOnlyList<string>? In = null,
  [property: JsonPropertyName("not_in")]
  IReadOnlyList<string>? NotIn = null
);

#endregion


#region PermissionGroup Model

/// <summary>
///   Represents a permission group that can be assigned to API tokens.
///   <para>
///     Permission groups define what API operations a token can perform.
///     Use <see cref="IApiTokensApi.GetAccountPermissionGroupsAsync" /> to discover available groups.
///   </para>
/// </summary>
/// <param name="Id">Permission group identifier.</param>
/// <param name="Name">Human-readable permission group name.</param>
/// <param name="Scopes">API scopes this permission group grants access to.</param>
public record PermissionGroup(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("scopes")]
  IReadOnlyList<string> Scopes
);

#endregion


#region Request Models

/// <summary>
///   Request to create a new API token.
/// </summary>
/// <param name="Name">Human-readable token name.</param>
/// <param name="Policies">Token policies defining permissions.</param>
/// <param name="ExpiresOn">When the token expires.</param>
/// <param name="NotBefore">Token is not valid before this time.</param>
/// <param name="Condition">IP-based access restrictions.</param>
/// <example>
///   <code>
///   var request = new CreateApiTokenRequest(
///     Name: "CI/CD Token",
///     Policies: new[]
///     {
///       new CreateTokenPolicyRequest(
///         Effect: "allow",
///         PermissionGroups: new[] { new TokenPermissionGroupReference("...") },
///         Resources: new Dictionary&lt;string, string&gt;
///         {
///           ["com.cloudflare.api.account.*"] = "*"
///         })
///     },
///     ExpiresOn: DateTime.UtcNow.AddYears(1));
///   </code>
/// </example>
public record CreateApiTokenRequest(
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("policies")]
  IReadOnlyList<CreateTokenPolicyRequest> Policies,
  [property: JsonPropertyName("expires_on")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  DateTime? ExpiresOn = null,
  [property: JsonPropertyName("not_before")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  DateTime? NotBefore = null,
  [property: JsonPropertyName("condition")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  TokenCondition? Condition = null
);


/// <summary>Policy definition for creating a token.</summary>
/// <param name="Effect">Policy effect: "allow" or "deny".</param>
/// <param name="PermissionGroups">Permission groups to include in this policy.</param>
/// <param name="Resources">Resources this policy applies to.</param>
public record CreateTokenPolicyRequest(
  [property: JsonPropertyName("effect")]
  string Effect,
  [property: JsonPropertyName("permission_groups")]
  IReadOnlyList<TokenPermissionGroupReference> PermissionGroups,
  [property: JsonPropertyName("resources")]
  IReadOnlyDictionary<string, string> Resources
);


/// <summary>Request to update an existing API token.</summary>
/// <param name="Name">Updated token name.</param>
/// <param name="Policies">Updated token policies.</param>
/// <param name="ExpiresOn">Updated expiration time.</param>
/// <param name="NotBefore">Updated not-before time.</param>
/// <param name="Condition">Updated IP restrictions.</param>
/// <param name="Status">Updated status.</param>
public record UpdateApiTokenRequest(
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("policies")]
  IReadOnlyList<CreateTokenPolicyRequest> Policies,
  [property: JsonPropertyName("expires_on")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  DateTime? ExpiresOn = null,
  [property: JsonPropertyName("not_before")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  DateTime? NotBefore = null,
  [property: JsonPropertyName("condition")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  TokenCondition? Condition = null,
  [property: JsonPropertyName("status")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  TokenStatus? Status = null
);

#endregion


#region Result Models

/// <summary>
///   Result of creating a new API token, including the secret value.
///   <para>
///     <b>Important:</b> The <see cref="Value" /> property contains the token secret
///     and is only available at creation time. Store it securely - it cannot be retrieved again.
///   </para>
/// </summary>
/// <param name="Id">Token identifier.</param>
/// <param name="Name">Token name.</param>
/// <param name="Value">The token secret value. Only returned on creation. Store securely.</param>
/// <param name="Status">Token status.</param>
/// <param name="IssuedOn">When the token was created.</param>
/// <param name="ModifiedOn">When the token was last modified.</param>
/// <param name="Policies">Token policies.</param>
/// <param name="ExpiresOn">Token expiration time.</param>
/// <param name="NotBefore">Token validity start time.</param>
/// <param name="Condition">IP restrictions.</param>
public record CreateApiTokenResult(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("value")]
  string Value,
  [property: JsonPropertyName("status")]
  TokenStatus Status,
  [property: JsonPropertyName("issued_on")]
  DateTime IssuedOn,
  [property: JsonPropertyName("modified_on")]
  DateTime ModifiedOn,
  [property: JsonPropertyName("policies")]
  IReadOnlyList<TokenPolicy> Policies,
  [property: JsonPropertyName("expires_on")]
  DateTime? ExpiresOn = null,
  [property: JsonPropertyName("not_before")]
  DateTime? NotBefore = null,
  [property: JsonPropertyName("condition")]
  TokenCondition? Condition = null
);


/// <summary>Result of verifying a token.</summary>
/// <param name="Id">Token identifier.</param>
/// <param name="Status">Token status.</param>
/// <param name="ExpiresOn">Token expiration time.</param>
/// <param name="NotBefore">Token validity start time.</param>
public record VerifyTokenResult(
  [property: JsonPropertyName("id")]
  string Id,
  [property: JsonPropertyName("status")]
  TokenStatus Status,
  [property: JsonPropertyName("expires_on")]
  DateTime? ExpiresOn = null,
  [property: JsonPropertyName("not_before")]
  DateTime? NotBefore = null
);

#endregion


#region Filter Models

/// <summary>Filtering and pagination options for listing API tokens.</summary>
/// <param name="Page">Page number (minimum 1, default 1).</param>
/// <param name="PerPage">Results per page (5-50, default 20).</param>
/// <param name="Direction">Sort direction.</param>
public record ListApiTokensFilters(
  int? Page = null,
  int? PerPage = null,
  ListOrderDirection? Direction = null
);


/// <summary>Filtering options for listing permission groups.</summary>
/// <param name="Name">
///   Filter by permission group name.
///   <para>
///     <b>Note:</b> This parameter is documented by Cloudflare but appears to be non-functional.
///     The API returns empty results regardless of the filter value.
///   </para>
/// </param>
/// <param name="Scope">Filter by permission group scope (e.g., "com.cloudflare.api.account.zone").</param>
/// <remarks>
///   The permission_groups endpoint does NOT support pagination. All permission groups are returned
///   in a single response. The 'page' and 'per_page' parameters are not accepted by this endpoint.
/// </remarks>
public record ListPermissionGroupsFilters(
  string? Name = null,
  string? Scope = null
);

#endregion
