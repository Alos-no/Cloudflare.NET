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

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  /// <summary>The xUnit test output helper for writing warnings and debug info.</summary>
  private readonly ITestOutputHelper _output;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AccountMembersApiIntegrationTests"/> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public AccountMembersApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.MembersApi;
    _settings = TestConfiguration.CloudflareSettings;
    _output   = output;

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

    _output.WriteLine($"Found {result.Items.Count} members in account");
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
    members.Should().NotBeEmpty();
    members.All(m => !string.IsNullOrEmpty(m.Id)).Should().BeTrue();
    members.All(m => m.User != null).Should().BeTrue();
    members.All(m => !string.IsNullOrEmpty(m.User.Email)).Should().BeTrue();

    // Log some members for visibility
    _output.WriteLine($"Found {members.Count} members:");
    foreach (var member in members.Take(10))
      _output.WriteLine($"  - {member.User.Email} ({member.Status})");
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
    var firstMember = result.Items.First();
    firstMember.Id.Should().NotBeNullOrEmpty();
    firstMember.Status.Value.Should().NotBeNullOrEmpty();
    firstMember.User.Should().NotBeNull();
    firstMember.User.Id.Should().NotBeNullOrEmpty();
    firstMember.User.Email.Should().NotBeNullOrEmpty();
    firstMember.Roles.Should().NotBeNull();

    _output.WriteLine($"First member: {firstMember.User.Email}");
    _output.WriteLine($"  Status: {firstMember.Status}");
    _output.WriteLine($"  Roles: {string.Join(", ", firstMember.Roles.Select(r => r.Name))}");
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
    // Note: The actual count may be less than or equal to PerPage
    _output.WriteLine($"Requested PerPage=5, got {result.Items.Count} members");
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

    // All returned members should have accepted status
    if (result.Items.Count > 0)
    {
      result.Items.All(m => m.Status == MemberStatus.Accepted).Should().BeTrue();
      _output.WriteLine($"Found {result.Items.Count} members with status 'accepted'");
    }
    else
    {
      _output.WriteLine("No members found with status 'accepted' (this may be expected)");
    }
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
    var memberId = members.Items.First().Id;

    // Act
    var result = await _sut.GetAccountMemberAsync(accountId, memberId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(memberId);
    result.User.Should().NotBeNull();
    result.User.Email.Should().NotBeNullOrEmpty();
    result.Status.Value.Should().NotBeNullOrEmpty();
    result.Roles.Should().NotBeNull();

    _output.WriteLine($"Retrieved member: {result.User.Email}");
    _output.WriteLine($"Status: {result.Status}");
    _output.WriteLine($"Roles: {string.Join(", ", result.Roles.Select(r => r.Name))}");
  }

  /// <summary>I07: Verifies that member user information is populated correctly.</summary>
  [IntegrationTest]
  public async Task GetAccountMemberAsync_HasUserInfo()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // First, list members to get a valid member ID
    var members = await _sut.ListAccountMembersAsync(accountId);
    var memberId = members.Items.First().Id;

    // Act
    var result = await _sut.GetAccountMemberAsync(accountId, memberId);

    // Assert
    result.User.Should().NotBeNull();
    result.User.Id.Should().NotBeNullOrEmpty();
    result.User.Email.Should().NotBeNullOrEmpty();

    // Log user details
    _output.WriteLine($"User info for member {result.Id}:");
    _output.WriteLine($"  User ID: {result.User.Id}");
    _output.WriteLine($"  Email: {result.User.Email}");
    _output.WriteLine($"  First Name: {result.User.FirstName ?? "(not set)"}");
    _output.WriteLine($"  Last Name: {result.User.LastName ?? "(not set)"}");
    _output.WriteLine($"  2FA Enabled: {result.User.TwoFactorAuthenticationEnabled}");
  }

  /// <summary>I08: Verifies that member roles are populated correctly.</summary>
  [IntegrationTest]
  public async Task GetAccountMemberAsync_HasRoles()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // First, list members to get a valid member ID
    var members = await _sut.ListAccountMembersAsync(accountId);
    var memberId = members.Items.First().Id;

    // Act
    var result = await _sut.GetAccountMemberAsync(accountId, memberId);

    // Assert
    result.Roles.Should().NotBeNull();
    result.Roles.Should().NotBeEmpty("every member should have at least one role");

    // Check role structure
    var firstRole = result.Roles.First();
    firstRole.Id.Should().NotBeNullOrEmpty();
    firstRole.Name.Should().NotBeNullOrEmpty();

    _output.WriteLine($"Roles for member {result.User.Email}:");
    foreach (var role in result.Roles)
      _output.WriteLine($"  - {role.Name} ({role.Id})");
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
    exception.Which.StatusCode.Should().NotBeNull();
    _output.WriteLine($"Received expected error status: {exception.Which.StatusCode}");
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
    foreach (var member in result.Items)
    {
      member.Status.Value.Should().NotBeNullOrEmpty();

      // Check if it's one of the known statuses or a custom one
      var isKnownStatus = member.Status == MemberStatus.Accepted ||
                          member.Status == MemberStatus.Pending ||
                          member.Status == MemberStatus.Rejected;

      _output.WriteLine($"Member {member.User.Email}: Status = {member.Status} (Known: {isKnownStatus})");
    }
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

    _output.WriteLine($"Found {acceptedMembers.Count} members with 'accepted' status:");
    foreach (var member in acceptedMembers)
      _output.WriteLine($"  - {member.User.Email}");
  }

  /// <summary>I12: Verifies that list and get return consistent data.</summary>
  [IntegrationTest]
  public async Task GetAccountMemberAsync_ConsistentWithListResults()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var members = await _sut.ListAccountMembersAsync(accountId);
    var memberFromList = members.Items.First();

    // Act
    var memberFromGet = await _sut.GetAccountMemberAsync(accountId, memberFromList.Id);

    // Assert
    memberFromGet.Id.Should().Be(memberFromList.Id);
    memberFromGet.Status.Should().Be(memberFromList.Status);
    memberFromGet.User.Id.Should().Be(memberFromList.User.Id);
    memberFromGet.User.Email.Should().Be(memberFromList.User.Email);

    _output.WriteLine($"List vs Get comparison for member: {memberFromList.User.Email}");
    _output.WriteLine($"  IDs match: {memberFromGet.Id == memberFromList.Id}");
    _output.WriteLine($"  Statuses match: {memberFromGet.Status == memberFromList.Status}");
    _output.WriteLine($"  Emails match: {memberFromGet.User.Email == memberFromList.User.Email}");
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

    _output.WriteLine($"Members ordered by email ({result.Items.Count} items):");
    foreach (var member in result.Items.Take(5))
      _output.WriteLine($"  - {member.User.Email}");
  }

  /// <summary>I14: Verifies that members can be ordered with direction.</summary>
  [IntegrationTest]
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

    _output.WriteLine("Ascending order emails:");
    foreach (var member in ascResult.Items.Take(3))
      _output.WriteLine($"  {member.User.Email}");

    _output.WriteLine("Descending order emails:");
    foreach (var member in descResult.Items.Take(3))
      _output.WriteLine($"  {member.User.Email}");
  }

  #endregion
}
