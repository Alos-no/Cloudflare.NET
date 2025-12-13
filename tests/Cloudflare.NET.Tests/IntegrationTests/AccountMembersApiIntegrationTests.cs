namespace Cloudflare.NET.Tests.IntegrationTests;

using Fixtures;
using Members;
using Members.Models;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;


/// <summary>
///   Contains integration tests for the <see cref="MembersApi"/> class.
///   These tests interact with the live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   <para>
///     <b>Important:</b> These tests are READ-ONLY operations. Creating, updating, or
///     deleting members is NOT tested to avoid impacting account membership.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class AccountMembersApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IMembersApi _sut;

  /// <summary>The roles API for fetching valid role IDs in tests.</summary>
  private readonly Cloudflare.NET.Roles.IRolesApi _rolesApi;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AccountMembersApiIntegrationTests"/> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public AccountMembersApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.MembersApi;
    _rolesApi = fixture.RolesApi;
    _settings = TestConfiguration.CloudflareSettings;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region List Members Tests (I01-I05)

  /// <summary>I01: Verifies that members can be listed successfully.</summary>
  [IntegrationTest]
  public async Task ListAccountMembersAsync_ReturnsMembers()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountMembersAsync(accountId);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNullOrEmpty("accounts should have at least one member (the owner)");
    result.PageInfo.Should().NotBeNull();
  }

  /// <summary>I02: Verifies that ListAllAccountMembersAsync iterates through all members.</summary>
  [IntegrationTest]
  public async Task ListAllAccountMembersAsync_CanIterateThroughAllMembers()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var members = new List<AccountMember>();

    await foreach (var member in _sut.ListAllAccountMembersAsync(accountId))
    {
      members.Add(member);

      // Limit to prevent excessive iteration
      if (members.Count >= 100)
        break;
    }

    // Assert
    members.Should().NotBeEmpty("account should have at least one member");
    members.Should().AllSatisfy(m =>
    {
      m.Id.Should().NotBeNullOrEmpty("each member should have an ID");
      m.User.Should().NotBeNull("each member should have a User");
      m.User.Email.Should().NotBeNullOrEmpty("each member should have an email");
    });
  }

  /// <summary>I03: Verifies that members have complete model properties.</summary>
  [IntegrationTest]
  public async Task ListAccountMembersAsync_ReturnsCompleteMemberModels()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountMembersAsync(accountId);

    // Assert - check first member has all expected properties
    result.Items.Should().NotBeEmpty();
    var firstMember = result.Items.First();

    firstMember.Id.Should().NotBeNullOrEmpty("member should have an ID");
    firstMember.Status.Value.Should().NotBeNullOrEmpty("member should have a status");
    firstMember.User.Should().NotBeNull("member should have a user");
    firstMember.User.Id.Should().NotBeNullOrEmpty("user should have an ID");
    firstMember.User.Email.Should().NotBeNullOrEmpty("user should have an email");
    firstMember.Roles.Should().NotBeNull("member should have roles collection");
  }

  /// <summary>I04: Verifies that members list with pagination parameter works correctly.</summary>
  [IntegrationTest]
  public async Task ListAccountMembersAsync_WithPagination_AcceptsRequest()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var filters = new ListAccountMembersFilters(PerPage: 5);

    // Act
    var result = await _sut.ListAccountMembersAsync(accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();
    result.Items.Count.Should().BeLessThanOrEqualTo(5, "API should respect PerPage parameter");
  }

  /// <summary>I05: Verifies that members can be filtered by status.</summary>
  [IntegrationTest]
  public async Task ListAccountMembersAsync_WithStatusFilter_AcceptsRequest()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var filters = new ListAccountMembersFilters(Status: MemberStatus.Accepted);

    // Act
    var result = await _sut.ListAccountMembersAsync(accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();

    // All returned members should have accepted status (if any returned)
    result.Items.Should().AllSatisfy(m =>
      m.Status.Should().Be(MemberStatus.Accepted, "all returned members should have accepted status"));
  }

  #endregion


  #region Get Member Tests (I06-I09)

  /// <summary>I06: Verifies that a specific member can be retrieved by ID.</summary>
  [IntegrationTest]
  public async Task GetAccountMemberAsync_ReturnsMember()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // First, list members to get a valid member ID
    var members = await _sut.ListAccountMembersAsync(accountId);
    members.Items.Should().NotBeEmpty("need at least one member to test GetAccountMemberAsync");
    var memberId = members.Items.First().Id;

    // Act
    var result = await _sut.GetAccountMemberAsync(accountId, memberId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(memberId, "returned member ID should match requested ID");
    result.User.Should().NotBeNull("member should have user information");
    result.User.Email.Should().NotBeNullOrEmpty("user should have an email");
    result.Status.Value.Should().NotBeNullOrEmpty("member should have a status");
    result.Roles.Should().NotBeNull("member should have roles");
  }

  /// <summary>I07: Verifies that member user information is populated correctly.</summary>
  [IntegrationTest]
  public async Task GetAccountMemberAsync_HasUserInfo()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // First, list members to get a valid member ID
    var members = await _sut.ListAccountMembersAsync(accountId);
    members.Items.Should().NotBeEmpty();
    var memberId = members.Items.First().Id;

    // Act
    var result = await _sut.GetAccountMemberAsync(accountId, memberId);

    // Assert
    result.User.Should().NotBeNull("member should have user");
    result.User.Id.Should().NotBeNullOrEmpty("user should have an ID");
    result.User.Email.Should().NotBeNullOrEmpty("user should have an email");
    // FirstName, LastName, TwoFactorAuthenticationEnabled may be null/false
  }

  /// <summary>I08: Verifies that member roles are populated correctly.</summary>
  [IntegrationTest]
  public async Task GetAccountMemberAsync_HasRoles()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // First, list members to get a valid member ID
    var members = await _sut.ListAccountMembersAsync(accountId);
    members.Items.Should().NotBeEmpty();
    var memberId = members.Items.First().Id;

    // Act
    var result = await _sut.GetAccountMemberAsync(accountId, memberId);

    // Assert
    result.Roles.Should().NotBeNull("member should have roles");
    result.Roles.Should().NotBeEmpty("every member should have at least one role");

    // Check role structure
    var firstRole = result.Roles.First();
    firstRole.Id.Should().NotBeNullOrEmpty("role should have an ID");
    firstRole.Name.Should().NotBeNullOrEmpty("role should have a name");
  }

  /// <summary>I09: Verifies that getting a non-existent member returns an error.</summary>
  [IntegrationTest]
  public async Task GetAccountMemberAsync_NonExistentMember_ThrowsError()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentMemberId = "non-existent-member-id-12345";

    // Act & Assert
    var act = async () => await _sut.GetAccountMemberAsync(accountId, nonExistentMemberId);

    // API may return 404 for non-existent member
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().NotBeNull("error should have a status code");
  }

  #endregion


  #region Member Status Tests (I10-I12)

  /// <summary>I10: Verifies that MemberStatus enum values are handled correctly.</summary>
  [IntegrationTest]
  public async Task ListAccountMembersAsync_MemberStatusIsValid()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountMembersAsync(accountId);

    // Assert - check that all members have valid status values
    result.Items.Should().NotBeEmpty();
    result.Items.Should().AllSatisfy(member =>
      member.Status.Value.Should().NotBeNullOrEmpty("each member should have a valid status value"));
  }

  /// <summary>I11: Verifies that the account owner has accepted status.</summary>
  [IntegrationTest]
  public async Task ListAccountMembersAsync_HasAtLeastOneAcceptedMember()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var members = new List<AccountMember>();

    await foreach (var member in _sut.ListAllAccountMembersAsync(accountId))
      members.Add(member);

    // Assert - at least the account owner should have accepted status
    var acceptedMembers = members.Where(m => m.Status == MemberStatus.Accepted).ToList();
    acceptedMembers.Should().NotBeEmpty("the account owner should have accepted status");
  }

  /// <summary>I12: Verifies that list and get return consistent data.</summary>
  [IntegrationTest]
  public async Task GetAccountMemberAsync_ConsistentWithListResults()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var members = await _sut.ListAccountMembersAsync(accountId);
    members.Items.Should().NotBeEmpty();
    var memberFromList = members.Items.First();

    // Act
    var memberFromGet = await _sut.GetAccountMemberAsync(accountId, memberFromList.Id);

    // Assert
    memberFromGet.Id.Should().Be(memberFromList.Id, "IDs should match");
    memberFromGet.Status.Should().Be(memberFromList.Status, "statuses should match");
    memberFromGet.User.Id.Should().Be(memberFromList.User.Id, "user IDs should match");
    memberFromGet.User.Email.Should().Be(memberFromList.User.Email, "emails should match");
  }

  #endregion


  #region CRUD Operations Tests (I15-I22) - Skip Tests for Safety

  /// <summary>
  ///   I15: Verifies that a member can be created (added to account).
  ///   <para>
  ///     <b>SKIPPED:</b> Creating members sends an invitation email to the target email address.
  ///     While there is NO billing cost (verified via Cloudflare docs), and the operation IS
  ///     reversible (members can be deleted), automated tests would send real emails which
  ///     is not appropriate for CI/CD pipelines.
  ///   </para>
  /// </summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/accounts/subresources/members/methods/create/</item>
  ///     <item>No billing implications documented for adding members</item>
  ///     <item>Members can be removed via DELETE /accounts/{id}/members/{member_id}</item>
  ///     <item>Enterprise accounts support "Direct Add" to skip email invitation</item>
  ///   </list>
  ///   <para><b>To enable this test:</b></para>
  ///   <list type="number">
  ///     <item>Use a test email address that can receive the invitation</item>
  ///     <item>Clean up the member after the test via DeleteAccountMemberAsync</item>
  ///     <item>Get role IDs via IRolesApi.ListAccountRolesAsync</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Sends real invitation emails - not straightforward to set up for CI/CD testing")]
  public async Task CreateAccountMemberAsync_WithValidRequest_CreatesMember()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var testEmail = "test-member@example.com"; // Would need a real test email

    // Get available roles first
    var roles = await _rolesApi.ListAccountRolesAsync(accountId);
    var roleId = roles.Items.FirstOrDefault()?.Id ?? throw new InvalidOperationException("No roles available");

    var request = new CreateAccountMemberRequest(
      Email: testEmail,
      Roles: [roleId]
    );

    // Act
    var result = await _sut.CreateAccountMemberAsync(accountId, request);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().NotBeNullOrEmpty();
    result.User.Email.Should().Be(testEmail);
    result.Status.Should().Be(MemberStatus.Pending);
    result.Roles.Should().NotBeEmpty();

    // Cleanup would be required
    // await _sut.DeleteAccountMemberAsync(accountId, result.Id);
  }

  /// <summary>
  ///   I16: Verifies that a member can be updated (roles changed).
  ///   <para>
  ///     <b>SKIPPED:</b> Modifying member roles affects real account permissions. While reversible
  ///     (roles can be changed back), automated tests could temporarily grant/revoke access
  ///     to production resources.
  ///   </para>
  /// </summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/accounts/subresources/members/methods/update/</item>
  ///     <item>No billing implications - role changes are free</item>
  ///     <item>Operation is reversible - roles can be updated again</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Modifies real account permissions - requires setting up the account with a secondary test member")]
  public async Task UpdateAccountMemberAsync_WithValidRequest_UpdatesMember()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var members = await _sut.ListAccountMembersAsync(accountId);
    var testMember = members.Items.FirstOrDefault(m => m.User.Email.Contains("test"));

    // Precondition: Test requires a member with "test" in their email
    testMember.Should().NotBeNull("test requires a member with 'test' in their email to exist");

    // Get available roles
    var roles = await _rolesApi.ListAccountRolesAsync(accountId);
    var newRoleId = roles.Items.FirstOrDefault()?.Id ?? throw new InvalidOperationException("No roles available");

    var request = new UpdateAccountMemberRequest(Roles: [newRoleId]);

    // Act
    var result = await _sut.UpdateAccountMemberAsync(accountId, testMember!.Id, request);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(testMember.Id);
    result.Roles.Should().NotBeEmpty();
  }

  /// <summary>I17: Verifies that updating a non-existent member returns HTTP 404 NotFound.</summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires Global API Key authentication.</b></para>
  ///   <para>
  ///     The Account Members Update endpoint does not support API Token authentication.
  ///     It requires the legacy Global API Key + Email authentication scheme.
  ///   </para>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API docs: https://developers.cloudflare.com/api/resources/accounts/subresources/members/methods/update/</item>
  ///     <item>Security section shows only "api_email + api_key" - no "api_token" option</item>
  ///     <item>Error returned: "PUT method not allowed for the api_token authentication scheme"</item>
  ///   </list>
  ///   <para><b>Expected behavior (inferred):</b> Non-existent member IDs in valid 32-char hex format
  ///   should return 404 NotFound following standard REST conventions.</para>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Global API Key authentication - API Tokens not supported for account member updates")]
  public async Task UpdateAccountMemberAsync_NonExistentMember_ThrowsNotFound()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentMemberId = "00000000000000000000000000000000";

    // Get a valid role ID to ensure we're testing the member ID validation, not role validation
    var roles = await _rolesApi.ListAccountRolesAsync(accountId);
    var validRoleId = roles.Items.FirstOrDefault()?.Id ?? throw new InvalidOperationException("No roles available");
    var request = new UpdateAccountMemberRequest(Roles: [validRoleId]);

    // Act & Assert - Expected 404 NotFound for non-existent member (standard REST)
    var act = async () => await _sut.UpdateAccountMemberAsync(accountId, nonExistentMemberId, request);
    await act.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  /// <summary>I18: Verifies that updating with a malformed member ID returns HTTP 400 BadRequest.</summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires Global API Key authentication.</b></para>
  ///   <para>
  ///     The Account Members Update endpoint does not support API Token authentication.
  ///     It requires the legacy Global API Key + Email authentication scheme.
  ///   </para>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API docs: https://developers.cloudflare.com/api/resources/accounts/subresources/members/methods/update/</item>
  ///     <item>Security section shows only "api_email + api_key" - no "api_token" option</item>
  ///     <item>Error returned: "PUT method not allowed for the api_token authentication scheme"</item>
  ///   </list>
  ///   <para><b>Expected behavior (inferred from I22):</b> Malformed member IDs containing invalid
  ///   characters return 400 BadRequest with error "Validating ID failed: invalid character".</para>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Global API Key authentication - API Tokens not supported for account member updates")]
  public async Task UpdateAccountMemberAsync_MalformedMemberId_ThrowsBadRequest()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var malformedMemberId = "invalid-member-id-format!!!";

    // Get a valid role ID to ensure we're testing the member ID validation, not role validation
    var roles = await _rolesApi.ListAccountRolesAsync(accountId);
    var validRoleId = roles.Items.FirstOrDefault()?.Id ?? throw new InvalidOperationException("No roles available");
    var request = new UpdateAccountMemberRequest(Roles: [validRoleId]);

    // Act & Assert - Expected 400 BadRequest for malformed member ID (consistent with GET behavior)
    var act = async () => await _sut.UpdateAccountMemberAsync(accountId, malformedMemberId, request);
    await act.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.BadRequest);
  }

  /// <summary>
  ///   I19: Verifies that a member can be deleted (removed from account).
  ///   <para>
  ///     <b>SKIPPED:</b> Deleting members removes real users from the account. While the user
  ///     CAN be re-invited (operation is reversible), automated tests could disrupt real
  ///     user access to production accounts.
  ///   </para>
  /// </summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API: https://developers.cloudflare.com/api/resources/accounts/subresources/members/methods/delete/</item>
  ///     <item>No billing implications - member removal is free</item>
  ///     <item>Operation is reversible - user can be re-invited via CreateAccountMemberAsync</item>
  ///     <item>Docs: https://developers.cloudflare.com/fundamentals/manage-members/manage/</item>
  ///   </list>
  /// </remarks>
  [IntegrationTest(Skip = "Removes real user access - requires controlled test environment with disposable member")]
  public async Task DeleteAccountMemberAsync_ExistingMember_DeletesMember()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Would need to create a test member first, or use a known test member
    var members = await _sut.ListAccountMembersAsync(accountId);
    var testMember = members.Items.FirstOrDefault(m => m.User.Email.Contains("test-to-delete"));

    // Precondition: Test requires a member with "test-to-delete" in their email
    testMember.Should().NotBeNull("test requires a member with 'test-to-delete' in their email to exist");

    // Act
    var result = await _sut.DeleteAccountMemberAsync(accountId, testMember!.Id);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(testMember.Id);
  }

  /// <summary>I20: Verifies that deleting a non-existent member returns HTTP 404 NotFound.</summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires Global API Key authentication.</b></para>
  ///   <para>
  ///     The Account Members Delete endpoint does not support API Token authentication.
  ///     It requires the legacy Global API Key + Email authentication scheme.
  ///   </para>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API docs: https://developers.cloudflare.com/api/resources/accounts/subresources/members/methods/delete/</item>
  ///     <item>Security section shows only "api_email + api_key" - no "api_token" option</item>
  ///     <item>Error returned: "DELETE method not allowed for the api_token authentication scheme"</item>
  ///   </list>
  ///   <para><b>Expected behavior (inferred):</b> Non-existent member IDs in valid 32-char hex format
  ///   should return 404 NotFound following standard REST conventions.</para>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Global API Key authentication - API Tokens not supported for account member deletes")]
  public async Task DeleteAccountMemberAsync_NonExistentMember_ThrowsNotFound()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentMemberId = "00000000000000000000000000000000";

    // Act & Assert - Expected 404 NotFound for non-existent member (standard REST)
    var act = async () => await _sut.DeleteAccountMemberAsync(accountId, nonExistentMemberId);
    await act.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
  }

  /// <summary>I21: Verifies that deleting with a malformed member ID returns HTTP 400 BadRequest.</summary>
  /// <remarks>
  ///   <para><b>SKIPPED: Requires Global API Key authentication.</b></para>
  ///   <para>
  ///     The Account Members Delete endpoint does not support API Token authentication.
  ///     It requires the legacy Global API Key + Email authentication scheme.
  ///   </para>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>API docs: https://developers.cloudflare.com/api/resources/accounts/subresources/members/methods/delete/</item>
  ///     <item>Security section shows only "api_email + api_key" - no "api_token" option</item>
  ///     <item>Error returned: "DELETE method not allowed for the api_token authentication scheme"</item>
  ///   </list>
  ///   <para><b>Expected behavior (inferred from I22):</b> Malformed member IDs containing invalid
  ///   characters return 400 BadRequest with error "Validating ID failed: invalid character".</para>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Global API Key authentication - API Tokens not supported for account member deletes")]
  public async Task DeleteAccountMemberAsync_MalformedMemberId_ThrowsBadRequest()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var malformedMemberId = "invalid-member-id-format!!!";

    // Act & Assert - Expected 400 BadRequest for malformed member ID (consistent with GET behavior)
    var act = async () => await _sut.DeleteAccountMemberAsync(accountId, malformedMemberId);
    await act.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.BadRequest);
  }

  /// <summary>I22: Verifies that getting with a malformed member ID returns HTTP 400 BadRequest.</summary>
  /// <remarks>
  ///   Malformed member IDs containing invalid characters return 400 BadRequest with error code 400
  ///   and message "Validating ID '{id}' failed: invalid character '!' in offset {n}".
  /// </remarks>
  [IntegrationTest]
  public async Task GetAccountMemberAsync_MalformedMemberId_ThrowsBadRequest()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var malformedMemberId = "invalid-member-id-format!!!";

    // Act & Assert
    var act = async () => await _sut.GetAccountMemberAsync(accountId, malformedMemberId);
    await act.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == System.Net.HttpStatusCode.BadRequest);
  }

  #endregion


  #region Ordering Tests (I13-I14)

  /// <summary>I13: Verifies that members can be ordered by email.</summary>
  [IntegrationTest]
  public async Task ListAccountMembersAsync_WithOrderByEmail_AcceptsRequest()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var filters = new ListAccountMembersFilters(Order: MemberOrderField.UserEmail);

    // Act
    var result = await _sut.ListAccountMembersAsync(accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();
    // API accepts the order parameter - actual ordering is verified implicitly
  }

  /// <summary>I14: Verifies that members can be ordered with direction.</summary>
  /// <remarks>
  ///   This test requires at least 2 account members to verify ordering behavior.
  /// </remarks>
  [IntegrationTest(Skip = "Requires at least 2 account members to verify ordering behavior")]
  public async Task ListAccountMembersAsync_WithOrderAndDirection_AcceptsRequest()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var ascFilters = new ListAccountMembersFilters(
      Order: MemberOrderField.UserEmail,
      Direction: Cloudflare.NET.Security.Firewall.Models.ListOrderDirection.Ascending
    );
    var descFilters = new ListAccountMembersFilters(
      Order: MemberOrderField.UserEmail,
      Direction: Cloudflare.NET.Security.Firewall.Models.ListOrderDirection.Descending
    );

    // Act
    var ascResult = await _sut.ListAccountMembersAsync(accountId, ascFilters);
    var descResult = await _sut.ListAccountMembersAsync(accountId, descFilters);

    // Assert
    ascResult.Should().NotBeNull();
    descResult.Should().NotBeNull();
    ascResult.Items.Should().HaveCountGreaterThanOrEqualTo(2, "at least 2 account members required to verify ordering");
    descResult.Items.Should().HaveCountGreaterThanOrEqualTo(2, "at least 2 account members required to verify ordering");

    // Verify ascending and descending produce different order
    var ascFirstEmail = ascResult.Items[0].User.Email;
    var descFirstEmail = descResult.Items[0].User.Email;
    ascFirstEmail.Should().NotBe(descFirstEmail, "ascending and descending order should produce different results");
  }

  #endregion
}
