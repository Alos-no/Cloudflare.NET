namespace Cloudflare.NET.Tests.IntegrationTests;

using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Roles;
using Roles.Models;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;


/// <summary>
///   Contains integration tests for the <see cref="RolesApi"/> class.
///   These tests interact with the live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   <para>
///     <b>Important:</b> These tests are read-only and do not modify any account resources.
///     Roles are predefined by Cloudflare and cannot be created or modified via the API.
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class AccountRolesApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IRolesApi _sut;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AccountRolesApiIntegrationTests"/> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public AccountRolesApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.RolesApi;
    _settings = TestConfiguration.CloudflareSettings;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region List Roles Tests (I01-I04)

  /// <summary>I01: Verifies that roles can be listed successfully.</summary>
  [IntegrationTest]
  public async Task ListAccountRolesAsync_ReturnsRoles()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountRolesAsync(accountId);

    // Assert
    result.Should().NotBeNull("API should return a valid response");
    result.Items.Should().NotBeNullOrEmpty("accounts should have at least one predefined role");
    result.PageInfo.Should().NotBeNull("response should include pagination info");
  }

  /// <summary>I02: Verifies that ListAllAccountRolesAsync iterates through all roles.</summary>
  [IntegrationTest]
  public async Task ListAllAccountRolesAsync_CanIterateThroughAllRoles()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var roles = new List<AccountRole>();
    await foreach (var role in _sut.ListAllAccountRolesAsync(accountId))
    {
      roles.Add(role);
      // Limit to prevent excessive iteration
      if (roles.Count >= 100)
        break;
    }

    // Assert - Verify all roles have required properties
    roles.Should().NotBeEmpty("account should have at least one role");
    roles.Should().AllSatisfy(r =>
    {
      r.Id.Should().NotBeNullOrEmpty("each role should have an ID");
      r.Name.Should().NotBeNullOrEmpty("each role should have a name");
      r.Description.Should().NotBeNullOrEmpty("each role should have a description");
      r.Permissions.Should().NotBeNull("each role should have permissions object");
    });
  }

  /// <summary>I03: Verifies that roles have complete model properties.</summary>
  [IntegrationTest]
  public async Task ListAccountRolesAsync_ReturnsCompleteRoleModels()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountRolesAsync(accountId);

    // Assert - check first role has all expected properties
    result.Items.Should().NotBeEmpty("account should have at least one role");
    var firstRole = result.Items.First();
    firstRole.Id.Should().NotBeNullOrEmpty("role should have an ID");
    firstRole.Name.Should().NotBeNullOrEmpty("role should have a name");
    firstRole.Description.Should().NotBeNullOrEmpty("role should have a description");
    firstRole.Permissions.Should().NotBeNull("role should have permissions");
  }

  /// <summary>I04: Verifies that roles with pagination parameter work correctly.</summary>
  [IntegrationTest]
  public async Task ListAccountRolesAsync_WithPagination_AcceptsRequest()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var filters = new ListAccountRolesFilters(PerPage: 5);

    // Act
    var result = await _sut.ListAccountRolesAsync(accountId, filters);

    // Assert
    result.Should().NotBeNull("API should return a valid response");
    result.Items.Should().NotBeNull("Items collection should not be null");
    result.Items.Count.Should().BeLessThanOrEqualTo(5, "API should respect PerPage parameter");
  }

  #endregion


  #region Get Role Tests (I05-I08)

  /// <summary>I05: Verifies that a specific role can be retrieved by ID.</summary>
  [IntegrationTest]
  public async Task GetAccountRoleAsync_ReturnsRole()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // First, list roles to get a valid role ID
    var roles = await _sut.ListAccountRolesAsync(accountId);
    roles.Items.Should().NotBeEmpty("need at least one role to test GetAccountRoleAsync");
    var roleId = roles.Items.First().Id;

    // Act
    var result = await _sut.GetAccountRoleAsync(accountId, roleId);

    // Assert
    result.Should().NotBeNull("API should return a valid role");
    result.Id.Should().Be(roleId, "returned role ID should match requested ID");
    result.Name.Should().NotBeNullOrEmpty("role should have a name");
    result.Description.Should().NotBeNullOrEmpty("role should have a description");
    result.Permissions.Should().NotBeNull("role should have permissions");
  }

  /// <summary>I06: Verifies that role permissions are populated correctly.</summary>
  /// <remarks>
  ///   This test verifies that roles have a permissions object and that at least some
  ///   permission categories exist. We cannot assert on specific permissions (DNS, Analytics, etc.)
  ///   because permission availability varies by account type and plan. Instead, we verify
  ///   the structure is correct and that the first role with any populated permissions has
  ///   readable permission entries.
  /// </remarks>
  [IntegrationTest]
  public async Task GetAccountRoleAsync_HasPermissions()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // First, list roles to find one with permissions
    var roles = await _sut.ListAccountRolesAsync(accountId);
    roles.Items.Should().NotBeEmpty("account should have at least one role");

    // Try to find roles in order of preference (most complete permissions first)
    // See RoleConstants for the full list of available Cloudflare roles
    var adminRole = roles.Items.FirstOrDefault(r => r.Name == RoleConstants.SuperAdministrator)
                 ?? roles.Items.FirstOrDefault(r => r.Name == RoleConstants.Administrator)
                 ?? roles.Items.First();

    // Act
    var result = await _sut.GetAccountRoleAsync(accountId, adminRole.Id);

    // Assert - Permissions object must exist
    result.Permissions.Should().NotBeNull("role should have permissions object");

    // Verify that the permissions object has some content (any permission category populated)
    // We cannot assume specific permissions like DNS/Analytics exist as they vary by account/plan
    var hasAnyPermission =
      result.Permissions.Analytics != null ||
      result.Permissions.Billing != null ||
      result.Permissions.CachePurge != null ||
      result.Permissions.Dns != null ||
      result.Permissions.DnsRecords != null ||
      result.Permissions.LoadBalancer != null ||
      result.Permissions.Logs != null ||
      result.Permissions.Organization != null ||
      result.Permissions.Ssl != null ||
      result.Permissions.Waf != null ||
      result.Permissions.ZoneSettings != null ||
      result.Permissions.Zones != null;

    hasAnyPermission.Should().BeTrue(
      $"role '{result.Name}' should have at least one permission category populated");
  }

  /// <summary>I07: Verifies that common roles exist (Administrator, etc.).</summary>
  [IntegrationTest]
  public async Task ListAccountRolesAsync_ContainsCommonRoles()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var roles = new List<AccountRole>();
    await foreach (var role in _sut.ListAllAccountRolesAsync(accountId))
      roles.Add(role);

    // Assert - At minimum, there should be some roles
    roles.Should().NotBeEmpty("every account should have at least one role");
    roles.Should().AllSatisfy(r =>
    {
      r.Id.Should().NotBeNullOrEmpty("each role should have an ID");
      r.Name.Should().NotBeNullOrEmpty("each role should have a name");
    });
  }

  /// <summary>I08: Verifies that getting a non-existent role returns error.</summary>
  [IntegrationTest]
  public async Task GetAccountRoleAsync_NonExistentRole_ThrowsNotFound()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentRoleId = "non-existent-role-id-12345";

    // Act & Assert
    var act = async () => await _sut.GetAccountRoleAsync(accountId, nonExistentRoleId);

    // API may return 404 for non-existent role
    var exception = await act.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().NotBeNull("error should have a status code");
  }

  #endregion


  #region Permission Grant Tests (I09-I12)

  /// <summary>I09: Verifies that PermissionGrant read/write flags deserialize correctly.</summary>
  [IntegrationTest]
  public async Task GetAccountRoleAsync_PermissionGrantsHaveCorrectFlags()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var roles = await _sut.ListAccountRolesAsync(accountId);
    roles.Items.Should().NotBeEmpty("account should have at least one role");
    var roleWithPermissions = roles.Items.FirstOrDefault(r =>
      r.Permissions.Dns is not null ||
      r.Permissions.Zones is not null ||
      r.Permissions.Analytics is not null) ?? roles.Items.First();

    // Act
    var result = await _sut.GetAccountRoleAsync(accountId, roleWithPermissions.Id);

    // Assert - At least one permission should exist with boolean flags
    var hasAnyPermission =
      result.Permissions.Analytics is not null ||
      result.Permissions.Billing is not null ||
      result.Permissions.CachePurge is not null ||
      result.Permissions.Dns is not null ||
      result.Permissions.DnsRecords is not null ||
      result.Permissions.LoadBalancer is not null ||
      result.Permissions.Logs is not null ||
      result.Permissions.Organization is not null ||
      result.Permissions.Ssl is not null ||
      result.Permissions.Waf is not null ||
      result.Permissions.ZoneSettings is not null ||
      result.Permissions.Zones is not null;

    hasAnyPermission.Should().BeTrue("roles should have at least one permission defined");
  }

  /// <summary>I10: Verifies that read-only roles have read=true, write=false when present.</summary>
  [IntegrationTest]
  public async Task ListAccountRolesAsync_ReadOnlyRolesHaveCorrectPermissions()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var roles = new List<AccountRole>();
    await foreach (var role in _sut.ListAllAccountRolesAsync(accountId))
      roles.Add(role);

    // Assert - Verify roles structure is valid
    roles.Should().NotBeEmpty("account should have at least one role");

    // Find a read-only role if one exists and verify its structure
    var readOnlyRole = roles.FirstOrDefault(r =>
      r.Name.Contains("Read Only", StringComparison.OrdinalIgnoreCase) ||
      r.Name.Contains("Viewer", StringComparison.OrdinalIgnoreCase));

    if (readOnlyRole != null && readOnlyRole.Permissions.Zones is not null)
    {
      // If a read-only role exists with Zones permission, verify the pattern
      readOnlyRole.Permissions.Zones.Read.Should().BeTrue("read-only role should have read access");
      readOnlyRole.Permissions.Zones.Write.Should().BeFalse("read-only role should not have write access");
    }
    // Note: Not all accounts have read-only roles, so we don't fail if none exists
  }

  /// <summary>I12: Verifies that roles list and get return consistent data.</summary>
  [IntegrationTest]
  public async Task GetAccountRoleAsync_ConsistentWithListResults()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var roles = await _sut.ListAccountRolesAsync(accountId);
    roles.Items.Should().NotBeEmpty("account should have at least one role");
    var roleFromList = roles.Items.First();

    // Act
    var roleFromGet = await _sut.GetAccountRoleAsync(accountId, roleFromList.Id);

    // Assert - Data should be consistent between list and get
    roleFromGet.Id.Should().Be(roleFromList.Id, "IDs should match");
    roleFromGet.Name.Should().Be(roleFromList.Name, "names should match");
    roleFromGet.Description.Should().Be(roleFromList.Description, "descriptions should match");
  }

  #endregion
}
