namespace Cloudflare.NET.User;

using Core.Models;
using Models;

/// <summary>
///   Defines the contract for interacting with the Cloudflare User API.
///   <para>
///     This includes retrieving and editing the authenticated user's profile.
///     The User API operates on the currently authenticated user only (self-only access).
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     All operations in this interface affect only the authenticated user's own profile.
///     There are no endpoints to view or modify other users' profiles.
///   </para>
/// </remarks>
/// <seealso href="https://developers.cloudflare.com/api/resources/user/" />
public interface IUserApi
{
  #region User Profile

  /// <summary>
  ///   Gets the authenticated user's profile.
  ///   <para>
  ///     Returns the full user object including email, name, contact details,
  ///     security settings, and subscription tier information.
  ///   </para>
  /// </summary>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The user's profile.</returns>
  /// <example>
  ///   <code>
  ///   var user = await client.User.GetUserAsync();
  ///   Console.WriteLine($"Hello, {user.FirstName}!");
  ///   Console.WriteLine($"Email: {user.Email}");
  ///   Console.WriteLine($"2FA enabled: {user.TwoFactorAuthenticationEnabled}");
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/user/methods/get/" />
  Task<User> GetUserAsync(CancellationToken cancellationToken = default);

  /// <summary>
  ///   Edits the authenticated user's profile.
  ///   <para>
  ///     Only the fields provided in the request will be updated.
  ///     Fields set to null are not modified.
  ///   </para>
  /// </summary>
  /// <param name="request">The fields to update.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated user profile.</returns>
  /// <remarks>
  ///   <para>
  ///     Only the following fields can be edited: FirstName, LastName, Country, Telephone, and Zipcode.
  ///     Email, 2FA settings, and suspension status are managed through separate Cloudflare flows.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Update only first and last name
  ///   var updated = await client.User.EditUserAsync(
  ///     new EditUserRequest(
  ///       FirstName: "John",
  ///       LastName: "Doe"));
  ///
  ///   // Update all editable fields
  ///   var updated = await client.User.EditUserAsync(
  ///     new EditUserRequest(
  ///       FirstName: "John",
  ///       LastName: "Doe",
  ///       Country: "US",
  ///       Telephone: "+1-555-555-5555",
  ///       Zipcode: "12345"));
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/user/methods/edit/" />
  Task<User> EditUserAsync(
    EditUserRequest request,
    CancellationToken cancellationToken = default);

  #endregion


  #region User Invitations

  /// <summary>
  ///   Lists all pending invitations for the authenticated user.
  ///   <para>
  ///     Returns invitations sent TO the authenticated user from various accounts.
  ///     Use <see cref="RespondToInvitationAsync"/> to accept or reject invitations.
  ///   </para>
  /// </summary>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>All invitations for the authenticated user.</returns>
  /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code.</exception>
  /// <exception cref="Core.Exceptions.CloudflareApiException">Thrown when the API returns a non-success response.</exception>
  /// <remarks>
  ///   <para><b>Preview:</b> This operation has limited test coverage.</para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   var invitations = await client.User.ListInvitationsAsync();
  ///   foreach (var invite in invitations)
  ///   {
  ///     Console.WriteLine($"Invitation to: {invite.OrganizationName}");
  ///     Console.WriteLine($"Status: {invite.Status}");
  ///     Console.WriteLine($"Expires: {invite.ExpiresOn}");
  ///   }
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/user/subresources/invites/methods/list/" />
  Task<IReadOnlyList<UserInvitation>> ListInvitationsAsync(
    CancellationToken cancellationToken = default);


  /// <summary>
  ///   Gets details for a specific invitation.
  /// </summary>
  /// <param name="invitationId">The invitation identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The invitation details.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="invitationId"/> is null or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code (404 if not found).</exception>
  /// <exception cref="Core.Exceptions.CloudflareApiException">Thrown when the API returns a non-success response.</exception>
  /// <remarks>
  ///   <para><b>Preview:</b> This operation has limited test coverage.</para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   var invitation = await client.User.GetInvitationAsync(invitationId);
  ///   Console.WriteLine($"Organization: {invitation.OrganizationName}");
  ///   Console.WriteLine($"Roles: {string.Join(", ", invitation.Roles?.Select(r => r.Name) ?? [])}");
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/user/subresources/invites/methods/get/" />
  Task<UserInvitation> GetInvitationAsync(
    string invitationId,
    CancellationToken cancellationToken = default);


  /// <summary>
  ///   Responds to an invitation (accept or reject).
  ///   <para>
  ///     <b>Warning:</b> This action cannot be undone. Once an invitation is accepted or rejected,
  ///     its status cannot be changed.
  ///   </para>
  /// </summary>
  /// <param name="invitationId">The invitation identifier to respond to.</param>
  /// <param name="request">The response containing the desired status (accepted or rejected).</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated invitation with the new status.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="invitationId"/> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code (404 if not found).</exception>
  /// <exception cref="Core.Exceptions.CloudflareApiException">Thrown when the invitation is expired, already responded to, or other API error.</exception>
  /// <remarks>
  ///   <para><b>Preview:</b> This operation has limited test coverage.</para>
  ///   <para>
  ///     <b>Accepting:</b> Upon acceptance, the user becomes a member of the account with the roles
  ///     specified in the invitation. The new membership will appear in both the User Memberships API
  ///     and the Account Members API.
  ///   </para>
  ///   <para>
  ///     <b>Rejecting:</b> The invitation is marked as rejected. The account admin may send a new invitation.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Accept an invitation
  ///   var accept = new RespondToInvitationRequest(MemberStatus.Accepted);
  ///   var result = await client.User.RespondToInvitationAsync(invitationId, accept);
  ///   Console.WriteLine($"Invitation {result.Status}!");
  ///
  ///   // Reject an invitation
  ///   var reject = new RespondToInvitationRequest(MemberStatus.Rejected);
  ///   var result = await client.User.RespondToInvitationAsync(invitationId, reject);
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/user/subresources/invites/methods/edit/" />
  Task<UserInvitation> RespondToInvitationAsync(
    string invitationId,
    RespondToInvitationRequest request,
    CancellationToken cancellationToken = default);

  #endregion


  #region User Memberships

  /// <summary>
  ///   Lists all account memberships for the authenticated user.
  ///   <para>
  ///     Memberships represent the accounts the user has access to.
  ///     Use <see cref="UpdateMembershipAsync"/> to accept or reject pending invitations.
  ///   </para>
  /// </summary>
  /// <param name="filters">Optional filtering and pagination options.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A page of memberships.</returns>
  /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code.</exception>
  /// <exception cref="Core.Exceptions.CloudflareApiException">Thrown when the API returns a non-success response.</exception>
  /// <example>
  ///   <code>
  ///   // Get all pending invitations
  ///   var pending = await client.User.ListMembershipsAsync(
  ///     new ListMembershipsFilters(Status: MemberStatus.Pending));
  ///
  ///   foreach (var membership in pending.Items)
  ///   {
  ///     Console.WriteLine($"Invitation from: {membership.Account.Name}");
  ///   }
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/memberships/methods/list/" />
  Task<PagePaginatedResult<Membership>> ListMembershipsAsync(
    ListMembershipsFilters? filters = null,
    CancellationToken cancellationToken = default);


  /// <summary>
  ///   Lists all memberships, automatically handling pagination.
  /// </summary>
  /// <param name="filters">Optional filtering options. Pagination parameters are ignored.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An async enumerable of all memberships.</returns>
  /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code.</exception>
  /// <exception cref="Core.Exceptions.CloudflareApiException">Thrown when the API returns a non-success response.</exception>
  /// <example>
  ///   <code>
  ///   await foreach (var membership in client.User.ListAllMembershipsAsync())
  ///   {
  ///     Console.WriteLine($"Account: {membership.Account.Name} [{membership.Status}]");
  ///   }
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/memberships/methods/list/" />
  IAsyncEnumerable<Membership> ListAllMembershipsAsync(
    ListMembershipsFilters? filters = null,
    CancellationToken cancellationToken = default);


  /// <summary>
  ///   Gets details for a specific membership.
  /// </summary>
  /// <param name="membershipId">The membership identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The membership details.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="membershipId"/> is null or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code (404 if not found).</exception>
  /// <exception cref="Core.Exceptions.CloudflareApiException">Thrown when the API returns a non-success response.</exception>
  /// <example>
  ///   <code>
  ///   var membership = await client.User.GetMembershipAsync(membershipId);
  ///   Console.WriteLine($"Account: {membership.Account.Name}");
  ///   Console.WriteLine($"Status: {membership.Status}");
  ///   Console.WriteLine($"API Access: {membership.ApiAccessEnabled}");
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/memberships/methods/get/" />
  Task<Membership> GetMembershipAsync(
    string membershipId,
    CancellationToken cancellationToken = default);


  /// <summary>
  ///   Updates a membership status (accept or reject an invitation).
  ///   <para>
  ///     <b>Warning:</b> This action cannot be undone. Once a membership is accepted
  ///     or rejected, its status cannot be changed (except by leaving the account).
  ///   </para>
  /// </summary>
  /// <param name="membershipId">The membership identifier.</param>
  /// <param name="request">The update parameters containing the new status.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated membership.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="membershipId"/> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code (404 if not found).</exception>
  /// <exception cref="Core.Exceptions.CloudflareApiException">Thrown when the API returns a non-success response.</exception>
  /// <remarks>
  ///   <para><b>Preview:</b> This operation has limited test coverage.</para>
  ///   <para>
  ///     <b>Accepting:</b> Upon acceptance, the user gains access to the account
  ///     with the permissions defined in the membership.
  ///   </para>
  ///   <para>
  ///     <b>Rejecting:</b> The invitation is declined and the user does not gain access.
  ///     The account admin may send a new invitation.
  ///   </para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Accept an invitation
  ///   var accept = new UpdateMembershipRequest(MemberStatus.Accepted);
  ///   var result = await client.User.UpdateMembershipAsync(membershipId, accept);
  ///   Console.WriteLine($"Membership status: {result.Status}");
  ///
  ///   // Reject an invitation
  ///   var reject = new UpdateMembershipRequest(MemberStatus.Rejected);
  ///   var result = await client.User.UpdateMembershipAsync(membershipId, reject);
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/memberships/methods/edit/" />
  Task<Membership> UpdateMembershipAsync(
    string membershipId,
    UpdateMembershipRequest request,
    CancellationToken cancellationToken = default);


  /// <summary>
  ///   Deletes a membership (leave an account).
  ///   <para>
  ///     <b>Warning:</b> This removes the user's access to the account.
  ///     To regain access, a new invitation must be sent by the account admin.
  ///   </para>
  /// </summary>
  /// <param name="membershipId">The membership identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <exception cref="ArgumentException">Thrown when <paramref name="membershipId"/> is null or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code (404 if not found).</exception>
  /// <exception cref="Core.Exceptions.CloudflareApiException">Thrown when the API returns a non-success response.</exception>
  /// <remarks>
  ///   <para><b>Preview:</b> This operation has limited test coverage.</para>
  /// </remarks>
  /// <example>
  ///   <code>
  ///   // Leave an account
  ///   await client.User.DeleteMembershipAsync(membershipId);
  ///   </code>
  /// </example>
  /// <seealso href="https://developers.cloudflare.com/api/resources/memberships/methods/delete/" />
  Task DeleteMembershipAsync(
    string membershipId,
    CancellationToken cancellationToken = default);

  #endregion
}
