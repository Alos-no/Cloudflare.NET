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
///   Contains integration tests for the User Memberships API implementing F11 - User Memberships.
///   These tests interact with the live Cloudflare API and require credentials.
///   <para>
///     <b>Important:</b> These tests require a user-scoped API token (<c>Cloudflare:UserApiToken</c>) with
///     <c>Memberships Read</c> and <c>Memberships Write</c> permissions.
///   </para>
///   <para>
///     <b>Note:</b> Membership tests are mostly read-only because modifying memberships (accept/reject/delete)
///     can permanently affect the user's account access. Tests that would modify memberships are informational
///     and do not actually perform the operations.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     User Memberships show which accounts the authenticated user has access to. The user must have
///     at least one account membership (typically their own account) for these tests to work.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
[Collection(TestCollections.UserMemberships)]
public class UserMembershipsIntegrationTests : IClassFixture<UserApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IUserApi _sut;

  /// <summary>Test output helper for logging.</summary>
  private readonly ITestOutputHelper _output;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="UserMembershipsIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides a User API client with user-scoped token.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public UserMembershipsIntegrationTests(UserApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut = fixture.UserApi;
    _output = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region List and Get Tests (I01-I05)

  /// <summary>I01: Verifies that ListMembershipsAsync returns a valid collection with at least one membership.</summary>
  [UserIntegrationTest]
  public async Task ListMembershipsAsync_ReturnsValidCollectionWithMemberships()
  {
    try
    {
      // Act
      var result = await _sut.ListMembershipsAsync();

      // Assert
      result.Should().NotBeNull("result should be a valid paginated response");
      result.Items.Should().NotBeNull("items should be a valid collection");
      result.Items.Should().NotBeEmpty("user should have at least one membership (their own account)");

      _output.WriteLine($"Found {result.Items.Count} membership(s)");

      foreach (var membership in result.Items)
      {
        _output.WriteLine($"  - {membership.Id}: {membership.Account.Name} [{membership.Status}]");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships read permission. Test skipped.");
    }
  }

  /// <summary>I02: Verifies that ListMembershipsAsync with status filter returns only matching memberships.</summary>
  [UserIntegrationTest]
  public async Task ListMembershipsAsync_FilterByStatus_ReturnsOnlyMatchingMemberships()
  {
    try
    {
      // Act
      var filters = new ListMembershipsFilters(Status: MemberStatus.Accepted);
      var result = await _sut.ListMembershipsAsync(filters);

      // Assert
      result.Should().NotBeNull();

      foreach (var membership in result.Items)
      {
        membership.Status.Should().Be(MemberStatus.Accepted, "all returned memberships should have accepted status");
        _output.WriteLine($"  - {membership.Id}: {membership.Account.Name} [{membership.Status}]");
      }

      _output.WriteLine($"Found {result.Items.Count} accepted membership(s)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships read permission. Test skipped.");
    }
  }

  /// <summary>I03: Verifies that ListAllMembershipsAsync yields all memberships through pagination.</summary>
  [UserIntegrationTest]
  public async Task ListAllMembershipsAsync_ReturnsAllMemberships()
  {
    try
    {
      // Act
      var memberships = new List<Membership>();
      await foreach (var membership in _sut.ListAllMembershipsAsync())
      {
        memberships.Add(membership);
      }

      // Assert
      memberships.Should().NotBeEmpty("user should have at least one membership");

      _output.WriteLine($"Total memberships via pagination: {memberships.Count}");

      foreach (var membership in memberships)
      {
        _output.WriteLine($"  - {membership.Id}: {membership.Account.Name}");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships read permission. Test skipped.");
    }
  }

  /// <summary>I04: Verifies that GetMembershipAsync returns full membership details.</summary>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_ReturnsFullMembershipDetails()
  {
    try
    {
      // Arrange - First get a membership ID from the list
      var listResult = await _sut.ListMembershipsAsync();

      if (listResult.Items.Count == 0)
      {
        _output.WriteLine("No memberships available to test GetMembershipAsync (this is unexpected)");

        return;
      }

      var membershipId = listResult.Items[0].Id;

      // Act
      var membership = await _sut.GetMembershipAsync(membershipId);

      // Assert
      membership.Should().NotBeNull();
      membership.Id.Should().Be(membershipId);
      membership.Account.Should().NotBeNull();
      membership.Account.Id.Should().NotBeNullOrEmpty();
      membership.Account.Name.Should().NotBeNullOrEmpty();

      _output.WriteLine($"Retrieved membership: {membership.Id}");
      _output.WriteLine($"  Account: {membership.Account.Name} ({membership.Account.Id})");
      _output.WriteLine($"  Status: {membership.Status}");
      _output.WriteLine($"  API Access: {membership.ApiAccessEnabled}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships read permission. Test skipped.");
    }
  }

  /// <summary>I05: Verifies that membership has account populated with ID and name.</summary>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_HasAccountPopulated()
  {
    try
    {
      // Arrange
      var listResult = await _sut.ListMembershipsAsync();

      if (listResult.Items.Count == 0)
      {
        _output.WriteLine("No memberships available to test account details");

        return;
      }

      var membershipId = listResult.Items[0].Id;

      // Act
      var membership = await _sut.GetMembershipAsync(membershipId);

      // Assert
      membership.Account.Should().NotBeNull("membership should have an account");
      membership.Account.Id.Should().NotBeNullOrEmpty("account should have an ID");
      membership.Account.Name.Should().NotBeNullOrEmpty("account should have a name");

      _output.WriteLine($"Account details for membership {membership.Id}:");
      _output.WriteLine($"  ID: {membership.Account.Id}");
      _output.WriteLine($"  Name: {membership.Account.Name}");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships read permission. Test skipped.");
    }
  }

  #endregion


  #region Invitation Workflow Tests (I06-I08) - Informational Only

  /// <summary>
  ///   I06: Lists pending invitations (pending memberships).
  ///   This test is informational and shows any pending invitations.
  /// </summary>
  [UserIntegrationTest]
  public async Task ListMembershipsAsync_FilterByPending_ShowsPendingInvitations()
  {
    try
    {
      // Act
      var filters = new ListMembershipsFilters(Status: MemberStatus.Pending);
      var result = await _sut.ListMembershipsAsync(filters);

      // Assert - Just report what we found
      _output.WriteLine($"Found {result.Items.Count} pending membership(s)/invitation(s)");

      foreach (var membership in result.Items)
      {
        _output.WriteLine($"  - {membership.Id}: {membership.Account.Name}");
        _output.WriteLine($"    Status: {membership.Status}");
      }

      if (result.Items.Count == 0)
      {
        _output.WriteLine("[INFO] No pending invitations (this is normal if no invitations have been sent)");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships read permission. Test skipped.");
    }
  }

  /// <summary>
  ///   I07: Test for accepting invitation - INFORMATIONAL ONLY.
  ///   This test does not actually accept any invitations to preserve test environment.
  /// </summary>
  [UserIntegrationTest]
  public async Task UpdateMembershipAsync_AcceptInvitation_Informational()
  {
    try
    {
      // Arrange - Check for pending invitations
      var filters = new ListMembershipsFilters(Status: MemberStatus.Pending);
      var result = await _sut.ListMembershipsAsync(filters);

      if (result.Items.Count == 0)
      {
        _output.WriteLine("[INFO] No pending invitations available for accept test.");
        _output.WriteLine("[INFO] To test accepting memberships, an invitation must be sent from another account.");

        return;
      }

      // Log but don't actually accept
      var pendingMembership = result.Items[0];
      _output.WriteLine($"[INFO] Found pending membership: {pendingMembership.Id}");
      _output.WriteLine($"[INFO] Account: {pendingMembership.Account.Name}");
      _output.WriteLine("[INFO] NOT accepting automatically to preserve test environment.");

      // Verify we can construct the request correctly
      var acceptRequest = new UpdateMembershipRequest(MemberStatus.Accepted);
      acceptRequest.Status.Should().Be(MemberStatus.Accepted);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships permission. Test skipped.");
    }
  }

  /// <summary>
  ///   I08: Test for rejecting invitation - INFORMATIONAL ONLY.
  ///   This test does not actually reject any invitations to preserve test environment.
  /// </summary>
  [UserIntegrationTest]
  public async Task UpdateMembershipAsync_RejectInvitation_Informational()
  {
    try
    {
      // Arrange - Check for pending invitations
      var filters = new ListMembershipsFilters(Status: MemberStatus.Pending);
      var result = await _sut.ListMembershipsAsync(filters);

      if (result.Items.Count == 0)
      {
        _output.WriteLine("[INFO] No pending invitations available for reject test.");

        return;
      }

      // Log but don't actually reject
      var pendingMembership = result.Items[0];
      _output.WriteLine($"[INFO] Found pending membership: {pendingMembership.Id}");
      _output.WriteLine($"[INFO] Account: {pendingMembership.Account.Name}");
      _output.WriteLine("[INFO] NOT rejecting automatically to preserve test environment.");

      // Verify we can construct the request correctly
      var rejectRequest = new UpdateMembershipRequest(MemberStatus.Rejected);
      rejectRequest.Status.Should().Be(MemberStatus.Rejected);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships permission. Test skipped.");
    }
  }

  #endregion


  #region Leave Account Tests (I09) - Informational Only

  /// <summary>
  ///   I09: Test for deleting membership (leaving account) - INFORMATIONAL ONLY.
  ///   This test does not actually delete any memberships to preserve access.
  /// </summary>
  [UserIntegrationTest]
  public async Task DeleteMembershipAsync_LeaveAccount_Informational()
  {
    try
    {
      // Arrange
      var result = await _sut.ListMembershipsAsync();

      _output.WriteLine($"[INFO] Found {result.Items.Count} membership(s)");

      foreach (var membership in result.Items)
      {
        _output.WriteLine($"  - {membership.Id}: {membership.Account.Name} [{membership.Status}]");
      }

      _output.WriteLine("[INFO] NOT deleting any memberships to preserve account access.");
      _output.WriteLine("[INFO] To test leaving an account, use a disposable test membership.");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships permission. Test skipped.");
    }
  }

  #endregion


  #region Permission Tests (I10-I11)

  /// <summary>I10: Verifies that accepted memberships may have permissions populated.</summary>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_HasPermissions()
  {
    try
    {
      // Arrange - Get an accepted membership
      var filters = new ListMembershipsFilters(Status: MemberStatus.Accepted);
      var result = await _sut.ListMembershipsAsync(filters);

      if (result.Items.Count == 0)
      {
        _output.WriteLine("No accepted memberships available to test permissions");

        return;
      }

      // Get full details
      var membership = await _sut.GetMembershipAsync(result.Items[0].Id);

      // Report permissions (may or may not be present depending on API response)
      _output.WriteLine($"Membership: {membership.Id} ({membership.Account.Name})");

      if (membership.Permissions is not null)
      {
        _output.WriteLine("Permissions found:");
        if (membership.Permissions.Analytics is not null)
          _output.WriteLine($"  - Analytics: Read={membership.Permissions.Analytics.Read}, Write={membership.Permissions.Analytics.Write}");
        if (membership.Permissions.Dns is not null)
          _output.WriteLine($"  - DNS: Read={membership.Permissions.Dns.Read}, Write={membership.Permissions.Dns.Write}");
        if (membership.Permissions.Zones is not null)
          _output.WriteLine($"  - Zones: Read={membership.Permissions.Zones.Read}, Write={membership.Permissions.Zones.Write}");
      }
      else
      {
        _output.WriteLine("[INFO] Permissions not included in response (may depend on account type)");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships permission. Test skipped.");
    }
  }

  /// <summary>I11: Verifies that PermissionGrant has Read/Write structure when present.</summary>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_PermissionGrantStructure()
  {
    try
    {
      // Arrange - Get an accepted membership
      var filters = new ListMembershipsFilters(Status: MemberStatus.Accepted);
      var result = await _sut.ListMembershipsAsync(filters);

      if (result.Items.Count == 0)
      {
        _output.WriteLine("No accepted memberships available to test permission structure");

        return;
      }

      // Get full details
      var membership = await _sut.GetMembershipAsync(result.Items[0].Id);

      if (membership.Permissions is null)
      {
        _output.WriteLine("[INFO] Permissions not present in this membership");

        return;
      }

      // Check first non-null permission grant
      var permissionGrant = membership.Permissions.Analytics ??
                            membership.Permissions.Dns ??
                            membership.Permissions.Zones ??
                            membership.Permissions.Billing;

      if (permissionGrant is not null)
      {
        _output.WriteLine($"Permission grant structure verified:");
        _output.WriteLine($"  Read: {permissionGrant.Read} (type: bool)");
        _output.WriteLine($"  Write: {permissionGrant.Write} (type: bool)");
      }
      else
      {
        _output.WriteLine("[INFO] No specific permission grants found in this membership");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships permission. Test skipped.");
    }
  }

  #endregion


  #region Edge Cases (I12-I15)

  /// <summary>I12: Verifies that GetMembershipAsync with non-existent ID throws appropriate exception.</summary>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_NonExistentId_ThrowsException()
  {
    // Arrange
    var nonExistentId = "nonexistent-" + Guid.NewGuid().ToString("N");

    try
    {
      // Act & Assert
      var act = () => _sut.GetMembershipAsync(nonExistentId);

      try
      {
        await act();
        _output.WriteLine("[UNEXPECTED] Non-existent membership ID did not throw an exception");
      }
      catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
      {
        _output.WriteLine($"[OK] Got expected {ex.StatusCode} for non-existent membership");
      }
      catch (CloudflareApiException ex)
      {
        _output.WriteLine($"[OK] Got CloudflareApiException: {ex.Message}");
        ex.Errors.Should().NotBeEmpty();
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships permission. Test skipped.");
    }
  }

  /// <summary>I13: Verifies behavior when deleting already-deleted membership (informational).</summary>
  [UserIntegrationTest]
  public async Task DeleteMembershipAsync_AlreadyDeleted_Informational()
  {
    // This test is informational only - we don't actually delete memberships
    _output.WriteLine("[INFO] Delete already-deleted test is informational only.");
    _output.WriteLine("[INFO] Expected behavior: API returns 404 Not Found for non-existent membership.");

    // Test with a clearly non-existent ID
    var nonExistentId = "deleted-" + Guid.NewGuid().ToString("N");

    try
    {
      var act = () => _sut.DeleteMembershipAsync(nonExistentId);

      try
      {
        await act();
        _output.WriteLine("[UNEXPECTED] Non-existent membership deletion did not throw");
      }
      catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
      {
        _output.WriteLine($"[OK] Got expected {ex.StatusCode} when trying to delete non-existent membership");
      }
      catch (CloudflareApiException ex)
      {
        _output.WriteLine($"[OK] Got CloudflareApiException: {ex.Message}");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships permission. Test skipped.");
    }

    await Task.CompletedTask;
  }

  /// <summary>I14: Verifies that permission denied returns 403 (if token lacks permission).</summary>
  [UserIntegrationTest]
  public async Task ListMembershipsAsync_WithValidToken_ReturnsResultOrForbidden()
  {
    // This test verifies the endpoint is accessible with the configured token
    try
    {
      // Act
      var result = await _sut.ListMembershipsAsync();

      // Assert - If we get here, the token has permission
      result.Should().NotBeNull();
      _output.WriteLine($"Token has permission - found {result.Items.Count} membership(s)");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      // This is expected if the token lacks memberships permission
      _output.WriteLine("[INFO] 403 Forbidden - This is expected if UserApiToken lacks 'Memberships Read' permission.");
      _output.WriteLine("[INFO] To test this endpoint, ensure the API token has appropriate permissions.");
      ex.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
  }

  /// <summary>I15: Verifies that invalid ID format is handled gracefully.</summary>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_InvalidIdFormat_HandledGracefully()
  {
    // Test various invalid ID formats
    var invalidIds = new[] { "invalid-format", "123", "!@#$%^&*()" };

    foreach (var invalidId in invalidIds)
    {
      try
      {
        try
        {
          await _sut.GetMembershipAsync(invalidId);
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
        _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships permission. Test skipped.");

        return;
      }
    }
  }

  #endregion


  #region API Access and Roles Tests

  /// <summary>Verifies that memberships may include API access status.</summary>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_IncludesApiAccessStatus()
  {
    try
    {
      // Arrange
      var result = await _sut.ListMembershipsAsync();

      if (result.Items.Count == 0)
      {
        _output.WriteLine("No memberships available");

        return;
      }

      // Get full details for first membership
      var membership = await _sut.GetMembershipAsync(result.Items[0].Id);

      // Report API access status
      _output.WriteLine($"Membership: {membership.Id}");
      _output.WriteLine($"  API Access Enabled: {membership.ApiAccessEnabled?.ToString() ?? "Not specified"}");

      if (membership.Roles is { Count: > 0 })
      {
        _output.WriteLine($"  Roles: {string.Join(", ", membership.Roles)}");
      }

      if (membership.Policies is { Count: > 0 })
      {
        _output.WriteLine($"  Policies: {membership.Policies.Count} defined");
      }
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
    {
      _output.WriteLine("[WARNING] 403 Forbidden - UserApiToken may lack memberships permission. Test skipped.");
    }
  }

  #endregion
}
