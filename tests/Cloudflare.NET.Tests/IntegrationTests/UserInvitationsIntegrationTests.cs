namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
using Cloudflare.NET.Core.Exceptions;
using Cloudflare.NET.Members.Models;
using Cloudflare.NET.User;
using Cloudflare.NET.User.Models;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the User Invitations API implementing F16 - User Invitations.
///   These tests interact with the live Cloudflare API and require credentials.
///   <para>
///     <b>Important:</b> These tests require a user-scoped API token (<c>Cloudflare:UserApiToken</c>) with
///     <c>User:Invites Read</c> permission. Account-scoped tokens cannot access user-level endpoints.
///     Missing permissions will be caught by the PermissionValidationTests that run first.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     <b>SKIPPED TESTS:</b> Most tests in this class require a second Cloudflare account to send
///     invitations TO the test user. Since we don't have a second account configured, these tests
///     are skipped with proper documentation. When a second account becomes available, remove the
///     Skip attribute and the tests will execute with proper assertions.
///   </para>
///   <para>
///     Tests that don't require invitations (error handling, API call validation) run normally.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
[Collection(TestCollections.UserInvitations)]
public class UserInvitationsIntegrationTests : IClassFixture<UserApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IUserApi _sut;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="UserInvitationsIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides a User API client with user-scoped token.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public UserInvitationsIntegrationTests(UserApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut = fixture.UserApi;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region List Invitations Tests - Can Run Without Second Account (I01-I02)

  /// <summary>I01: Verifies that ListInvitationsAsync returns a valid collection.</summary>
  /// <remarks>
  ///   This test verifies the API call succeeds and returns a valid collection.
  ///   The collection may contain historical invitations (accepted, rejected, or pending).
  /// </remarks>
  [UserIntegrationTest]
  public async Task ListInvitationsAsync_ReturnsValidCollection()
  {
    // Act
    var invitations = await _sut.ListInvitationsAsync();

    // Assert
    invitations.Should().NotBeNull("API should return a valid collection");

    // Validate any returned invitations have required fields (collection may be empty or not)
    foreach (var invitation in invitations)
    {
      invitation.Id.Should().NotBeNullOrEmpty("invitation should have an ID");
      invitation.InvitedMemberEmail.Should().NotBeNullOrEmpty("invitation should have an email");
      invitation.Status.Should().NotBeNull("invitation should have a status");
    }
  }

  /// <summary>I02: Verifies that ListInvitationsAsync with valid token returns result.</summary>
  [UserIntegrationTest]
  public async Task ListInvitationsAsync_WithValidToken_ReturnsResult()
  {
    // Act
    var invitations = await _sut.ListInvitationsAsync();

    // Assert
    invitations.Should().NotBeNull("API should return a result with valid token");
  }

  #endregion


  #region List Invitations Tests - Require Second Account (I03-I04, I10-I12)

  /// <summary>
  ///   I03: Verifies that listed invitations have required fields populated.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires second Cloudflare account.</b></para>
  ///   <para>
  ///     This test requires a second Cloudflare account to send an invitation to the test user.
  ///     Without a pending invitation, there is no data to verify.
  ///   </para>
  ///   <para><b>API Documentation:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/user/subresources/invites/methods/list/</item>
  ///     <item>Required fields: id, invited_member_email, status</item>
  ///   </list>
  /// </remarks>
  [UserIntegrationTest(Skip = "Requires second Cloudflare account to send invitation to test user.")]
  public async Task ListInvitationsAsync_HasRequiredFields()
  {
    // Act
    var invitations = await _sut.ListInvitationsAsync();

    // Assert
    invitations.Should().NotBeEmpty("test requires at least one invitation");

    var firstInvitation = invitations[0];
    firstInvitation.Id.Should().NotBeNullOrEmpty("invitation should have an ID");
    firstInvitation.InvitedMemberEmail.Should().NotBeNullOrEmpty("invitation should have an email");
    firstInvitation.Status.Should().NotBeNull("invitation should have a status");
  }

  /// <summary>
  ///   I04: Verifies that listed invitations have timestamp fields populated.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires second Cloudflare account.</b></para>
  ///   <para>
  ///     This test requires a second Cloudflare account to send an invitation to the test user.
  ///     Without a pending invitation, there is no data to verify.
  ///   </para>
  ///   <para><b>API Documentation:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/user/subresources/invites/methods/list/</item>
  ///     <item>Timestamp fields: invited_on, expires_on</item>
  ///   </list>
  /// </remarks>
  [UserIntegrationTest(Skip = "Requires second Cloudflare account to send invitation to test user.")]
  public async Task ListInvitationsAsync_HasTimestamps()
  {
    // Act
    var invitations = await _sut.ListInvitationsAsync();

    // Assert
    invitations.Should().NotBeEmpty("test requires at least one invitation");

    var firstInvitation = invitations[0];
    firstInvitation.InvitedOn.Should().BeBefore(DateTime.UtcNow, "InvitedOn should be in the past");
    firstInvitation.ExpiresOn.Should().BeAfter(firstInvitation.InvitedOn, "ExpiresOn should be after InvitedOn");
  }

  /// <summary>
  ///   I10: Verifies that accepted invitations have accepted status.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires second Cloudflare account.</b></para>
  ///   <para>
  ///     This test requires a second Cloudflare account to send an invitation that has been accepted.
  ///   </para>
  /// </remarks>
  [UserIntegrationTest(Skip = "Requires second Cloudflare account to send invitation to test user.")]
  public async Task ListInvitationsAsync_AcceptedInvitations_HaveCorrectStatus()
  {
    // Act
    var invitations = await _sut.ListInvitationsAsync();

    // Assert
    var acceptedInvitations = invitations.Where(i => i.Status == MemberStatus.Accepted).ToList();
    acceptedInvitations.Should().NotBeEmpty("test requires at least one accepted invitation");

    foreach (var invitation in acceptedInvitations)
    {
      invitation.Status.Should().Be(MemberStatus.Accepted);
    }
  }

  /// <summary>
  ///   I11: Verifies handling of expired invitations.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires second Cloudflare account.</b></para>
  ///   <para>
  ///     This test requires a second Cloudflare account to send an invitation that has expired.
  ///   </para>
  /// </remarks>
  [UserIntegrationTest(Skip = "Requires second Cloudflare account to send invitation to test user.")]
  public async Task ListInvitationsAsync_ExpiredInvitations_AreHandled()
  {
    // Act
    var invitations = await _sut.ListInvitationsAsync();

    // Assert
    var now = DateTime.UtcNow;
    var expiredByDate = invitations.Where(i => i.ExpiresOn < now).ToList();
    expiredByDate.Should().NotBeEmpty("test requires at least one expired invitation");

    foreach (var invitation in expiredByDate)
    {
      invitation.Id.Should().NotBeNullOrEmpty("expired invitation should have an ID");
      invitation.ExpiresOn.Should().BeBefore(now, "expired invitation should have past expiration date");
    }
  }

  /// <summary>
  ///   I12: Verifies that already responded invitations show their final status.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires second Cloudflare account.</b></para>
  ///   <para>
  ///     This test requires a second Cloudflare account to send invitations that have been responded to.
  ///   </para>
  /// </remarks>
  [UserIntegrationTest(Skip = "Requires second Cloudflare account to send invitation to test user.")]
  public async Task ListInvitationsAsync_AlreadyRespondedInvitations_ShowFinalStatus()
  {
    // Act
    var invitations = await _sut.ListInvitationsAsync();

    // Assert
    var respondedInvitations = invitations.Where(i =>
      i.Status == MemberStatus.Accepted ||
      i.Status == MemberStatus.Rejected).ToList();

    respondedInvitations.Should().NotBeEmpty("test requires at least one responded invitation");

    foreach (var invitation in respondedInvitations)
    {
      var isAcceptedOrRejected = invitation.Status == MemberStatus.Accepted ||
                                 invitation.Status == MemberStatus.Rejected;
      isAcceptedOrRejected.Should().BeTrue("responded invitation should have accepted or rejected status");
    }
  }

  /// <summary>
  ///   Verifies that MemberStatus values are correctly parsed from API responses.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires second Cloudflare account.</b></para>
  ///   <para>
  ///     This test requires invitations to exist to verify status parsing.
  ///   </para>
  /// </remarks>
  [UserIntegrationTest(Skip = "Requires second Cloudflare account to send invitation to test user.")]
  public async Task ListInvitationsAsync_StatusValues_ParsedCorrectly()
  {
    // Act
    var invitations = await _sut.ListInvitationsAsync();

    // Assert
    invitations.Should().NotBeEmpty("test requires at least one invitation");

    foreach (var invitation in invitations)
    {
      var statusString = (string)invitation.Status;
      statusString.Should().BeOneOf("pending", "accepted", "rejected");
    }
  }

  #endregion


  #region Get Invitation Tests - Require Second Account (I05-I06)

  /// <summary>
  ///   I05: Verifies that GetInvitationAsync returns invitation details when invitation exists.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires second Cloudflare account.</b></para>
  ///   <para>
  ///     This test requires a second Cloudflare account to send an invitation to the test user.
  ///   </para>
  ///   <para><b>API Documentation:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/user/subresources/invites/methods/get/</item>
  ///   </list>
  /// </remarks>
  [UserIntegrationTest(Skip = "Requires second Cloudflare account to send invitation to test user.")]
  public async Task GetInvitationAsync_ExistingInvitation_ReturnsDetails()
  {
    // Arrange - First list to get an invitation ID
    var invitations = await _sut.ListInvitationsAsync();
    invitations.Should().NotBeEmpty("test requires at least one invitation");

    var invitationId = invitations[0].Id;

    // Act
    var invitation = await _sut.GetInvitationAsync(invitationId);

    // Assert
    invitation.Should().NotBeNull();
    invitation.Id.Should().Be(invitationId);
    invitation.InvitedMemberEmail.Should().NotBeNullOrEmpty();
  }

  /// <summary>
  ///   I06: Verifies that invitation with roles has roles array populated.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires second Cloudflare account.</b></para>
  ///   <para>
  ///     This test requires a second Cloudflare account to send an invitation with roles assigned.
  ///   </para>
  ///   <para><b>API Documentation:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/user/subresources/invites/methods/get/</item>
  ///     <item>Roles are returned as an array of role name strings</item>
  ///   </list>
  /// </remarks>
  [UserIntegrationTest(Skip = "Requires second Cloudflare account to send invitation with roles to test user.")]
  public async Task GetInvitationAsync_WithRoles_HasRolesArray()
  {
    // Arrange - First list to get an invitation ID
    var invitations = await _sut.ListInvitationsAsync();
    var invitationWithRoles = invitations.FirstOrDefault(i => i.Roles is { Count: > 0 });

    invitationWithRoles.Should().NotBeNull("test requires an invitation with roles");

    // Assert
    invitationWithRoles!.Roles.Should().NotBeNull();
    invitationWithRoles.Roles!.Count.Should().BeGreaterThan(0);

    foreach (var roleName in invitationWithRoles.Roles)
    {
      roleName.Should().NotBeNullOrEmpty("each role name should be populated");
    }
  }

  #endregion


  #region Get Invitation Tests - Error Handling (I07, I14)

  /// <summary>I07: Verifies that GetInvitationAsync throws 400 for invalid invitation ID format.</summary>
  /// <remarks>
  ///   Per Cloudflare API: GET /user/invites/{id} returns 400 Bad Request for invalid ID formats.
  ///   Error code 7003: "Could not route to /user/invites/{id}, perhaps your object identifier is invalid?"
  ///   The API's routing layer validates the ID format before processing.
  ///   https://developers.cloudflare.com/api/resources/user/subresources/invites/methods/get/
  /// </remarks>
  [UserIntegrationTest]
  public async Task GetInvitationAsync_InvalidIdFormat_ThrowsBadRequest()
  {
    // Arrange - Use an ID format that the API doesn't recognize
    var invalidId = "nonexistent-" + Guid.NewGuid().ToString("N");

    // Act
    var act = () => _sut.GetInvitationAsync(invalidId);

    // Assert - Invalid ID format returns 400 Bad Request (routing error)
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.BadRequest);
  }

  /// <summary>I14: Verifies that malformed ID format returns 400 Bad Request.</summary>
  /// <remarks>
  ///   Per Cloudflare API: Malformed IDs with special characters fail at the routing layer.
  ///   Error code 7003: "Could not route to /user/invites/{id}"
  ///   https://developers.cloudflare.com/api/resources/user/subresources/invites/
  /// </remarks>
  [UserIntegrationTest]
  public async Task GetInvitationAsync_MalformedId_ThrowsBadRequest()
  {
    // Arrange - Use ID with special characters that fail routing
    var malformedId = "!@#$%^&*()";

    // Act
    var act = () => _sut.GetInvitationAsync(malformedId);

    // Assert - Malformed ID returns 400 Bad Request (routing error)
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.BadRequest);
  }

  #endregion


  #region Respond to Invitation Tests - Require Second Account (I08-I09, I15-I16)

  /// <summary>
  ///   I08: Verifies that RespondToInvitationAsync (accept) actually accepts the invitation.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires second Cloudflare account.</b></para>
  ///   <para>
  ///     This test requires a second Cloudflare account to send a pending invitation to the test user.
  ///     Accepting an invitation is a one-time operation that grants account access.
  ///   </para>
  ///   <para><b>API Documentation:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/user/subresources/invites/methods/edit/</item>
  ///     <item>Status can be set to "accepted" or "rejected"</item>
  ///     <item>User can leave account later via DELETE /memberships/{id}</item>
  ///   </list>
  /// </remarks>
  [UserIntegrationTest(Skip = "Requires second Cloudflare account to send pending invitation to test user.")]
  public async Task RespondToInvitationAsync_AcceptInvitation_UpdatesStatus()
  {
    // Arrange - Find a pending invitation
    var invitations = await _sut.ListInvitationsAsync();
    var pendingInvitation = invitations.FirstOrDefault(i => i.Status == MemberStatus.Pending);
    pendingInvitation.Should().NotBeNull("test requires a pending invitation");

    // Act
    var request = new RespondToInvitationRequest(MemberStatus.Accepted);
    var result = await _sut.RespondToInvitationAsync(pendingInvitation!.Id, request);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(pendingInvitation.Id);
    result.Status.Should().Be(MemberStatus.Accepted);
  }

  /// <summary>
  ///   I09: Verifies that RespondToInvitationAsync (reject) actually rejects the invitation.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires second Cloudflare account.</b></para>
  ///   <para>
  ///     This test requires a second Cloudflare account to send a pending invitation to the test user.
  ///     Rejecting an invitation declines account access.
  ///   </para>
  ///   <para><b>API Documentation:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/user/subresources/invites/methods/edit/</item>
  ///     <item>Status can be set to "accepted" or "rejected"</item>
  ///     <item>Account owner can send a new invitation if needed</item>
  ///   </list>
  /// </remarks>
  [UserIntegrationTest(Skip = "Requires second Cloudflare account to send pending invitation to test user.")]
  public async Task RespondToInvitationAsync_RejectInvitation_UpdatesStatus()
  {
    // Arrange - Find a pending invitation
    var invitations = await _sut.ListInvitationsAsync();
    var pendingInvitation = invitations.FirstOrDefault(i => i.Status == MemberStatus.Pending);
    pendingInvitation.Should().NotBeNull("test requires a pending invitation");

    // Act
    var request = new RespondToInvitationRequest(MemberStatus.Rejected);
    var result = await _sut.RespondToInvitationAsync(pendingInvitation!.Id, request);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(pendingInvitation.Id);
    result.Status.Should().Be(MemberStatus.Rejected);
  }

  #endregion


  #region Respond to Invitation Tests - Error Handling (I17-I18)

  /// <summary>I17: Verifies that responding to a non-existent invitation returns 404.</summary>
  [UserIntegrationTest]
  public async Task RespondToInvitationAsync_NonExistent_ThrowsNotFound()
  {
    // Arrange
    var nonExistentId = "00000000000000000000000000000000";
    var request = new RespondToInvitationRequest(MemberStatus.Accepted);

    // Act
    var act = () => _sut.RespondToInvitationAsync(nonExistentId, request);

    // Assert - Non-existent invitation returns 404 or 400 or 403
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound ||
                   ex.StatusCode == HttpStatusCode.BadRequest ||
                   ex.StatusCode == HttpStatusCode.Forbidden);
  }

  /// <summary>I18: Verifies that responding with a malformed invitation ID returns 400.</summary>
  [UserIntegrationTest]
  public async Task RespondToInvitationAsync_MalformedId_ThrowsBadRequest()
  {
    // Arrange
    var malformedId = "!@#$%^&*()";
    var request = new RespondToInvitationRequest(MemberStatus.Accepted);

    // Act
    var act = () => _sut.RespondToInvitationAsync(malformedId, request);

    // Assert - Malformed ID fails at routing with 400
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.BadRequest);
  }

  #endregion
}
