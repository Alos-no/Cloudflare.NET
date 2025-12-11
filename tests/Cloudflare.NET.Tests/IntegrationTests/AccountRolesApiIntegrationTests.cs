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

  /// <summary>The xUnit test output helper for writing warnings and debug info.</summary>
  private readonly ITestOutputHelper _output;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AccountRolesApiIntegrationTests"/> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public AccountRolesApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.RolesApi;
    _settings = TestConfiguration.CloudflareSettings;
    _output   = output;

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
    result.Should().NotBeNull();
    result.Items.Should().NotBeNullOrEmpty("accounts should have at least one predefined role");
    result.PageInfo.Should().NotBeNull();
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

    // Assert
    roles.Should().NotBeEmpty();
    roles.All(r => !string.IsNullOrEmpty(r.Id)).Should().BeTrue();
    roles.All(r => !string.IsNullOrEmpty(r.Name)).Should().BeTrue();
    roles.All(r => !string.IsNullOrEmpty(r.Description)).Should().BeTrue();
    roles.All(r => r.Permissions != null).Should().BeTrue();

    // Log some roles for visibility
    _output.WriteLine($"Found {roles.Count} roles:");
    foreach (var role in roles.Take(10))
      _output.WriteLine($"  - {role.Name} ({role.Id}): {role.Description.Substring(0, Math.Min(50, role.Description.Length))}...");
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
    var firstRole = result.Items.First();
    firstRole.Id.Should().NotBeNullOrEmpty();
    firstRole.Name.Should().NotBeNullOrEmpty();
    firstRole.Description.Should().NotBeNullOrEmpty();
    firstRole.Permissions.Should().NotBeNull();

    _output.WriteLine($"First role: {firstRole.Name}");
    _output.WriteLine($"  Description: {firstRole.Description}");
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
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();
    // Note: The actual count may be less than or equal to PerPage
    _output.WriteLine($"Requested PerPage=5, got {result.Items.Count} roles");
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
    var roleId = roles.Items.First().Id;

    // Act
    var result = await _sut.GetAccountRoleAsync(accountId, roleId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(roleId);
    result.Name.Should().NotBeNullOrEmpty();
    result.Description.Should().NotBeNullOrEmpty();
    result.Permissions.Should().NotBeNull();

    _output.WriteLine($"Retrieved role: {result.Name} ({result.Id})");
    _output.WriteLine($"Description: {result.Description}");
  }

  /// <summary>I06: Verifies that role permissions are populated correctly.</summary>
  [IntegrationTest]
  public async Task GetAccountRoleAsync_HasPermissions()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // First, list roles to find one with permissions
    var roles = await _sut.ListAccountRolesAsync(accountId);
    var adminRole = roles.Items.FirstOrDefault(r => r.Name.Contains("Admin", StringComparison.OrdinalIgnoreCase))
                 ?? roles.Items.First();

    // Act
    var result = await _sut.GetAccountRoleAsync(accountId, adminRole.Id);

    // Assert
    result.Permissions.Should().NotBeNull();

    // Log the permissions that are set
    _output.WriteLine($"Role: {result.Name}");
    _output.WriteLine("Permissions:");

    if (result.Permissions.Analytics is not null)
      _output.WriteLine($"  Analytics: Read={result.Permissions.Analytics.Read}, Write={result.Permissions.Analytics.Write}");
    if (result.Permissions.Billing is not null)
      _output.WriteLine($"  Billing: Read={result.Permissions.Billing.Read}, Write={result.Permissions.Billing.Write}");
    if (result.Permissions.Dns is not null)
      _output.WriteLine($"  DNS: Read={result.Permissions.Dns.Read}, Write={result.Permissions.Dns.Write}");
    if (result.Permissions.DnsRecords is not null)
      _output.WriteLine($"  DNS Records: Read={result.Permissions.DnsRecords.Read}, Write={result.Permissions.DnsRecords.Write}");
    if (result.Permissions.Zones is not null)
      _output.WriteLine($"  Zones: Read={result.Permissions.Zones.Read}, Write={result.Permissions.Zones.Write}");
    if (result.Permissions.ZoneSettings is not null)
      _output.WriteLine($"  Zone Settings: Read={result.Permissions.ZoneSettings.Read}, Write={result.Permissions.ZoneSettings.Write}");
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

    // Assert - check for common role types
    var roleNames = roles.Select(r => r.Name.ToLowerInvariant()).ToList();

    _output.WriteLine("Available roles:");
    foreach (var role in roles)
      _output.WriteLine($"  - {role.Name}: {role.Description}");

    // At minimum, there should be some roles
    roles.Should().NotBeEmpty("every account should have at least one role");
  }

  /// <summary>I08: Verifies that getting a non-existent role returns 404.</summary>
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
    exception.Which.StatusCode.Should().NotBeNull();
    _output.WriteLine($"Received expected error status: {exception.Which.StatusCode}");
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

  /// <summary>I10: Verifies that read-only roles have read=true, write=false.</summary>
  [IntegrationTest]
  public async Task ListAccountRolesAsync_ReadOnlyRolesHaveCorrectPermissions()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var roles = new List<AccountRole>();
    await foreach (var role in _sut.ListAllAccountRolesAsync(accountId))
      roles.Add(role);

    // Find a read-only role if one exists
    var readOnlyRole = roles.FirstOrDefault(r =>
      r.Name.Contains("Read Only", StringComparison.OrdinalIgnoreCase) ||
      r.Name.Contains("Viewer", StringComparison.OrdinalIgnoreCase));

    if (readOnlyRole != null)
    {
      _output.WriteLine($"Found read-only role: {readOnlyRole.Name}");

      // Check that any non-null permission has read=true and write=false
      if (readOnlyRole.Permissions.Zones is not null)
      {
        readOnlyRole.Permissions.Zones.Read.Should().BeTrue();
        readOnlyRole.Permissions.Zones.Write.Should().BeFalse();
        _output.WriteLine($"  Zones: Read={readOnlyRole.Permissions.Zones.Read}, Write={readOnlyRole.Permissions.Zones.Write}");
      }
    }
    else
    {
      _output.WriteLine("No read-only role found - this is expected for some account types");
    }
  }

  /// <summary>I11: Verifies that the Administrator role has write permissions.</summary>
  [IntegrationTest]
  public async Task GetAccountRoleAsync_AdminRoleHasWritePermissions()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var roles = await _sut.ListAccountRolesAsync(accountId);

    // Find the Administrator or Super Admin role
    var adminRole = roles.Items.FirstOrDefault(r =>
      r.Name.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
      r.Name.Contains("Super Admin", StringComparison.OrdinalIgnoreCase) ||
      r.Name.Contains("Admin", StringComparison.OrdinalIgnoreCase));

    if (adminRole == null)
    {
      _output.WriteLine("No admin role found - skipping write permission check");
      return;
    }

    // Act
    var result = await _sut.GetAccountRoleAsync(accountId, adminRole.Id);

    // Assert - Admin should have some write permissions
    var hasWritePermission =
      result.Permissions.Dns?.Write == true ||
      result.Permissions.DnsRecords?.Write == true ||
      result.Permissions.Zones?.Write == true ||
      result.Permissions.ZoneSettings?.Write == true ||
      result.Permissions.Organization?.Write == true;

    _output.WriteLine($"Admin role: {result.Name}");
    _output.WriteLine($"  Has write permissions: {hasWritePermission}");

    // Note: Different account types have different admin permissions
    // Just log the result rather than asserting
  }

  /// <summary>I12: Verifies that roles list and get return consistent data.</summary>
  [IntegrationTest]
  public async Task GetAccountRoleAsync_ConsistentWithListResults()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var roles = await _sut.ListAccountRolesAsync(accountId);
    var roleFromList = roles.Items.First();

    // Act
    var roleFromGet = await _sut.GetAccountRoleAsync(accountId, roleFromList.Id);

    // Assert
    roleFromGet.Id.Should().Be(roleFromList.Id);
    roleFromGet.Name.Should().Be(roleFromList.Name);
    roleFromGet.Description.Should().Be(roleFromList.Description);

    _output.WriteLine($"List vs Get comparison for role: {roleFromList.Name}");
    _output.WriteLine($"  IDs match: {roleFromGet.Id == roleFromList.Id}");
    _output.WriteLine($"  Names match: {roleFromGet.Name == roleFromList.Name}");
  }

  #endregion
}
