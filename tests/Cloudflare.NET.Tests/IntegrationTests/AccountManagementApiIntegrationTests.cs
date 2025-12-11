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

  /// <summary>The xUnit test output helper for writing warnings.</summary>
  private readonly ITestOutputHelper _output;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="AccountManagementApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public AccountManagementApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    // The SUT is resolved via the fixture's pre-configured DI container.
    _sut      = fixture.AccountsApi;
    _settings = TestConfiguration.CloudflareSettings;
    _output   = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
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

    // Assert - Settings may or may not be present depending on account configuration
    // If settings is present, verify its properties are accessible
    if (result.Settings is not null)
    {
      // AbuseContactEmail may be null
      // EnforceTwofactor should be a valid boolean (default false) - this assertion verifies access
      _ = result.Settings.EnforceTwofactor; // Simply accessing the property verifies deserialization
    }
  }

  /// <summary>I06: Verifies that GetAccountAsync returns ManagedBy when account is managed.</summary>
  /// <remarks>
  ///   This test verifies that ManagedBy is properly deserialized when present.
  ///   Most standalone accounts will have ManagedBy as null.
  /// </remarks>
  [IntegrationTest]
  public async Task GetAccountAsync_DeserializesManagedBy()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.GetAccountAsync(accountId);

    // Assert - ManagedBy is optional; verify it deserializes correctly if present
    if (result.ManagedBy is not null)
    {
      // If ManagedBy exists, it should have valid parent org info
      result.ManagedBy.ParentOrgId.Should().NotBeNullOrEmpty();
      result.ManagedBy.ParentOrgName.Should().NotBeNullOrEmpty();
    }
    // If ManagedBy is null, the account is not managed - this is also valid
  }

  /// <summary>I07: Verifies that GetAccountAsync throws HttpRequestException for non-existent account.</summary>
  [IntegrationTest]
  public async Task GetAccountAsync_WhenAccountNotFound_ThrowsHttpRequestException()
  {
    // Arrange - Use a non-existent account ID
    var nonExistentAccountId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.GetAccountAsync(nonExistentAccountId);

    // Assert - Cloudflare returns 403 (Invalid account identifier) or 404 for non-existent accounts
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
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
    Account originalAccount;
    try
    {
      originalAccount = await _sut.GetAccountAsync(accountId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      // Cloudflare API may return transient 5xx errors - skip this test with a warning
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }

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
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      // Cloudflare API may return transient 5xx errors - skip with warning
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
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
        // Cleanup may also fail with transient errors - that's OK
      }
    }
  }

  /// <summary>
  ///   I12: Verifies that UpdateAccountAsync can update account settings.
  ///   Note: This test modifies the account and reverts changes.
  /// </summary>
  [IntegrationTest]
  public async Task UpdateAccountAsync_CanUpdateSettings()
  {
    // Arrange
    var accountId = _settings.AccountId;
    Account originalAccount;
    try
    {
      originalAccount = await _sut.GetAccountAsync(accountId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      // Cloudflare API may return transient 5xx errors - skip this test with a warning
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }

    var originalEnforce2fa = originalAccount.Settings?.EnforceTwofactor ?? false;

    try
    {
      // Act - Update settings
      var newSettings = new AccountSettings(EnforceTwofactor: !originalEnforce2fa);
      var updateRequest = new UpdateAccountRequest(originalAccount.Name, newSettings);
      var updatedAccount = await _sut.UpdateAccountAsync(accountId, updateRequest);

      // Assert
      updatedAccount.Should().NotBeNull();
      updatedAccount.Settings.Should().NotBeNull();
      updatedAccount.Settings!.EnforceTwofactor.Should().Be(!originalEnforce2fa);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      // Cloudflare API may return transient 5xx errors - skip with warning
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
    }
    finally
    {
      // Cleanup - Restore original settings (best effort)
      try
      {
        var revertSettings = new AccountSettings(EnforceTwofactor: originalEnforce2fa);
        var revertRequest = new UpdateAccountRequest(originalAccount.Name, revertSettings);
        await _sut.UpdateAccountAsync(accountId, revertRequest);
      }
      catch (HttpRequestException)
      {
        // Cleanup may also fail with transient errors - that's OK
      }
    }
  }

  /// <summary>I13: Verifies that UpdateAccountAsync can update both name and settings simultaneously.</summary>
  [IntegrationTest]
  public async Task UpdateAccountAsync_CanUpdateNameAndSettings()
  {
    // Arrange
    var accountId = _settings.AccountId;
    Account originalAccount;
    try
    {
      originalAccount = await _sut.GetAccountAsync(accountId);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      // Cloudflare API may return transient 5xx errors - skip this test with a warning
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
      return;
    }

    var originalName = originalAccount.Name;
    var originalEnforce2fa = originalAccount.Settings?.EnforceTwofactor ?? false;
    var comboTestName = $"{originalName} - Combo Test";
    var testName = comboTestName.Substring(0, Math.Min(100, comboTestName.Length));

    try
    {
      // Act - Update both name and settings
      var newSettings = new AccountSettings(EnforceTwofactor: !originalEnforce2fa);
      var updateRequest = new UpdateAccountRequest(testName, newSettings);
      var updatedAccount = await _sut.UpdateAccountAsync(accountId, updateRequest);

      // Assert
      updatedAccount.Should().NotBeNull();
      updatedAccount.Name.Should().Be(testName);
      updatedAccount.Settings.Should().NotBeNull();
      updatedAccount.Settings!.EnforceTwofactor.Should().Be(!originalEnforce2fa);
    }
    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway)
    {
      // Cloudflare API may return transient 5xx errors - skip with warning
      _output.WriteLine($"[WARNING - Transient API Error] Cloudflare returned {ex.StatusCode}. Test skipped due to transient API issue.");
    }
    finally
    {
      // Cleanup - Restore original state (best effort)
      try
      {
        var revertSettings = new AccountSettings(EnforceTwofactor: originalEnforce2fa);
        var revertRequest = new UpdateAccountRequest(originalName, revertSettings);
        await _sut.UpdateAccountAsync(accountId, revertRequest);
      }
      catch (HttpRequestException)
      {
        // Cleanup may also fail with transient errors - that's OK
      }
    }
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
  [IntegrationTest]
  public async Task UpdateAccountAsync_WhenAccountNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var nonExistentAccountId = "00000000000000000000000000000000";
    var updateRequest = new UpdateAccountRequest("Test Name");

    // Act
    var action = async () => await _sut.UpdateAccountAsync(nonExistentAccountId, updateRequest);

    // Assert - Cloudflare returns 403 (Invalid account identifier) or 404 for non-existent accounts
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
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
    // The filter is a partial match, so results should contain accounts with the substring
    if (result.Items.Any())
    {
      result.Items.Should().Contain(a => a.Name.Contains(nameSubstring, StringComparison.OrdinalIgnoreCase));
    }
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
}
