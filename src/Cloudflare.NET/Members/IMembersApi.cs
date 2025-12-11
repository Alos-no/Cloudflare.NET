namespace Cloudflare.NET.Members;

using Core.Models;
using Models;


/// <summary>
///   Provides access to Cloudflare Account Members API.
///   <para>
///     Account members are users who have been granted access to an account's resources.
///     This API allows inviting new members, managing their roles, and removing access.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     When a new member is invited, they receive an email and must accept the invitation
///     to gain access. Members can have multiple roles that define their permissions.
///   </para>
///   <para>
///     <b>Important:</b> Modifying or removing members may have security implications.
///     Ensure you have proper authorization before making changes.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // List all members
///   await foreach (var member in client.Members.ListAllAccountMembersAsync(accountId))
///   {
///     Console.WriteLine($"{member.User.Email} - {member.Status}");
///   }
///
///   // Invite a new member
///   var request = new CreateAccountMemberRequest(
///     Email: "newuser@example.com",
///     Roles: new[] { roleId }
///   );
///   var newMember = await client.Members.CreateAccountMemberAsync(accountId, request);
///   </code>
/// </example>
public interface IMembersApi
{
  #region Account Members

  /// <summary>
  ///   Lists all members of an account.
  ///   <para>
  ///     Returns paginated results of account members with their status and roles.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filtering and pagination options.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A page of account members.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="accountId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var members = await client.Members.ListAccountMembersAsync(accountId,
  ///     new ListAccountMembersFilters(Status: MemberStatus.Accepted));
  ///
  ///   foreach (var member in members.Items)
  ///     Console.WriteLine($"{member.User.Email}: {member.Status}");
  ///   </code>
  /// </example>
  Task<PagePaginatedResult<AccountMember>> ListAccountMembersAsync(
    string accountId,
    ListAccountMembersFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists all members of an account, automatically handling pagination.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filtering options. Pagination parameters are ignored.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An async enumerable of all account members.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="accountId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   // Iterate through all members without manual pagination
  ///   await foreach (var member in client.Members.ListAllAccountMembersAsync(accountId))
  ///   {
  ///     Console.WriteLine($"{member.User.Email}: {string.Join(", ", member.Roles.Select(r => r.Name))}");
  ///   }
  ///   </code>
  /// </example>
  IAsyncEnumerable<AccountMember> ListAllAccountMembersAsync(
    string accountId,
    ListAccountMembersFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Gets details for a specific account member.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="memberId">The membership identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The member details including user info and roles.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="accountId"/> or <paramref name="memberId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> or <paramref name="memberId"/> is empty or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the member is not found (HTTP 404).</exception>
  /// <example>
  ///   <code>
  ///   var member = await client.Members.GetAccountMemberAsync(accountId, memberId);
  ///   Console.WriteLine($"Member: {member.User.Email}");
  ///   Console.WriteLine($"Status: {member.Status}");
  ///   Console.WriteLine($"Roles: {string.Join(", ", member.Roles.Select(r => r.Name))}");
  ///   </code>
  /// </example>
  Task<AccountMember> GetAccountMemberAsync(
    string accountId,
    string memberId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Invites a new member to the account.
  ///   <para>
  ///     An invitation email will be sent to the specified email address.
  ///     The user must accept the invitation to become an active member.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="request">The invitation details including email and roles.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created member (with status "pending" until accepted).</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="accountId"/> or <paramref name="request"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> is empty or whitespace.</exception>
  /// <remarks>
  ///   <para>
  ///     The email address must not already be associated with an existing member.
  ///     At least one role must be specified.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   var request = new CreateAccountMemberRequest(
  ///     Email: "developer@example.com",
  ///     Roles: new[] { dnsAdminRoleId }
  ///   );
  ///   var member = await client.Members.CreateAccountMemberAsync(accountId, request);
  ///   Console.WriteLine($"Invited {member.User.Email}, status: {member.Status}");
  ///   </code>
  /// </example>
  Task<AccountMember> CreateAccountMemberAsync(
    string accountId,
    CreateAccountMemberRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Updates an existing account member's roles.
  ///   <para>
  ///     Only the roles assigned to the member can be updated.
  ///     The user's email and status cannot be changed.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="memberId">The membership identifier.</param>
  /// <param name="request">The updated member details.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated member.</returns>
  /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> or <paramref name="memberId"/> is empty or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the member is not found (HTTP 404).</exception>
  /// <example>
  ///   <code>
  ///   var request = new UpdateAccountMemberRequest(
  ///     Roles: new[] { viewerRoleId, dnsAdminRoleId }
  ///   );
  ///   var updated = await client.Members.UpdateAccountMemberAsync(accountId, memberId, request);
  ///   </code>
  /// </example>
  Task<AccountMember> UpdateAccountMemberAsync(
    string accountId,
    string memberId,
    UpdateAccountMemberRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Removes a member from the account.
  ///   <para>
  ///     The user will immediately lose access to the account's resources.
  ///     This action cannot be undone - the user must be re-invited to regain access.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="memberId">The membership identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>Result containing the deleted membership ID.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="accountId"/> or <paramref name="memberId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> or <paramref name="memberId"/> is empty or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the member is not found (HTTP 404).</exception>
  /// <remarks>
  ///   <para>
  ///     <b>Warning:</b> You cannot remove yourself from an account. Attempting to do so
  ///     will result in an error.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   var result = await client.Members.DeleteAccountMemberAsync(accountId, memberId);
  ///   Console.WriteLine($"Removed member: {result.Id}");
  ///   </code>
  /// </example>
  Task<DeleteAccountMemberResult> DeleteAccountMemberAsync(
    string accountId,
    string memberId,
    CancellationToken cancellationToken = default);

  #endregion
}
