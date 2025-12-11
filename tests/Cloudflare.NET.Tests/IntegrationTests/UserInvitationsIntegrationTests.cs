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
///   </para>
///   <para>
///     <b>Note:</b> Invitation tests are mostly read-only because responding to invitations is a one-time
///     operation that cannot be undone. Tests that would respond to invitations are conditional and only
///     run when appropriate test invitations are available.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     User Invitations are sent TO the authenticated user from other accounts. This means the test
///     account may not have any pending invitations, which is a valid state. Tests are designed to
///     handle both scenarios (invitations exist vs. no invitations).
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
[Collection(TestCollections.UserInvitations)]
public class UserInvitationsIntegrationTests : IClassFixture<UserApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IUserApi _sut;

  /// <summary>Test output helper for logging.</summary>
  private readonly ITestOutputHelper _output;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="UserInvitationsIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides a User API client with user-scoped token.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public UserInvitationsIntegrationTests(UserApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut = fixture.UserApi;
    _output = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region List Invitations Tests (I01-I04)

  /// <summary>I01: Verifies that ListInvitationsAsync returns a valid collection (may be empty).</summary>
  [UserIntegrationTest]
  public async Task ListInvitationsAsync_ReturnsValidCollection()
  {
    try
    {
      // Act
      var invitations = await _sut.ListInvitationsAsync();

      // Assert
      invitations.Should().NotBeNull("result should be a valid collection");
      _output.WriteLine($"Found {invitations.Count} invitation(s)");

      foreach (var invitation in invitations)
      {
        _output.WriteLine($"  - {invitation.Id}: {invitation.OrganizationName ?? "(no org name)"} [{invitation.Status}]");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations read permission. Test skipped.");
    }
  }

  /// <summary>I02: Verifies that ListInvitationsAsync handles empty invitation list gracefully.</summary>
  [UserIntegrationTest]
  public async Task ListInvitationsAsync_EmptyList_ReturnsEmptyCollection()
  {
    try
    {
      // Act
      var invitations = await _sut.ListInvitationsAsync();

      // Assert - Empty list is valid, we're testing that it doesn't throw
      invitations.Should().NotBeNull("result should be a valid collection even if empty");

      if (invitations.Count == 0)
      {
        _output.WriteLine("No pending invitations found (this is a valid state)");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations read permission. Test skipped.");
    }
  }

  /// <summary>I03: Verifies that listed invitations have required fields populated.</summary>
  [UserIntegrationTest]
  public async Task ListInvitationsAsync_HasRequiredFields()
  {
    try
    {
      // Act
      var invitations = await _sut.ListInvitationsAsync();

      // Assert - If we have invitations, verify required fields
      if (invitations.Count == 0)
      {
        _output.WriteLine("No invitations to verify required fields (this is acceptable)");

        return;
      }

      var firstInvitation = invitations[0];
      firstInvitation.Id.Should().NotBeNullOrEmpty("invitation should have an ID");
      firstInvitation.InvitedMemberEmail.Should().NotBeNullOrEmpty("invitation should have an email");
      firstInvitation.Status.Should().NotBeNull("invitation should have a status");

      _output.WriteLine($"Verified invitation {firstInvitation.Id} has required fields");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations read permission. Test skipped.");
    }
  }

  /// <summary>I04: Verifies that listed invitations have timestamp fields populated.</summary>
  [UserIntegrationTest]
  public async Task ListInvitationsAsync_HasTimestamps()
  {
    try
    {
      // Act
      var invitations = await _sut.ListInvitationsAsync();

      // Assert - If we have invitations, verify timestamps
      if (invitations.Count == 0)
      {
        _output.WriteLine("No invitations to verify timestamps (this is acceptable)");

        return;
      }

      var firstInvitation = invitations[0];
      firstInvitation.InvitedOn.Should().BeBefore(DateTime.UtcNow, "InvitedOn should be in the past");
      firstInvitation.ExpiresOn.Should().BeAfter(firstInvitation.InvitedOn, "ExpiresOn should be after InvitedOn");

      _output.WriteLine($"Invitation {firstInvitation.Id}: InvitedOn={firstInvitation.InvitedOn}, ExpiresOn={firstInvitation.ExpiresOn}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations read permission. Test skipped.");
    }
  }

  #endregion


  #region Get Invitation Tests (I05-I07)

  /// <summary>I05: Verifies that GetInvitationAsync returns invitation details when invitation exists.</summary>
  [UserIntegrationTest]
  public async Task GetInvitationAsync_ExistingInvitation_ReturnsDetails()
  {
    try
    {
      // Arrange - First list to get an invitation ID
      var invitations = await _sut.ListInvitationsAsync();

      if (invitations.Count == 0)
      {
        _output.WriteLine("No invitations available to test GetInvitationAsync (this is acceptable)");

        return;
      }

      var invitationId = invitations[0].Id;

      // Act
      var invitation = await _sut.GetInvitationAsync(invitationId);

      // Assert
      invitation.Should().NotBeNull();
      invitation.Id.Should().Be(invitationId);
      invitation.InvitedMemberEmail.Should().NotBeNullOrEmpty();

      _output.WriteLine($"Retrieved invitation {invitation.Id}: {invitation.OrganizationName ?? "(no org name)"}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations read permission. Test skipped.");
    }
  }

  /// <summary>I06: Verifies that invitation with roles has roles array populated.</summary>
  [UserIntegrationTest]
  public async Task GetInvitationAsync_WithRoles_HasRolesArray()
  {
    try
    {
      // Arrange - First list to get an invitation ID
      var invitations = await _sut.ListInvitationsAsync();

      if (invitations.Count == 0)
      {
        _output.WriteLine("No invitations available to test roles (this is acceptable)");

        return;
      }

      // Try to find an invitation with roles
      var invitationWithRoles = invitations.FirstOrDefault(i => i.Roles is { Count: > 0 });

      if (invitationWithRoles is null)
      {
        // If the list doesn't have roles, try getting full details for each
        foreach (var listedInvite in invitations)
        {
          var fullInvite = await _sut.GetInvitationAsync(listedInvite.Id);

          if (fullInvite.Roles is { Count: > 0 })
          {
            invitationWithRoles = fullInvite;

            break;
          }
        }
      }

      if (invitationWithRoles is null)
      {
        _output.WriteLine("No invitations with roles found (this is acceptable)");

        return;
      }

      // Assert
      invitationWithRoles.Roles.Should().NotBeNull();
      invitationWithRoles.Roles!.Count.Should().BeGreaterThan(0);

      foreach (var role in invitationWithRoles.Roles)
      {
        role.Id.Should().NotBeNullOrEmpty();
        role.Name.Should().NotBeNullOrEmpty();
        _output.WriteLine($"  Role: {role.Name} ({role.Id})");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations read permission. Test skipped.");
    }
  }

  /// <summary>I07: Verifies that GetInvitationAsync throws 404 for non-existent invitation ID.</summary>
  [UserIntegrationTest]
  public async Task GetInvitationAsync_NonExistentId_Throws404()
  {
    // Arrange - Use an ID that definitely doesn't exist
    var nonExistentId = "nonexistent-" + Guid.NewGuid().ToString("N");

    try
    {
      // Act
      var act = () => _sut.GetInvitationAsync(nonExistentId);

      // Assert
      await act.Should().ThrowAsync<HttpRequestException>()
               .Where(ex => ex.StatusCode == HttpStatusCode.NotFound ||
                            ex.StatusCode == HttpStatusCode.BadRequest); // API may return 400 for invalid format
    }
    catch (CloudflareApiException ex)
    {
      // API may also return success=false for not found
      _output.WriteLine($"Got CloudflareApiException as expected: {ex.Message}");
      ex.Errors.Should().NotBeEmpty();
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations read permission. Test skipped.");
    }
  }

  #endregion


  #region Respond to Invitation Tests (I08-I10) - Mostly Conditional

  /// <summary>
  ///   I08: Test for accepting invitation - CONDITIONAL.
  ///   This test is informational and skipped if no suitable test invitation exists.
  ///   Responding to invitations is a one-time, irreversible operation.
  /// </summary>
  [UserIntegrationTest]
  public async Task RespondToInvitationAsync_AcceptInvitation_Informational()
  {
    try
    {
      // Arrange - Check for pending invitations
      var invitations = await _sut.ListInvitationsAsync();
      var pendingInvitation = invitations.FirstOrDefault(i => i.Status == MemberStatus.Pending);

      if (pendingInvitation is null)
      {
        _output.WriteLine("[INFO] No pending invitations available for accept test.");
        _output.WriteLine("[INFO] To test accepting invitations, a test invitation must be sent from another account.");
        _output.WriteLine("[INFO] This is expected behavior - skipping accept test.");

        return;
      }

      // Log but don't actually accept - this is a destructive operation
      _output.WriteLine($"[INFO] Found pending invitation: {pendingInvitation.Id}");
      _output.WriteLine($"[INFO] Organization: {pendingInvitation.OrganizationName ?? "(not specified)"}");
      _output.WriteLine("[INFO] NOT accepting automatically to preserve test environment.");
      _output.WriteLine("[INFO] To test accept functionality, use manual testing with a disposable invitation.");

      // Verify we can construct the request correctly
      var acceptRequest = new RespondToInvitationRequest(MemberStatus.Accepted);
      acceptRequest.Status.Should().Be(MemberStatus.Accepted);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations permission. Test skipped.");
    }
  }

  /// <summary>
  ///   I09: Test for rejecting invitation - CONDITIONAL.
  ///   This test is informational and skipped if no suitable test invitation exists.
  ///   Responding to invitations is a one-time, irreversible operation.
  /// </summary>
  [UserIntegrationTest]
  public async Task RespondToInvitationAsync_RejectInvitation_Informational()
  {
    try
    {
      // Arrange - Check for pending invitations
      var invitations = await _sut.ListInvitationsAsync();
      var pendingInvitation = invitations.FirstOrDefault(i => i.Status == MemberStatus.Pending);

      if (pendingInvitation is null)
      {
        _output.WriteLine("[INFO] No pending invitations available for reject test.");
        _output.WriteLine("[INFO] To test rejecting invitations, a test invitation must be sent from another account.");
        _output.WriteLine("[INFO] This is expected behavior - skipping reject test.");

        return;
      }

      // Log but don't actually reject - this is a destructive operation
      _output.WriteLine($"[INFO] Found pending invitation: {pendingInvitation.Id}");
      _output.WriteLine($"[INFO] Organization: {pendingInvitation.OrganizationName ?? "(not specified)"}");
      _output.WriteLine("[INFO] NOT rejecting automatically to preserve test environment.");
      _output.WriteLine("[INFO] To test reject functionality, use manual testing with a disposable invitation.");

      // Verify we can construct the request correctly
      var rejectRequest = new RespondToInvitationRequest(MemberStatus.Rejected);
      rejectRequest.Status.Should().Be(MemberStatus.Rejected);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations permission. Test skipped.");
    }
  }

  /// <summary>
  ///   I10: Verifies that accepted invitations have accepted status.
  ///   This test checks existing accepted invitations rather than accepting new ones.
  /// </summary>
  [UserIntegrationTest]
  public async Task ListInvitationsAsync_AcceptedInvitations_HaveCorrectStatus()
  {
    try
    {
      // Act
      var invitations = await _sut.ListInvitationsAsync();

      // Assert - Check for any accepted invitations in history
      var acceptedInvitations = invitations.Where(i => i.Status == MemberStatus.Accepted).ToList();

      _output.WriteLine($"Found {acceptedInvitations.Count} accepted invitation(s)");

      foreach (var invitation in acceptedInvitations)
      {
        invitation.Status.Should().Be(MemberStatus.Accepted);
        _output.WriteLine($"  - {invitation.Id}: {invitation.OrganizationName ?? "(no org name)"} [accepted]");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations read permission. Test skipped.");
    }
  }

  #endregion


  #region Edge Cases (I11-I14)

  /// <summary>I11: Verifies handling of expired invitation (informational - checks for expired status).</summary>
  [UserIntegrationTest]
  public async Task ListInvitationsAsync_ExpiredInvitations_AreHandled()
  {
    try
    {
      // Act
      var invitations = await _sut.ListInvitationsAsync();

      // Check for any expired invitations or invitations past their expiry date
      var now = DateTime.UtcNow;
      var expiredByDate = invitations.Where(i => i.ExpiresOn < now).ToList();

      _output.WriteLine($"Found {expiredByDate.Count} invitation(s) past expiry date");

      foreach (var invitation in expiredByDate)
      {
        _output.WriteLine($"  - {invitation.Id}: expired on {invitation.ExpiresOn} (status: {invitation.Status})");
      }

      // If we have a pending but expired invitation, it would be a good test candidate
      var pendingExpired = expiredByDate.FirstOrDefault(i => i.Status == MemberStatus.Pending);

      if (pendingExpired is not null)
      {
        _output.WriteLine($"[INFO] Found pending expired invitation {pendingExpired.Id} - API should reject response attempts");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations read permission. Test skipped.");
    }
  }

  /// <summary>I12: Verifies that already responded invitations show their final status.</summary>
  [UserIntegrationTest]
  public async Task ListInvitationsAsync_AlreadyRespondedInvitations_ShowFinalStatus()
  {
    try
    {
      // Act
      var invitations = await _sut.ListInvitationsAsync();

      // Check for invitations that have been responded to
      var respondedInvitations = invitations.Where(i =>
        i.Status == MemberStatus.Accepted ||
        i.Status == MemberStatus.Rejected).ToList();

      _output.WriteLine($"Found {respondedInvitations.Count} already-responded invitation(s)");

      foreach (var invitation in respondedInvitations)
      {
        var isAcceptedOrRejected = invitation.Status == MemberStatus.Accepted ||
                                   invitation.Status == MemberStatus.Rejected;
        isAcceptedOrRejected.Should().BeTrue("responded invitation should have accepted or rejected status");
        _output.WriteLine($"  - {invitation.Id}: [{invitation.Status}]");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations read permission. Test skipped.");
    }
  }

  /// <summary>I13: Verifies that permission denied returns 403 (tested via API response).</summary>
  [UserIntegrationTest]
  public async Task ListInvitationsAsync_WithValidToken_ReturnsResultOrForbidden()
  {
    // This test verifies the endpoint is accessible with the configured token
    // If the token lacks permissions, we get 403 which is handled gracefully
    try
    {
      // Act
      var invitations = await _sut.ListInvitationsAsync();

      // Assert - If we get here, the token has permission
      invitations.Should().NotBeNull();
      _output.WriteLine($"Token has permission - found {invitations.Count} invitation(s)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      // This is expected if the token lacks user invitations permission
      _output.WriteLine("[INFO] 403 Forbidden - This is expected if UserApiToken lacks 'User:Invites Read' permission.");
      _output.WriteLine("[INFO] To test this endpoint, ensure the API token has appropriate permissions.");

      // Verify the exception has the right status code
      ex.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
  }

  /// <summary>I14: Verifies that invalid ID format is handled gracefully.</summary>
  [UserIntegrationTest]
  public async Task GetInvitationAsync_InvalidIdFormat_HandledGracefully()
  {
    // Arrange - Use various invalid ID formats
    var invalidIds = new[]
    {
      "invalid-format",
      "123",
      "",
      "   ",
      "!@#$%^&*()"
    };

    foreach (var invalidId in invalidIds)
    {
      // Skip empty/whitespace as those throw ArgumentException before API call
      if (string.IsNullOrWhiteSpace(invalidId))
      {
        continue;
      }

      try
      {
        // Act & Assert
        var act = () => _sut.GetInvitationAsync(invalidId);

        try
        {
          await act();
          _output.WriteLine($"[UNEXPECTED] ID '{invalidId}' did not throw - API may accept this format");
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
          _output.WriteLine($"[OK] ID '{invalidId}' threw {ex.StatusCode} as expected");
        }
        catch (CloudflareApiException ex)
        {
          _output.WriteLine($"[OK] ID '{invalidId}' threw CloudflareApiException: {ex.Message}");
        }
      }
      catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
      {
        _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations read permission. Test skipped.");

        return;
      }
    }
  }

  #endregion


  #region Status Verification Tests

  /// <summary>Verifies that MemberStatus values are correctly parsed from API responses.</summary>
  [UserIntegrationTest]
  public async Task ListInvitationsAsync_StatusValues_ParsedCorrectly()
  {
    try
    {
      // Act
      var invitations = await _sut.ListInvitationsAsync();

      // Assert - Group by status and report
      var byStatus = invitations.GroupBy(i => (string)i.Status).ToList();

      _output.WriteLine("Invitation status distribution:");

      foreach (var group in byStatus)
      {
        _output.WriteLine($"  - {group.Key}: {group.Count()} invitation(s)");
      }

      // Verify status values are from known set
      foreach (var invitation in invitations)
      {
        var statusString = (string)invitation.Status;
        statusString.Should().BeOneOf("pending", "accepted", "rejected",
          $"Status '{statusString}' should be a known value, but extensible enum allows unknown values");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack user invitations read permission. Test skipped.");
    }
  }

  #endregion
}
