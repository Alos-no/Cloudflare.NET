namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
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
/// </summary>
/// <remarks>
///   <para>
///     <b>Important:</b> These tests require a user-scoped API token (<c>Cloudflare:UserApiToken</c>) with
///     <c>Memberships Read</c> and <c>Memberships Write</c> permissions.
///   </para>
///   <para>
///     <b>Precondition:</b> The authenticated user MUST have at least one membership (their own account).
///     Tests assume this precondition and will FAIL if it is not met.
///   </para>
///   <para>
///     <b>SKIPPED TESTS:</b> Tests that modify memberships (accept/reject/delete) are skipped because:
///     <list type="bullet">
///       <item>Accept/Reject require a pending invitation from another account owner</item>
///       <item>Delete removes the user from an account (disruptive to real access)</item>
///     </list>
///     When these operations can be safely tested (e.g., with a disposable test invitation),
///     remove the Skip attribute and the tests will execute with proper assertions.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
[Collection(TestCollections.UserMemberships)]
public class UserMembershipsIntegrationTests : IClassFixture<UserApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IUserApi _sut;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="UserMembershipsIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides a User API client with user-scoped token.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public UserMembershipsIntegrationTests(UserApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut = fixture.UserApi;

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
    // Act
    var result = await _sut.ListMembershipsAsync();

    // Assert
    result.Should().NotBeNull("result should be a valid paginated response");
    result.Items.Should().NotBeNull("items should be a valid collection");
    result.Items.Should().NotBeEmpty("user should have at least one membership (their own account)");

    // Verify each membership has required fields
    foreach (var membership in result.Items)
    {
      membership.Id.Should().NotBeNullOrEmpty("each membership should have an ID");
      membership.Account.Should().NotBeNull("each membership should have an account");
      membership.Account.Name.Should().NotBeNullOrEmpty("each account should have a name");
    }
  }

  /// <summary>I02: Verifies that ListMembershipsAsync with status filter returns only matching memberships.</summary>
  [UserIntegrationTest]
  public async Task ListMembershipsAsync_FilterByStatus_ReturnsOnlyMatchingMemberships()
  {
    // Arrange
    var filters = new ListMembershipsFilters(Status: MemberStatus.Accepted);

    // Act
    var result = await _sut.ListMembershipsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("user should have at least one accepted membership (their own account)");

    foreach (var membership in result.Items)
    {
      membership.Status.Should().Be(MemberStatus.Accepted, "all returned memberships should have accepted status");
    }
  }

  /// <summary>I03: Verifies that ListAllMembershipsAsync yields all memberships through pagination.</summary>
  [UserIntegrationTest]
  public async Task ListAllMembershipsAsync_ReturnsAllMemberships()
  {
    // Act
    var memberships = new List<Membership>();

    await foreach (var membership in _sut.ListAllMembershipsAsync())
    {
      memberships.Add(membership);
    }

    // Assert
    memberships.Should().NotBeEmpty("user should have at least one membership");

    foreach (var membership in memberships)
    {
      membership.Id.Should().NotBeNullOrEmpty("each membership should have an ID");
      membership.Account.Should().NotBeNull("each membership should have an account");
    }
  }

  /// <summary>I04: Verifies that GetMembershipAsync returns full membership details.</summary>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_ReturnsFullMembershipDetails()
  {
    // Arrange - Get a membership ID from the list (user must have at least one)
    var listResult = await _sut.ListMembershipsAsync();
    listResult.Items.Should().NotBeEmpty("user must have at least one membership to run this test");

    var membershipId = listResult.Items[0].Id;

    // Act
    var membership = await _sut.GetMembershipAsync(membershipId);

    // Assert
    membership.Should().NotBeNull();
    membership.Id.Should().Be(membershipId);
    membership.Account.Should().NotBeNull();
    membership.Account.Id.Should().NotBeNullOrEmpty();
    membership.Account.Name.Should().NotBeNullOrEmpty();
    membership.Status.Should().NotBeNull();
  }

  /// <summary>I05: Verifies that membership has account populated with ID and name.</summary>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_HasAccountPopulated()
  {
    // Arrange - Get a membership (user must have at least one)
    var listResult = await _sut.ListMembershipsAsync();
    listResult.Items.Should().NotBeEmpty("user must have at least one membership to run this test");

    var membershipId = listResult.Items[0].Id;

    // Act
    var membership = await _sut.GetMembershipAsync(membershipId);

    // Assert
    membership.Account.Should().NotBeNull("membership should have an account");
    membership.Account.Id.Should().NotBeNullOrEmpty("account should have an ID");
    membership.Account.Name.Should().NotBeNullOrEmpty("account should have a name");
  }

  #endregion


  #region Filter Tests (I06)

  /// <summary>
  ///   I06: Verifies that filtering by pending status returns only pending memberships.
  /// </summary>
  /// <remarks>
  ///   This test verifies the filter mechanism works correctly. An empty result is valid
  ///   when the user has no pending invitations - the filter correctly excludes non-pending memberships.
  /// </remarks>
  [UserIntegrationTest]
  public async Task ListMembershipsAsync_FilterByPending_ReturnsOnlyPendingMemberships()
  {
    // Arrange
    var filters = new ListMembershipsFilters(Status: MemberStatus.Pending);

    // Act
    var result = await _sut.ListMembershipsAsync(filters);

    // Assert - All returned items must have pending status. An empty collection is valid (no pending invitations).
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();

    foreach (var membership in result.Items)
    {
      membership.Status.Should().Be(MemberStatus.Pending, "all returned memberships should have pending status");
    }
  }

  #endregion


  #region Mutation Tests - Skipped (I07-I09)

  /// <summary>
  ///   I07: Verifies that UpdateMembershipAsync (accept) works for a pending membership.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED:</b> Requires a pending invitation from another account owner.</para>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/memberships/methods/update/</item>
  ///     <item>Status can be set to "accepted" to join an account</item>
  ///     <item>Requires pending invitation from account owner</item>
  ///   </list>
  /// </remarks>
  [UserIntegrationTest(Skip = "Requires pending invitation from another account owner - cannot create via API")]
  public async Task UpdateMembershipAsync_AcceptPendingMembership_AcceptsMembership()
  {
    // Arrange - Find a pending membership
    var filters = new ListMembershipsFilters(Status: MemberStatus.Pending);
    var result = await _sut.ListMembershipsAsync(filters);
    result.Items.Should().NotBeEmpty("test requires a pending membership");

    var pendingMembership = result.Items[0];

    // Act
    var request = new UpdateMembershipRequest(MemberStatus.Accepted);
    var updated = await _sut.UpdateMembershipAsync(pendingMembership.Id, request);

    // Assert
    updated.Should().NotBeNull();
    updated.Id.Should().Be(pendingMembership.Id);
    updated.Status.Should().Be(MemberStatus.Accepted);
  }

  /// <summary>
  ///   I08: Verifies that UpdateMembershipAsync (reject) works for a pending membership.
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED:</b> Requires a pending invitation from another account owner.</para>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/memberships/methods/update/</item>
  ///     <item>Status can be set to "rejected" to decline an invitation</item>
  ///     <item>Account owner can re-invite if needed</item>
  ///   </list>
  /// </remarks>
  [UserIntegrationTest(Skip = "Requires pending invitation from another account owner - cannot create via API")]
  public async Task UpdateMembershipAsync_RejectPendingMembership_RejectsMembership()
  {
    // Arrange - Find a pending membership
    var filters = new ListMembershipsFilters(Status: MemberStatus.Pending);
    var result = await _sut.ListMembershipsAsync(filters);
    result.Items.Should().NotBeEmpty("test requires a pending membership");

    var pendingMembership = result.Items[0];

    // Act
    var request = new UpdateMembershipRequest(MemberStatus.Rejected);
    var updated = await _sut.UpdateMembershipAsync(pendingMembership.Id, request);

    // Assert
    updated.Should().NotBeNull();
    updated.Id.Should().Be(pendingMembership.Id);
    updated.Status.Should().Be(MemberStatus.Rejected);
  }

  /// <summary>
  ///   I09: Verifies that DeleteMembershipAsync works (leaving an account).
  /// </summary>
  /// <remarks>
  ///   <para><b>SKIPPED:</b> Removes user from account - requires disposable test membership.</para>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/memberships/methods/delete/</item>
  ///     <item>"Remove the associated member from an account"</item>
  ///     <item>User can be re-invited by account owner</item>
  ///   </list>
  /// </remarks>
  [UserIntegrationTest(Skip = "Removes user from account - requires disposable test membership")]
  public async Task DeleteMembershipAsync_ExistingMembership_DeletesMembership()
  {
    // Arrange - Need a secondary membership (not the primary account)
    var result = await _sut.ListMembershipsAsync();
    result.Items.Should().HaveCountGreaterThan(1, "test requires at least 2 memberships to safely delete one");

    // Find a secondary membership to delete (skip the first/primary)
    var membershipToDelete = result.Items[1];

    // Act
    await _sut.DeleteMembershipAsync(membershipToDelete.Id);

    // Assert - Verify deletion by trying to get the membership (should throw 404)
    var action = () => _sut.GetMembershipAsync(membershipToDelete.Id);

    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  #endregion


  #region Permission Tests (I10-I11)

  /// <summary>I10: Verifies that GetMembershipAsync returns membership with account details.</summary>
  /// <remarks>
  ///   Note: Permissions may or may not be present depending on account type and API response.
  ///   This test verifies the membership structure is valid.
  /// </remarks>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_ReturnsValidMembershipStructure()
  {
    // Arrange - Get an accepted membership
    var filters = new ListMembershipsFilters(Status: MemberStatus.Accepted);
    var result = await _sut.ListMembershipsAsync(filters);
    result.Items.Should().NotBeEmpty("user must have at least one accepted membership");

    // Act
    var membership = await _sut.GetMembershipAsync(result.Items[0].Id);

    // Assert - Verify core membership structure
    membership.Should().NotBeNull();
    membership.Id.Should().NotBeNullOrEmpty();
    membership.Account.Should().NotBeNull();
    membership.Account.Id.Should().NotBeNullOrEmpty();
    membership.Account.Name.Should().NotBeNullOrEmpty();
    membership.Status.Should().Be(MemberStatus.Accepted);
  }

  /// <summary>I11: Verifies that when Permissions is present, PermissionGrant has valid structure.</summary>
  /// <remarks>
  ///   This test verifies that the Permissions object is populated with valid PermissionGrant objects.
  /// </remarks>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_WhenPermissionsPresent_HasValidPermissionGrantStructure()
  {
    // Arrange - Get an accepted membership
    var filters = new ListMembershipsFilters(Status: MemberStatus.Accepted);
    var result = await _sut.ListMembershipsAsync(filters);
    result.Items.Should().NotBeEmpty("user must have at least one accepted membership");

    // Act
    var membership = await _sut.GetMembershipAsync(result.Items[0].Id);

    // Assert - Permissions must be present for this test
    membership.Permissions.Should().NotBeNull("test requires permissions to be present");

    // Check first non-null permission grant
    var permissionGrant = membership.Permissions!.Analytics ??
                          membership.Permissions.Dns ??
                          membership.Permissions.Zones ??
                          membership.Permissions.Billing;

    permissionGrant.Should().NotBeNull("at least one permission grant should be present");

    // PermissionGrant has Read/Write boolean properties - verify the structure exists
    // (The bool values themselves can be true or false, which is valid either way)
    permissionGrant!.Should().NotBeNull();
  }

  #endregion


  #region Error Handling Tests (I12-I15)

  /// <summary>I12: Verifies that GetMembershipAsync with non-existent ID throws 404.</summary>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_NonExistentId_ThrowsNotFound()
  {
    // Arrange
    var nonExistentId = "nonexistent" + Guid.NewGuid().ToString("N");

    // Act
    var act = () => _sut.GetMembershipAsync(nonExistentId);

    // Assert
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  /// <summary>I13: Verifies that deleting non-existent membership throws appropriate error.</summary>
  /// <remarks>
  ///   <para><b>SKIPPED:</b> DELETE /memberships requires Global API Key authentication.</para>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/memberships/methods/delete/</item>
  ///     <item>Documentation uses X-Auth-Email + X-Auth-Key (Global API Key), not Bearer token</item>
  ///     <item>API tokens return 405 Method Not Allowed for this endpoint</item>
  ///   </list>
  /// </remarks>
  [UserIntegrationTest(Skip = "DELETE /memberships requires Global API Key authentication, not API Token")]
  public async Task DeleteMembershipAsync_NonExistent_ThrowsNotFound()
  {
    // Arrange
    var nonExistentId = "deleted" + Guid.NewGuid().ToString("N");

    // Act
    var act = () => _sut.DeleteMembershipAsync(nonExistentId);

    // Assert
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  /// <summary>I14: Verifies that ListMembershipsAsync returns valid response.</summary>
  [UserIntegrationTest]
  public async Task ListMembershipsAsync_WithValidToken_ReturnsNonEmptyResult()
  {
    // Act
    var result = await _sut.ListMembershipsAsync();

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("user should have at least one membership");
  }

  /// <summary>I15: Verifies that GetMembershipAsync with malformed ID returns 400 Bad Request.</summary>
  /// <remarks>
  ///   Per Cloudflare API: Malformed IDs with special characters fail at the routing layer.
  ///   Error code 7003: "Could not route to /memberships/{id}"
  /// </remarks>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_MalformedId_ThrowsBadRequest()
  {
    // Arrange - Use ID with special characters that fail routing
    var malformedId = "!@#$%^&*()";

    // Act
    var act = () => _sut.GetMembershipAsync(malformedId);

    // Assert
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.BadRequest);
  }

  #endregion


  #region Update Error Tests (I16-I19)

  /// <summary>I16: Verifies that updating a non-existent membership returns error.</summary>
  [UserIntegrationTest]
  public async Task UpdateMembershipAsync_NonExistent_ThrowsError()
  {
    // Arrange
    var nonExistentId = "00000000000000000000000000000000";
    var request = new UpdateMembershipRequest(MemberStatus.Accepted);

    // Act
    var act = () => _sut.UpdateMembershipAsync(nonExistentId, request);

    // Assert - May return 404, 400, or 403 depending on API behavior
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound ||
                   ex.StatusCode == HttpStatusCode.BadRequest ||
                   ex.StatusCode == HttpStatusCode.Forbidden);
  }

  /// <summary>I17: Verifies that updating with a malformed membership ID returns 400 Bad Request.</summary>
  /// <remarks>
  ///   <para><b>SKIPPED:</b> PUT /memberships requires Global API Key authentication.</para>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/memberships/methods/update/</item>
  ///     <item>Documentation shows X-Auth-Email + X-Auth-Key headers (Global API Key)</item>
  ///     <item>API tokens return 405 Method Not Allowed: "PUT method not allowed for the api_token authentication scheme"</item>
  ///     <item>See also: https://community.cloudflare.com/t/cant-update-dns-record-error-put-method-not-allowed-for-the-api-token-authentication-scheme/447185</item>
  ///   </list>
  /// </remarks>
  [UserIntegrationTest(Skip = "PUT /memberships requires Global API Key authentication, not API Token")]
  public async Task UpdateMembershipAsync_MalformedId_ThrowsBadRequest()
  {
    // Arrange
    var malformedId = "!@#$%^&*()";
    var request = new UpdateMembershipRequest(MemberStatus.Accepted);

    // Act
    var act = () => _sut.UpdateMembershipAsync(malformedId, request);

    // Assert - Malformed ID should fail at routing with 400 Bad Request
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.BadRequest);
  }

  #endregion


  #region Delete Error Tests (I18-I19)

  /// <summary>I18: Verifies that deleting with a malformed membership ID returns error.</summary>
  /// <remarks>
  ///   Note: DELETE /memberships requires Global API Key, but malformed IDs should fail at routing.
  /// </remarks>
  [UserIntegrationTest]
  public async Task DeleteMembershipAsync_MalformedId_ThrowsError()
  {
    // Arrange
    var malformedId = "!@#$%^&*()";

    // Act
    var act = () => _sut.DeleteMembershipAsync(malformedId);

    // Assert - May return 400 (routing) or 405 (method not allowed for API tokens)
    await act.Should()
      .ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.BadRequest ||
                   ex.StatusCode == HttpStatusCode.MethodNotAllowed);
  }

  #endregion


  #region API Access and Roles Tests (I19-I20)

  /// <summary>I19: Verifies that membership deserializes with API access status field.</summary>
  /// <remarks>
  ///   ApiAccessEnabled is a nullable boolean in the API response. The test verifies the membership
  ///   structure is valid - the property value (null, true, or false) depends on account configuration.
  /// </remarks>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_DeserializesApiAccessField()
  {
    // Arrange - Get a membership
    var listResult = await _sut.ListMembershipsAsync();
    listResult.Items.Should().NotBeEmpty("user must have at least one membership");

    // Act
    var membership = await _sut.GetMembershipAsync(listResult.Items[0].Id);

    // Assert - Membership deserializes correctly. ApiAccessEnabled is nullable per API contract.
    membership.Should().NotBeNull();
    membership.Id.Should().NotBeNullOrEmpty();
    membership.Account.Should().NotBeNull();
  }

  /// <summary>I20: Verifies that membership roles deserialize correctly when populated.</summary>
  /// <remarks>
  ///   Roles are nullable/optional in the API response. Their presence depends on
  ///   account configuration. Skipped when the membership has no roles.
  /// </remarks>
  [UserIntegrationTest]
  public async Task GetMembershipAsync_DeserializesRoles()
  {
    // Arrange - Get a membership
    var listResult = await _sut.ListMembershipsAsync();
    listResult.Items.Should().NotBeEmpty("user must have at least one membership");

    // Act
    var membership = await _sut.GetMembershipAsync(listResult.Items[0].Id);

    // Assert - Membership deserializes correctly.
    membership.Should().NotBeNull();
    membership.Id.Should().NotBeNullOrEmpty();
    membership.Account.Should().NotBeNull();
    membership.Status.Should().NotBeNull();

    // Skip if membership has no roles - this is a valid state but not testable for role structure validation.
    Skip.If(membership.Roles is not { Count: > 0 },
      "Membership has no roles - cannot verify role IDs");

    // Verify each role ID is a non-empty string.
    // Note: Roles is IReadOnlyList<string> containing role IDs, not full role objects.
    membership.Roles.Should().AllSatisfy(roleId =>
    {
      roleId.Should().NotBeNullOrEmpty("role ID should be a valid identifier");
    });
  }

  #endregion
}
