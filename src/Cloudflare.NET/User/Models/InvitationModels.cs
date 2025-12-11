namespace Cloudflare.NET.User.Models;

using System.Text.Json.Serialization;
using Members.Models;
using Roles.Models;

/// <summary>
///   Represents an invitation for a user to join an account.
///   <para>
///     Invitations are sent by account administrators and received by users.
///     Users can accept or reject invitations through the User Invitations API.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     <b>User-Scoped:</b> These invitations are the ones sent TO the authenticated user,
///     not invitations sent BY the user. Use the Account Members API (F09) to send invitations.
///   </para>
///   <para>
///     <b>One-Time Response:</b> Each invitation can only be responded to once.
///     After accepting or rejecting, the status cannot be changed.
///   </para>
/// </remarks>
/// <param name="Id">The unique invitation identifier.</param>
/// <param name="InvitedMemberEmail">The email address the invitation was sent to.</param>
/// <param name="Status">The invitation status (pending, accepted, or rejected).</param>
/// <param name="InvitedOn">When the invitation was sent.</param>
/// <param name="ExpiresOn">When the invitation expires.</param>
/// <param name="OrganizationName">The name of the account the user is invited to join.</param>
/// <param name="Roles">The roles that will be assigned upon acceptance.</param>
public record UserInvitation(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("invited_member_email")]
  string InvitedMemberEmail,

  [property: JsonPropertyName("status")]
  MemberStatus Status,

  [property: JsonPropertyName("invited_on")]
  DateTime InvitedOn,

  [property: JsonPropertyName("expires_on")]
  DateTime ExpiresOn,

  [property: JsonPropertyName("organization_name")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? OrganizationName = null,

  [property: JsonPropertyName("roles")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<AccountRole>? Roles = null
);


/// <summary>
///   Request to respond to an account invitation.
///   <para>
///     Use this to accept or reject a pending invitation.
///     Once responded, the invitation status cannot be changed.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     <b>Accepting:</b> Use <see cref="MemberStatus.Accepted"/> to accept the invitation.
///     This will add the user as a member of the account with the specified roles.
///   </para>
///   <para>
///     <b>Rejecting:</b> Use <see cref="MemberStatus.Rejected"/> to decline the invitation.
///     The user will not be added to the account.
///   </para>
/// </remarks>
/// <param name="Status">The response status (accepted or rejected).</param>
/// <example>
///   <code>
///   // Accept an invitation
///   var acceptRequest = new RespondToInvitationRequest(MemberStatus.Accepted);
///   var result = await client.User.RespondToInvitationAsync(invitationId, acceptRequest);
///
///   // Reject an invitation
///   var rejectRequest = new RespondToInvitationRequest(MemberStatus.Rejected);
///   var result = await client.User.RespondToInvitationAsync(invitationId, rejectRequest);
///   </code>
/// </example>
public record RespondToInvitationRequest(
  [property: JsonPropertyName("status")]
  MemberStatus Status
);
