namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
using Accounts;
using Accounts.Models;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the Account Management operations in the <see cref="AccountsApi" /> class.
///   These tests interact with the live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   This test class covers the Account CRUD operations (List, Get, Create, Update, Delete) as opposed to
///   the R2 bucket operations which are tested in <see cref="AccountsApiIntegrationTests" />.
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class AccountManagementApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IAccountsApi _sut;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AccountManagementApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  public AccountManagementApiIntegrationTests(CloudflareApiTestFixture fixture)
  {
    // The SUT is resolved via the fixture's pre-configured DI container.
    _sut      = fixture.AccountsApi;
    _settings = TestConfiguration.CloudflareSettings;
  }

  #endregion


  #region Account Management Integration Tests (F06)

  /// <summary>I01: Verifies that accounts can be listed successfully.</summary>
  [IntegrationTest]
  public async Task ListAccountsAsync_ReturnsAccounts()
  {
    // Act
    var result = await _sut.ListAccountsAsync();

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNullOrEmpty("the authenticated user should have access to at least one account");
    result.PageInfo.Should().NotBeNull();
    result.PageInfo!.TotalCount.Should().BeGreaterThanOrEqualTo(1);
  }

  /// <summary>I02: Verifies that ListAllAccountsAsync iterates through all accounts.</summary>
  [IntegrationTest]
  public async Task ListAllAccountsAsync_CanIterateThroughAllAccounts()
  {
    // Act
    var accounts = new List<Account>();
    await foreach (var account in _sut.ListAllAccountsAsync())
      accounts.Add(account);

    // Assert
    accounts.Should().NotBeEmpty("the authenticated user should have access to at least one account");
    accounts.All(a => !string.IsNullOrEmpty(a.Id)).Should().BeTrue();
    accounts.All(a => !string.IsNullOrEmpty(a.Name)).Should().BeTrue();
  }

  /// <summary>I03: Verifies that a specific account can be retrieved by ID.</summary>
  [IntegrationTest]
  public async Task GetAccountAsync_ReturnsAccountDetails()
  {
    // Arrange - Use the account ID from settings
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.GetAccountAsync(accountId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(accountId);
    result.Name.Should().NotBeNullOrEmpty();
    result.Type.Value.Should().NotBeNullOrEmpty();
    result.CreatedOn.Should().BeBefore(DateTime.UtcNow);
  }

  /// <summary>I04: Verifies that GetAccountAsync returns complete Account model with all properties.</summary>
  [IntegrationTest]
  public async Task GetAccountAsync_ReturnsCompleteModel()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.GetAccountAsync(accountId);

    // Assert - Verify required fields are populated
    result.Id.Should().NotBeNullOrEmpty();
    result.Name.Should().NotBeNullOrEmpty();
    result.Type.Value.Should().NotBeNullOrEmpty();
    result.CreatedOn.Should().NotBe(default(DateTime));

    // AccountType should be one of the known types or a valid unknown type
    var knownTypes = new[] { "standard", "enterprise" };
    if (knownTypes.Contains(result.Type.Value))
    {
      // Known type - verify it matches the static property
      if (result.Type.Value == "standard")
        result.Type.Should().Be(AccountType.Standard);
      else if (result.Type.Value == "enterprise")
        result.Type.Should().Be(AccountType.Enterprise);
    }
    else
    {
      // Unknown type - the extensible enum should preserve the value
      result.Type.Value.Should().NotBeNullOrEmpty();
    }
  }

  /// <summary>I05: Verifies that Account model deserializes settings when present.</summary>
  [IntegrationTest]
  public async Task GetAccountAsync_DeserializesSettings()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.GetAccountAsync(accountId);

    // Assert - Settings should be present to verify deserialization
    result.Settings.Should().NotBeNull("test requires account with settings to validate deserialization");

    // Accessing EnforceTwofactor verifies deserialization worked
    // (it's a boolean so no further assertion needed beyond accessibility)
    _ = result.Settings!.EnforceTwofactor;
  }

  /// <summary>I06: Verifies that GetAccountAsync returns ManagedBy when account is managed.</summary>
  /// <remarks>
  ///   This test verifies that ManagedBy is properly deserialized when present.
  ///   Most standalone accounts will have ManagedBy as null.
  /// </remarks>
  [IntegrationTest(Skip = "Requires a managed account (part of an organization) - test account is standalone")]
  public async Task GetAccountAsync_DeserializesManagedBy()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.GetAccountAsync(accountId);

    // Assert - ManagedBy should be present to verify deserialization
    result.ManagedBy.Should().NotBeNull("test requires a managed account to validate ManagedBy deserialization");

    // If ManagedBy exists, it should have valid parent org info
    result.ManagedBy!.ParentOrgId.Should().NotBeNullOrEmpty();
    result.ManagedBy.ParentOrgName.Should().NotBeNullOrEmpty();
  }

  /// <summary>I07: Verifies that GetAccountAsync throws HttpRequestException for non-existent account.</summary>
  /// <remarks>
  ///   <para>
  ///     Cloudflare returns 403 Forbidden with error code 9109 "Invalid account identifier"
  ///     for non-existent account IDs. This is intentional security behavior to prevent
  ///     account enumeration attacks - by returning the same 403 for both non-existent
  ///     and unauthorized accounts, attackers cannot discover which account IDs are valid.
  ///   </para>
  ///   <para>
  ///     See: https://authress.io/knowledge-base/articles/choosing-the-right-http-error-code-401-403-404
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task GetAccountAsync_WhenAccountNotFound_ThrowsHttpRequestException()
  {
    // Arrange - Use a non-existent account ID
    var nonExistentAccountId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.GetAccountAsync(nonExistentAccountId);

    // Assert - Cloudflare returns 403 to prevent account enumeration (security best practice)
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.Forbidden);
  }

  /// <summary>I08: Verifies pagination works correctly with ListAccountsAsync.</summary>
  [IntegrationTest]
  public async Task ListAccountsAsync_WithPagination_ReturnsPaginatedResults()
  {
    // Arrange - Request a small page size
    var filters = new ListAccountsFilters(PerPage: 5);

    // Act
    var result = await _sut.ListAccountsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.PageInfo.Should().NotBeNull();
    result.PageInfo!.PerPage.Should().Be(5);
    result.PageInfo.Page.Should().BeGreaterThanOrEqualTo(1);

    // Items should not exceed per_page
    result.Items.Should().HaveCountLessThanOrEqualTo(5);
  }

  /// <summary>
  ///   I11: Verifies that UpdateAccountAsync can update account name.
  ///   Note: This test modifies the account and reverts changes.
  /// </summary>
  [IntegrationTest]
  public async Task UpdateAccountAsync_CanUpdateName()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var originalAccount = await _sut.GetAccountAsync(accountId);

    var originalName = originalAccount.Name;
    var updateTestName = $"{originalName} - Test Update {Guid.NewGuid():N}";
    var testName = updateTestName.Substring(0, Math.Min(100, updateTestName.Length));

    try
    {
      // Act - Update with a new name
      var updateRequest = new UpdateAccountRequest(testName);
      var updatedAccount = await _sut.UpdateAccountAsync(accountId, updateRequest);

      // Assert
      updatedAccount.Should().NotBeNull();
      updatedAccount.Name.Should().Be(testName);
      updatedAccount.Id.Should().Be(accountId);
    }
    finally
    {
      // Cleanup - Restore original name (best effort)
      try
      {
        var revertRequest = new UpdateAccountRequest(originalName);
        await _sut.UpdateAccountAsync(accountId, revertRequest);
      }
      catch (HttpRequestException)
      {
        // Cleanup may fail - that's OK, we want the main test to report its result
      }
    }
  }

  /// <summary>I12: Verifies that UpdateAccountAsync with settings parameter is broken and returns 500.</summary>
  /// <remarks>
  ///   <b>Cloudflare API Bug:</b> The 'settings' body parameter is documented but causes a 500 error.
  ///   <para>
  ///     The Cloudflare API documentation for PUT /accounts/{account_id} lists 'settings' as an optional
  ///     body parameter with properties 'abuse_contact_email' and 'enforce_twofactor'. However, when
  ///     sending a valid request with the 'settings' field, the API returns HTTP 500 Internal Server Error
  ///     with error code 500 and message "unhandled server error".
  ///   </para>
  ///   <para>
  ///     Error response: {"success":false,"errors":[{"code":500,"message":"unhandled server error"}],"messages":[],"result":null}
  ///   </para>
  ///   <para>
  ///     API Ref: https://developers.cloudflare.com/api/resources/accounts/methods/update/
  ///   </para>
  /// </remarks>
  [CloudflareInternalBug(
    BugDescription = "PUT /accounts/{account_id} with 'settings' body parameter returns 500 Internal Server Error ('unhandled server error') despite 'settings' being documented as an optional body parameter",
    ReferenceUrl = "https://community.cloudflare.com/t/put-accounts-account-id-returns-500-internal-server-error-with-settings/868211")]
  [IntegrationTest]
  public async Task UpdateAccountAsync_WithSettings_IsBroken()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var originalAccount = await _sut.GetAccountAsync(accountId);

    // Act - Attempt to update settings (documented but broken)
    var newSettings = new AccountSettings(AbuseContactEmail: "thisisatest@email.com");
    var updateRequest = new UpdateAccountRequest(originalAccount.Name, newSettings);
    var action = async () => await _sut.UpdateAccountAsync(accountId, updateRequest);

    // Assert - Document actual API behavior: settings parameter causes 500 Internal Server Error
    // If this assertion fails in the future, it means Cloudflare fixed the bug
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.InternalServerError,
        "the Cloudflare API returns 500 when 'settings' body parameter is sent; " +
        "if this test fails, Cloudflare may have fixed the bug and the test should be updated to verify settings can be updated");
  }

  /// <summary>I13: Verifies that UpdateAccountAsync with name and settings is broken and returns 500.</summary>
  /// <remarks>
  ///   <b>Cloudflare API Bug:</b> Same root cause as UpdateAccountAsync_WithSettings_IsBroken.
  ///   <para>
  ///     The Cloudflare API returns 500 Internal Server Error when sending the documented 'settings'
  ///     body parameter, even when combined with a valid name update.
  ///   </para>
  /// </remarks>
  [CloudflareInternalBug(
    BugDescription = "PUT /accounts/{account_id} with 'settings' body parameter returns 500 Internal Server Error - see UpdateAccountAsync_WithSettings_IsBroken",
    ReferenceUrl = "https://community.cloudflare.com/t/put-accounts-account-id-returns-500-internal-server-error-with-settings/868211")]
  [IntegrationTest]
  public async Task UpdateAccountAsync_WithNameAndSettings_IsBroken()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var originalAccount = await _sut.GetAccountAsync(accountId);
    var comboTestName = $"{originalAccount.Name} - Combo Test";
    var testName = comboTestName.Substring(0, Math.Min(100, comboTestName.Length));

    // Act - Attempt to update both name and settings (settings causes the failure)
    var newSettings = new AccountSettings(AbuseContactEmail: "thisisatest@email.com");
    var updateRequest = new UpdateAccountRequest(testName, newSettings);
    var action = async () => await _sut.UpdateAccountAsync(accountId, updateRequest);

    // Assert - Document actual API behavior: settings parameter causes 500 Internal Server Error
    // If this assertion fails in the future, it means Cloudflare fixed the bug
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.InternalServerError,
        "the Cloudflare API returns 500 when 'settings' body parameter is sent; " +
        "if this test fails, Cloudflare may have fixed the bug and the test should be updated to verify name+settings can be updated");
  }

  /// <summary>I14: Verifies UpdateAccountAsync returns the updated account.</summary>
  [IntegrationTest]
  public async Task UpdateAccountAsync_ReturnsUpdatedAccount()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var originalAccount = await _sut.GetAccountAsync(accountId);
    var originalName = originalAccount.Name;
    // Add a simple suffix that won't change the meaning
    var testName = $"{originalName} ".TrimEnd() + " "; // Just add/remove whitespace

    try
    {
      // Act
      var updateRequest = new UpdateAccountRequest(testName.TrimEnd()); // Trim to keep same name essentially
      var result = await _sut.UpdateAccountAsync(accountId, updateRequest);

      // Assert - The result should be the same account with potentially same name
      result.Should().NotBeNull();
      result.Id.Should().Be(accountId);
      result.Type.Should().Be(originalAccount.Type);
      result.CreatedOn.Should().Be(originalAccount.CreatedOn);
    }
    finally
    {
      // Cleanup - Ensure original name is set
      var revertRequest = new UpdateAccountRequest(originalName);
      await _sut.UpdateAccountAsync(accountId, revertRequest);
    }
  }

  /// <summary>I15: Verifies UpdateAccountAsync throws for invalid account ID.</summary>
  /// <remarks>
  ///   <para>
  ///     Cloudflare returns 403 Forbidden with error code 9109 "Invalid account identifier"
  ///     for non-existent account IDs. This is intentional security behavior to prevent
  ///     account enumeration attacks - by returning the same 403 for both non-existent
  ///     and unauthorized accounts, attackers cannot discover which account IDs are valid.
  ///   </para>
  ///   <para>
  ///     See: https://authress.io/knowledge-base/articles/choosing-the-right-http-error-code-401-403-404
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task UpdateAccountAsync_WhenAccountNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var nonExistentAccountId = "00000000000000000000000000000000";
    var updateRequest = new UpdateAccountRequest("Test Name");

    // Act
    var action = async () => await _sut.UpdateAccountAsync(nonExistentAccountId, updateRequest);

    // Assert - Cloudflare returns 403 to prevent account enumeration (security best practice)
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.Forbidden);
  }

  /// <summary>I16: Verifies ListAccountsAsync with name filter returns filtered results.</summary>
  [IntegrationTest]
  public async Task ListAccountsAsync_WithNameFilter_ReturnsFilteredResults()
  {
    // Arrange - Get an existing account name to use as filter
    var existingAccount = await _sut.GetAccountAsync(_settings.AccountId);
    var nameSubstring = existingAccount.Name.Length > 3
      ? existingAccount.Name.Substring(0, 3)
      : existingAccount.Name;

    var filters = new ListAccountsFilters(Name: nameSubstring);

    // Act
    var result = await _sut.ListAccountsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("filtering by a known account name substring should return results");
    result.Items.Should().Contain(a => a.Name.Contains(nameSubstring, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  ///   I17: Verifies that ListAccountsAsync respects direction filter.
  ///   Note: This test only verifies the request is accepted; actual ordering depends on API behavior.
  /// </summary>
  [IntegrationTest]
  public async Task ListAccountsAsync_WithDirectionFilter_IsAccepted()
  {
    // Arrange
    var filtersAsc = new ListAccountsFilters(Direction: Cloudflare.NET.Security.Firewall.Models.ListOrderDirection.Ascending);
    var filtersDesc = new ListAccountsFilters(Direction: Cloudflare.NET.Security.Firewall.Models.ListOrderDirection.Descending);

    // Act
    var resultAsc = await _sut.ListAccountsAsync(filtersAsc);
    var resultDesc = await _sut.ListAccountsAsync(filtersDesc);

    // Assert - Both requests should succeed
    resultAsc.Should().NotBeNull();
    resultDesc.Should().NotBeNull();
  }

  #endregion


  #region Account Create/Delete Tests (Skipped - Special Permissions Required)

  /// <summary>
  ///   I09: Verifies that a new account can be created successfully.
  ///   This operation creates a sub-account under the parent account.
  /// </summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Tenant API required: https://developers.cloudflare.com/tenant/</item>
  ///     <item>Account creation: https://developers.cloudflare.com/tenant/how-to/manage-accounts/</item>
  ///     <item>API reference: https://developers.cloudflare.com/api/resources/accounts/methods/create/</item>
  ///     <item>Only Tenant admins (Channel/Alliance partners) can create accounts</item>
  ///     <item>Requires signed partner agreement with Cloudflare</item>
  ///   </list>
  ///   <para>
  ///     <b>Prerequisites:</b> To run this test, you need:
  ///     <list type="bullet">
  ///       <item><description>A Tenant admin account (partner agreement required)</description></item>
  ///       <item><description>An API token with Account:Edit permission</description></item>
  ///       <item><description>Provisioning capability enabled for the tenant</description></item>
  ///     </list>
  ///   </para>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Tenant API/Enterprise partner permissions - Account creation")]
  public async Task CreateAccountAsync_ReturnsCreatedAccount()
  {
    // Arrange
    var request = new CreateAccountRequest(
      Name: $"SDK Test Account {Guid.NewGuid():N}");

    // Act
    var result = await _sut.CreateAccountAsync(request);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().NotBeNullOrEmpty();
    result.Name.Should().Be(request.Name);

    // Cleanup - Delete the created account
    // await _sut.DeleteAccountAsync(result.Id);
  }

  /// <summary>
  ///   I10: Verifies that an account can be deleted successfully.
  ///   This operation permanently removes a sub-account.
  /// </summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Tenant API required: https://developers.cloudflare.com/tenant/</item>
  ///     <item>Account deletion: https://developers.cloudflare.com/api/resources/accounts/methods/delete/</item>
  ///     <item>"Delete a specific account is only available for tenant admins at this time"</item>
  ///     <item>Account deletion is IRREVERSIBLE - all zones, DNS, configs, billing lost</item>
  ///   </list>
  ///   <para>
  ///     <b>Prerequisites:</b> Account deletion requires:
  ///     <list type="bullet">
  ///       <item><description>Tenant admin permissions (partner agreement required)</description></item>
  ///       <item><description>A disposable sub-account to delete</description></item>
  ///       <item><description>Account must have no active subscriptions or zones</description></item>
  ///     </list>
  ///   </para>
  /// </remarks>
  [IntegrationTest(Skip = "Requires Tenant API/Enterprise partner permissions - Account deletion is DESTRUCTIVE/IRREVERSIBLE")]
  public async Task DeleteAccountAsync_ReturnsDeleteResult()
  {
    // Arrange
    // First create an account to delete (requires Enterprise permissions)
    // var createRequest = new CreateAccountRequest(Name: "Delete Test Account", Type: AccountType.Standard);
    // var account = await _sut.CreateAccountAsync(createRequest);
    var accountIdToDelete = "account-id-from-created-account";

    // Act
    var result = await _sut.DeleteAccountAsync(accountIdToDelete);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(accountIdToDelete);

    // Verify deletion by attempting to get the account (should fail)
    var getAction = async () => await _sut.GetAccountAsync(accountIdToDelete);
    await getAction.Should().ThrowAsync<HttpRequestException>();
  }

  #endregion
}
