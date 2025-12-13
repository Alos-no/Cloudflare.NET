namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
using ApiTokens;
using ApiTokens.Models;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the <see cref="ApiTokensApi" /> class.
///   These tests interact with the live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   <para>
///     <b>Important:</b> These tests create and delete API tokens on your account.
///     The test token is cleaned up automatically after each test, but if tests fail
///     unexpectedly, you may have leftover test tokens that need manual cleanup.
///   </para>
///   <para>
///     Test tokens are named with a "cfnet-test-" prefix for easy identification.
///     The token used for testing must have permission to manage API tokens
///     (Access: API Tokens Write permission).
///   </para>
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
[Collection(TestCollections.ApiTokens)]
public class ApiTokensApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IApiTokensApi _sut;

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  /// <summary>List of token IDs created during tests that need cleanup.</summary>
  private readonly List<string> _createdTokenIds = new();

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="ApiTokensApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public ApiTokensApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.ApiTokensApi;
    _settings = TestConfiguration.CloudflareSettings;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Permission Groups Tests (I01-I04)

  /// <summary>I01: Verifies that permission groups can be listed successfully.</summary>
  [IntegrationTest]
  public async Task GetAccountPermissionGroupsAsync_ReturnsPermissionGroups()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.GetAccountPermissionGroupsAsync(accountId);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNullOrEmpty("there should be permission groups available");
    result.PageInfo.Should().BeNull("permission groups endpoint does not return result_info");
  }

  /// <summary>I02: Verifies that GetAllAccountPermissionGroupsAsync iterates through all groups.</summary>
  [IntegrationTest]
  public async Task GetAllAccountPermissionGroupsAsync_CanIterateThroughAllGroups()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var groups = new List<PermissionGroup>();
    await foreach (var group in _sut.GetAllAccountPermissionGroupsAsync(accountId))
    {
      groups.Add(group);
      // Limit to prevent excessive iteration
      if (groups.Count >= 100)
        break;
    }

    // Assert
    groups.Should().NotBeEmpty();
    groups.All(g => !string.IsNullOrEmpty(g.Id)).Should().BeTrue();
    groups.All(g => !string.IsNullOrEmpty(g.Name)).Should().BeTrue();
    groups.All(g => g.Scopes != null).Should().BeTrue();
  }

  /// <summary>I03: Verifies that the name filter parameter is broken and returns empty results.</summary>
  /// <remarks>
  ///   <b>Cloudflare API Bug:</b> The 'name' filter is documented but does not work.
  ///   The API returns empty results when a name filter is applied, regardless of filter value.
  ///   <para>
  ///     API documentation at /api/resources/accounts/.../permission_groups/methods/list/ states:
  ///     "name (optional, string) - Filter by the name of the permission group."
  ///     However, the API returns empty results regardless of the filter value.
  ///   </para>
  ///   <para>
  ///     Verified 2024-12-12: Unfiltered request returns 308 groups (12 containing "Zone").
  ///     Request with ?name=Zone returns 0 groups despite documentation claiming filter support.
  ///   </para>
  /// </remarks>
  [CloudflareInternalBug(
    BugDescription = "GET /accounts/{account_id}/tokens/permission_groups?name={filter} returns empty results despite documentation claiming name filter support",
    ReferenceUrl = "https://community.cloudflare.com/t/name-filter-on-permission-groups-endpoint-returns-empty-results/868236")]
  [IntegrationTest]
  public async Task GetAccountPermissionGroupsAsync_WithNameFilter_IsBroken()
  {
    // Arrange - First get all permission groups
    var accountId = _settings.AccountId;
    var allGroups = await _sut.GetAccountPermissionGroupsAsync(accountId);
    allGroups.Items.Should().NotBeEmpty("test requires permission groups to exist");

    // Find a group name containing "Zone" (common across all Cloudflare accounts)
    var zoneGroup = allGroups.Items.FirstOrDefault(g =>
      g.Name.Contains("Zone", StringComparison.OrdinalIgnoreCase));
    zoneGroup.Should().NotBeNull(
      "test requires at least one permission group with 'Zone' in the name");

    // Act - Filter by "Zone" which should return results if filter worked
    var filters = new ListPermissionGroupsFilters(Name: "Zone");
    var result = await _sut.GetAccountPermissionGroupsAsync(accountId, filters);

    // Assert - Document actual API behavior: name filter returns empty results
    // This is a documented API bug - the filter is silently ignored/broken
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();

    // The API returns empty results when name filter is applied (documented bug)
    // If this assertion fails in the future, it means Cloudflare fixed the bug
    result.Items.Should().BeEmpty(
      "the Cloudflare API 'name' filter is documented but silently returns empty results; " +
      "if this test fails, Cloudflare may have fixed the bug and the test should be updated");
  }

  /// <summary>I04: Verifies that the scope filter parameter works correctly.</summary>
  /// <remarks>
  ///   Unlike the 'name' filter (which is broken), the 'scope' filter works correctly.
  ///   This test verifies that filtering by scope (e.g., "com.cloudflare.api.account.zone")
  ///   returns only permission groups that include that scope.
  /// </remarks>
  [IntegrationTest]
  public async Task GetAccountPermissionGroupsAsync_WithScopeFilter_ReturnsFilteredResults()
  {
    // Arrange - Use the zone scope which is common across Cloudflare accounts
    var accountId = _settings.AccountId;
    var zoneScope = "com.cloudflare.api.account.zone";

    // Act - Filter by zone scope
    var filters = new ListPermissionGroupsFilters(Scope: zoneScope);
    var result = await _sut.GetAccountPermissionGroupsAsync(accountId, filters);

    // Also get all groups for comparison
    var allGroups = await _sut.GetAccountPermissionGroupsAsync(accountId);

    // Assert - Results must not be empty and must match the scope filter
    result.Should().NotBeNull();
    result.Items.Should().NotBeEmpty("filtering by zone scope should return results");

    // Verify filtered results are fewer than total (proving filter works)
    result.Items.Should().HaveCountLessThan(allGroups.Items.Count,
      "scope filter should return fewer results than unfiltered request");

    // Verify all returned groups have the zone scope
    result.Items.Should().OnlyContain(g =>
      g.Scopes.Any(s => s.Contains("zone", StringComparison.OrdinalIgnoreCase)),
      "all filtered results must contain 'zone' in their scopes");
  }

  #endregion


  #region Token Listing Tests (I05-I08)

  /// <summary>I05: Verifies that tokens can be listed successfully.</summary>
  [IntegrationTest]
  public async Task ListAccountTokensAsync_ReturnsTokens()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountTokensAsync(accountId);

    // Assert
    result.Should().NotBeNull();
    // There should be at least the token being used for this test
    result.Items.Should().NotBeNullOrEmpty("there should be at least one API token (the one used for testing)");
    result.PageInfo.Should().NotBeNull();
  }

  /// <summary>I06: Verifies that ListAllAccountTokensAsync iterates through tokens.</summary>
  [IntegrationTest]
  public async Task ListAllAccountTokensAsync_CanIterateThroughTokens()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var tokens = new List<ApiToken>();
    await foreach (var token in _sut.ListAllAccountTokensAsync(accountId))
    {
      tokens.Add(token);
      // Limit to prevent excessive iteration
      if (tokens.Count >= 50)
        break;
    }

    // Assert
    tokens.Should().NotBeEmpty();
    tokens.All(t => !string.IsNullOrEmpty(t.Id)).Should().BeTrue();
    tokens.All(t => !string.IsNullOrEmpty(t.Name)).Should().BeTrue();
  }

  /// <summary>I07: Verifies token listing with pagination.</summary>
  [IntegrationTest]
  public async Task ListAccountTokensAsync_WithPagination_ReturnsPaginatedResults()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var filters = new ListApiTokensFilters(PerPage: 5);

    // Act
    var result = await _sut.ListAccountTokensAsync(accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().HaveCountLessThanOrEqualTo(5);
    result.PageInfo.Should().NotBeNull();
    result.PageInfo!.PerPage.Should().Be(5);
  }

  /// <summary>I08: Verifies that token listing returns complete token models.</summary>
  [IntegrationTest]
  public async Task ListAccountTokensAsync_ReturnsCompleteModels()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.ListAccountTokensAsync(accountId);

    // Assert
    var token = result.Items.FirstOrDefault();
    token.Should().NotBeNull("at least one token should exist");

    token!.Id.Should().NotBeNullOrEmpty();
    token.Name.Should().NotBeNullOrEmpty();
    token.Status.Value.Should().NotBeNullOrEmpty();
    token.IssuedOn.Should().NotBe(default);
    token.ModifiedOn.Should().NotBe(default);
    token.Policies.Should().NotBeNull();
  }

  #endregion


  #region Token CRUD Tests (I09-I18)

  /// <summary>I09: Verifies that a token can be created and returns the secret value.</summary>
  [IntegrationTest]
  public async Task CreateAccountTokenAsync_ReturnsTokenWithValue()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var tokenName = $"cfnet-test-token-{Guid.NewGuid():N}";

    // Get a permission group to use
    var permGroups = await _sut.GetAccountPermissionGroupsAsync(accountId);
    var readOnlyGroup = permGroups.Items.FirstOrDefault(g =>
      g.Name.Contains("Read", StringComparison.OrdinalIgnoreCase) &&
      g.Name.Contains("Account", StringComparison.OrdinalIgnoreCase));

    readOnlyGroup.Should().NotBeNull("test requires at least one read-only permission group (Account Read)");

    try
    {
      // Act
      // Note: Truncate ExpiresOn to whole seconds - Cloudflare API rejects fractional seconds.
      var request = new CreateApiTokenRequest(
        tokenName,
        new[]
        {
          new CreateTokenPolicyRequest(
            "allow",
            new[] { new TokenPermissionGroupReference(readOnlyGroup.Id) },
            new Dictionary<string, string>
            {
              [$"com.cloudflare.api.account.{accountId}"] = "*"
            }
          )
        },
        ExpiresOn: TruncateToSeconds(DateTime.UtcNow.AddDays(1))
      );

      var result = await _sut.CreateAccountTokenAsync(accountId, request);
      _createdTokenIds.Add(result.Id);

      // Assert
      result.Should().NotBeNull();
      result.Id.Should().NotBeNullOrEmpty();
      result.Name.Should().Be(tokenName);
      result.Value.Should().NotBeNullOrEmpty("the token secret should be returned on creation");
      result.Status.Should().Be(TokenStatus.Active);
      result.Policies.Should().NotBeEmpty();
    }
    finally
    {
      // Cleanup
      await CleanupTestTokens(accountId);
    }
  }

  /// <summary>I10: Verifies that a specific token can be retrieved by ID.</summary>
  [IntegrationTest]
  public async Task GetAccountTokenAsync_ReturnsTokenDetails()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // First list tokens to get a valid ID
    var listResult = await _sut.ListAccountTokensAsync(accountId);
    var existingToken = listResult.Items.FirstOrDefault();

    existingToken.Should().NotBeNull("test requires at least one existing API token");

    // Act
    var result = await _sut.GetAccountTokenAsync(accountId, existingToken.Id);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().Be(existingToken.Id);
    result.Name.Should().Be(existingToken.Name);
    result.Status.Should().Be(existingToken.Status);
  }

  /// <summary>I11: Verifies that GetAccountTokenAsync returns full token details including policies.</summary>
  [IntegrationTest]
  public async Task GetAccountTokenAsync_ReturnsFullModelWithPolicies()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var tokenName = $"cfnet-test-get-{Guid.NewGuid():N}";

    // Get permission groups
    var permGroups = await _sut.GetAccountPermissionGroupsAsync(accountId);
    var anyGroup = permGroups.Items.FirstOrDefault();

    anyGroup.Should().NotBeNull("test requires at least one permission group");

    try
    {
      // Create a token first
      var createRequest = new CreateApiTokenRequest(
        tokenName,
        new[]
        {
          new CreateTokenPolicyRequest(
            "allow",
            new[] { new TokenPermissionGroupReference(anyGroup.Id) },
            new Dictionary<string, string>
            {
              [$"com.cloudflare.api.account.{accountId}"] = "*"
            }
          )
        }
      );

      var created = await _sut.CreateAccountTokenAsync(accountId, createRequest);
      _createdTokenIds.Add(created.Id);

      // Act
      var result = await _sut.GetAccountTokenAsync(accountId, created.Id);

      // Assert
      result.Should().NotBeNull();
      result.Id.Should().Be(created.Id);
      result.Name.Should().Be(tokenName);
      result.Policies.Should().NotBeEmpty();
      result.Policies[0].Effect.Should().Be("allow");
      result.Policies[0].PermissionGroups.Should().NotBeEmpty();
      result.Policies[0].Resources.Should().NotBeEmpty();
    }
    finally
    {
      await CleanupTestTokens(accountId);
    }
  }

  /// <summary>I12: Verifies that a token can be updated.</summary>
  [IntegrationTest]
  public async Task UpdateAccountTokenAsync_CanUpdateToken()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var originalName = $"cfnet-test-update-{Guid.NewGuid():N}";
    var updatedName = $"cfnet-test-updated-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetAccountPermissionGroupsAsync(accountId);
    var anyGroup = permGroups.Items.FirstOrDefault();

    anyGroup.Should().NotBeNull("test requires at least one permission group");

    try
    {
      // Create a token
      var createRequest = new CreateApiTokenRequest(
        originalName,
        new[]
        {
          new CreateTokenPolicyRequest(
            "allow",
            new[] { new TokenPermissionGroupReference(anyGroup.Id) },
            new Dictionary<string, string>
            {
              [$"com.cloudflare.api.account.{accountId}"] = "*"
            }
          )
        }
      );

      var created = await _sut.CreateAccountTokenAsync(accountId, createRequest);
      _createdTokenIds.Add(created.Id);

      // Act - Update the token name
      var updateRequest = new UpdateApiTokenRequest(
        updatedName,
        new[]
        {
          new CreateTokenPolicyRequest(
            "allow",
            new[] { new TokenPermissionGroupReference(anyGroup.Id) },
            new Dictionary<string, string>
            {
              [$"com.cloudflare.api.account.{accountId}"] = "*"
            }
          )
        }
      );

      var updated = await _sut.UpdateAccountTokenAsync(accountId, created.Id, updateRequest);

      // Assert
      updated.Should().NotBeNull();
      updated.Id.Should().Be(created.Id);
      updated.Name.Should().Be(updatedName);
    }
    finally
    {
      await CleanupTestTokens(accountId);
    }
  }

  /// <summary>I13: Verifies that a token can be disabled via update.</summary>
  [IntegrationTest]
  public async Task UpdateAccountTokenAsync_CanDisableToken()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var tokenName = $"cfnet-test-disable-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetAccountPermissionGroupsAsync(accountId);
    var anyGroup = permGroups.Items.FirstOrDefault();

    anyGroup.Should().NotBeNull("test requires at least one permission group");

    try
    {
      // Create a token
      var createRequest = new CreateApiTokenRequest(
        tokenName,
        new[]
        {
          new CreateTokenPolicyRequest(
            "allow",
            new[] { new TokenPermissionGroupReference(anyGroup.Id) },
            new Dictionary<string, string>
            {
              [$"com.cloudflare.api.account.{accountId}"] = "*"
            }
          )
        }
      );

      var created = await _sut.CreateAccountTokenAsync(accountId, createRequest);
      _createdTokenIds.Add(created.Id);
      created.Status.Should().Be(TokenStatus.Active);

      // Act - Disable the token
      var updateRequest = new UpdateApiTokenRequest(
        tokenName,
        new[]
        {
          new CreateTokenPolicyRequest(
            "allow",
            new[] { new TokenPermissionGroupReference(anyGroup.Id) },
            new Dictionary<string, string>
            {
              [$"com.cloudflare.api.account.{accountId}"] = "*"
            }
          )
        },
        Status: TokenStatus.Disabled
      );

      var updated = await _sut.UpdateAccountTokenAsync(accountId, created.Id, updateRequest);

      // Assert
      updated.Status.Should().Be(TokenStatus.Disabled);
    }
    finally
    {
      await CleanupTestTokens(accountId);
    }
  }

  /// <summary>I14: Verifies that a token can be deleted.</summary>
  [IntegrationTest]
  public async Task DeleteAccountTokenAsync_DeletesToken()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var tokenName = $"cfnet-test-delete-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetAccountPermissionGroupsAsync(accountId);
    var anyGroup = permGroups.Items.FirstOrDefault();

    anyGroup.Should().NotBeNull("test requires at least one permission group");

    // Create a token
    var createRequest = new CreateApiTokenRequest(
      tokenName,
      new[]
      {
        new CreateTokenPolicyRequest(
          "allow",
          new[] { new TokenPermissionGroupReference(anyGroup.Id) },
          new Dictionary<string, string>
          {
            [$"com.cloudflare.api.account.{accountId}"] = "*"
          }
        )
      }
    );

    var created = await _sut.CreateAccountTokenAsync(accountId, createRequest);

    // Act
    await _sut.DeleteAccountTokenAsync(accountId, created.Id);

    // Assert - Trying to get the deleted token should throw
    var action = async () => await _sut.GetAccountTokenAsync(accountId, created.Id);
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  /// <summary>I15: Verifies the verify token endpoint works.</summary>
  [IntegrationTest]
  public async Task VerifyAccountTokenAsync_ReturnsVerificationResult()
  {
    // Arrange
    var accountId = _settings.AccountId;

    // Act
    var result = await _sut.VerifyAccountTokenAsync(accountId);

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().NotBeNullOrEmpty();
    result.Status.Should().Be(TokenStatus.Active, "the token being used for testing should be active");
  }

  /// <summary>I16: Verifies that token rolling generates a new secret.</summary>
  [IntegrationTest]
  public async Task RollAccountTokenAsync_GeneratesNewValue()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var tokenName = $"cfnet-test-roll-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetAccountPermissionGroupsAsync(accountId);
    var anyGroup = permGroups.Items.FirstOrDefault();

    anyGroup.Should().NotBeNull("test requires at least one permission group");

    try
    {
      // Create a token
      var createRequest = new CreateApiTokenRequest(
        tokenName,
        new[]
        {
          new CreateTokenPolicyRequest(
            "allow",
            new[] { new TokenPermissionGroupReference(anyGroup.Id) },
            new Dictionary<string, string>
            {
              [$"com.cloudflare.api.account.{accountId}"] = "*"
            }
          )
        }
      );

      var created = await _sut.CreateAccountTokenAsync(accountId, createRequest);
      _createdTokenIds.Add(created.Id);
      var originalValue = created.Value;

      // Act
      var newValue = await _sut.RollAccountTokenAsync(accountId, created.Id);

      // Assert
      newValue.Should().NotBeNullOrEmpty();
      newValue.Should().NotBe(originalValue, "rolled token should have a different value");
    }
    finally
    {
      await CleanupTestTokens(accountId);
    }
  }

  /// <summary>I17: Verifies creating a token with IP restrictions.</summary>
  [IntegrationTest]
  public async Task CreateAccountTokenAsync_WithIpCondition_CreatesTokenWithConditions()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var tokenName = $"cfnet-test-ip-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetAccountPermissionGroupsAsync(accountId);
    var anyGroup = permGroups.Items.FirstOrDefault();

    anyGroup.Should().NotBeNull("test requires at least one permission group");

    try
    {
      // Act
      var request = new CreateApiTokenRequest(
        tokenName,
        new[]
        {
          new CreateTokenPolicyRequest(
            "allow",
            new[] { new TokenPermissionGroupReference(anyGroup.Id) },
            new Dictionary<string, string>
            {
              [$"com.cloudflare.api.account.{accountId}"] = "*"
            }
          )
        },
        Condition: new TokenCondition(
          new TokenIpCondition(
            In: new[] { "192.168.1.0/24", "10.0.0.0/8" },
            NotIn: new[] { "192.168.1.100/32" }
          )
        )
      );

      var result = await _sut.CreateAccountTokenAsync(accountId, request);
      _createdTokenIds.Add(result.Id);

      // Assert - Basic token creation succeeded
      result.Should().NotBeNull();
      result.Name.Should().Be(tokenName);
      result.Value.Should().NotBeNullOrEmpty();

      // Assert - Verify IP conditions are returned in create response
      result.Condition.Should().NotBeNull("token should have condition in create response");
      result.Condition!.RequestIp.Should().NotBeNull("token should have IP restriction");
      result.Condition.RequestIp!.In.Should().Contain("192.168.1.0/24");
      result.Condition.RequestIp.In.Should().Contain("10.0.0.0/8");
      result.Condition.RequestIp.NotIn.Should().Contain("192.168.1.100/32");

      // Also verify via GET endpoint
      var fetchedToken = await _sut.GetAccountTokenAsync(accountId, result.Id);
      fetchedToken.Should().NotBeNull("token should be retrievable after creation");
      fetchedToken.Condition.Should().NotBeNull("fetched token should have condition");
      fetchedToken.Condition!.RequestIp.Should().NotBeNull("fetched token should have IP restriction");
    }
    finally
    {
      await CleanupTestTokens(accountId);
    }
  }

  /// <summary>I18: Verifies creating a token with expiration.</summary>
  [IntegrationTest]
  public async Task CreateAccountTokenAsync_WithExpiration_CreatesTokenWithExpiry()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var tokenName = $"cfnet-test-expire-{Guid.NewGuid():N}";
    // Note: Truncate to whole seconds - Cloudflare API rejects fractional seconds.
    var expiresOn = TruncateToSeconds(DateTime.UtcNow.AddDays(7));

    var permGroups = await _sut.GetAccountPermissionGroupsAsync(accountId);
    var anyGroup = permGroups.Items.FirstOrDefault();

    anyGroup.Should().NotBeNull("test requires at least one permission group");

    try
    {
      // Act
      var request = new CreateApiTokenRequest(
        tokenName,
        new[]
        {
          new CreateTokenPolicyRequest(
            "allow",
            new[] { new TokenPermissionGroupReference(anyGroup.Id) },
            new Dictionary<string, string>
            {
              [$"com.cloudflare.api.account.{accountId}"] = "*"
            }
          )
        },
        ExpiresOn: expiresOn
      );

      var result = await _sut.CreateAccountTokenAsync(accountId, request);
      _createdTokenIds.Add(result.Id);

      // Assert
      result.Should().NotBeNull();
      result.ExpiresOn.Should().NotBeNull();
      result.ExpiresOn!.Value.Should().BeCloseTo(expiresOn, TimeSpan.FromMinutes(1));
    }
    finally
    {
      await CleanupTestTokens(accountId);
    }
  }

  #endregion


  #region Error Handling Tests (I19-I21)

  /// <summary>I19: Verifies that GetAccountTokenAsync throws 404 for non-existent token.</summary>
  [IntegrationTest]
  public async Task GetAccountTokenAsync_WhenTokenNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentTokenId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.GetAccountTokenAsync(accountId, nonExistentTokenId);

    // Assert
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  /// <summary>I20: Verifies that DeleteAccountTokenAsync returns 404 for non-existent token.</summary>
  [IntegrationTest]
  public async Task DeleteAccountTokenAsync_WhenTokenNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentTokenId = "00000000000000000000000000000000";

    // Act & Assert
    var action = async () => await _sut.DeleteAccountTokenAsync(accountId, nonExistentTokenId);
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  /// <summary>I21: Verifies that RollAccountTokenAsync returns 404 for non-existent token.</summary>
  [IntegrationTest]
  public async Task RollAccountTokenAsync_WhenTokenNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentTokenId = "00000000000000000000000000000000";

    // Act & Assert
    var action = async () => await _sut.RollAccountTokenAsync(accountId, nonExistentTokenId);
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  /// <summary>I23: Verifies that GetAccountTokenAsync with a malformed account ID returns HTTP 400.</summary>
  /// <remarks>
  ///   Malformed account IDs containing special characters that cannot be parsed as valid
  ///   identifiers return 400 BadRequest with error code 7003 "Could not route to..."
  ///   because the request fails at the routing/parsing layer.
  /// </remarks>
  [IntegrationTest]
  public async Task GetAccountTokenAsync_MalformedAccountId_ThrowsBadRequest()
  {
    // Arrange - Use special characters that cause a parsing error (400 BadRequest)
    var malformedAccountId = "!@#$%^&*()";
    // Token ID format is valid (32 hex chars) - actual existence doesn't matter since
    // account ID validation occurs first and will reject the malformed account ID
    var validFormatTokenId = "00000000000000000000000000000000";

    // Act & Assert
    var action = async () => await _sut.GetAccountTokenAsync(malformedAccountId, validFormatTokenId);
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.BadRequest);
  }

  /// <summary>I24: Verifies that GetAccountTokenAsync with a malformed token ID returns 400 Bad Request.</summary>
  [IntegrationTest]
  public async Task GetAccountTokenAsync_MalformedTokenId_ThrowsBadRequest()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var malformedTokenId = "invalid-token-id-format!!!";

    // Act & Assert
    var action = async () => await _sut.GetAccountTokenAsync(accountId, malformedTokenId);
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.BadRequest);
  }

  /// <summary>I25: Verifies that ListAccountTokensAsync with a malformed account ID returns HTTP 400.</summary>
  /// <remarks>
  ///   Malformed account IDs containing special characters that cannot be parsed as valid
  ///   identifiers return 400 BadRequest with error code 7003 "Could not route to..."
  ///   because the request fails at the routing/parsing layer.
  /// </remarks>
  [IntegrationTest]
  public async Task ListAccountTokensAsync_MalformedAccountId_ThrowsBadRequest()
  {
    // Arrange - Use special characters that cause a parsing error (400 BadRequest)
    var malformedAccountId = "!@#$%^&*()";

    // Act & Assert
    var action = async () => await _sut.ListAccountTokensAsync(malformedAccountId);
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.BadRequest);
  }

  #endregion


  #region Full Lifecycle Test (I22)

  /// <summary>I22: Verifies the full token lifecycle: create, get, update, roll, delete.</summary>
  [IntegrationTest]
  public async Task TokenLifecycle_CreateGetUpdateRollDelete_Succeeds()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var tokenName = $"cfnet-test-lifecycle-{Guid.NewGuid():N}";
    var updatedName = $"cfnet-test-lifecycle-updated-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetAccountPermissionGroupsAsync(accountId);
    var anyGroup = permGroups.Items.FirstOrDefault();

    anyGroup.Should().NotBeNull("test requires at least one permission group");

    string? tokenId = null;

    try
    {
      // 1. Create
      // Note: Truncate ExpiresOn to whole seconds - Cloudflare API rejects fractional seconds.
      var createRequest = new CreateApiTokenRequest(
        tokenName,
        new[]
        {
          new CreateTokenPolicyRequest(
            "allow",
            new[] { new TokenPermissionGroupReference(anyGroup.Id) },
            new Dictionary<string, string>
            {
              [$"com.cloudflare.api.account.{accountId}"] = "*"
            }
          )
        },
        ExpiresOn: TruncateToSeconds(DateTime.UtcNow.AddDays(30))
      );

      var created = await _sut.CreateAccountTokenAsync(accountId, createRequest);
      tokenId = created.Id;
      created.Name.Should().Be(tokenName);
      created.Value.Should().NotBeNullOrEmpty();
      var originalValue = created.Value;

      // 2. Get
      var retrieved = await _sut.GetAccountTokenAsync(accountId, tokenId);
      retrieved.Id.Should().Be(tokenId);
      retrieved.Name.Should().Be(tokenName);

      // 3. Update
      var updateRequest = new UpdateApiTokenRequest(
        updatedName,
        new[]
        {
          new CreateTokenPolicyRequest(
            "allow",
            new[] { new TokenPermissionGroupReference(anyGroup.Id) },
            new Dictionary<string, string>
            {
              [$"com.cloudflare.api.account.{accountId}"] = "*"
            }
          )
        }
      );

      var updated = await _sut.UpdateAccountTokenAsync(accountId, tokenId, updateRequest);
      updated.Name.Should().Be(updatedName);

      // 4. Roll
      var newValue = await _sut.RollAccountTokenAsync(accountId, tokenId);
      newValue.Should().NotBe(originalValue);

      // 5. Delete
      await _sut.DeleteAccountTokenAsync(accountId, tokenId);
      tokenId = null; // Mark as deleted so cleanup doesn't try to delete again

      // 6. Verify deletion
      var action = async () => await _sut.GetAccountTokenAsync(accountId, created.Id);
      await action.Should().ThrowAsync<HttpRequestException>()
        .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
    }
    finally
    {
      // Cleanup if something failed before deletion
      if (tokenId != null)
      {
        try
        {
          await _sut.DeleteAccountTokenAsync(accountId, tokenId);
        }
        catch
        {
          // Ignore cleanup errors
        }
      }
    }
  }

  #endregion


  #region Helpers

  /// <summary>
  ///   Truncates a DateTime to whole seconds by removing fractional seconds.
  ///   Cloudflare API rejects DateTime values with milliseconds/microseconds.
  /// </summary>
  /// <param name="dt">The DateTime to truncate.</param>
  /// <returns>A new DateTime with the same value truncated to whole seconds.</returns>
  private static DateTime TruncateToSeconds(DateTime dt) =>
    new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);

  /// <summary>Cleans up test tokens created during tests.</summary>
  private async Task CleanupTestTokens(string accountId)
  {
    foreach (var tokenId in _createdTokenIds)
    {
      try
      {
        await _sut.DeleteAccountTokenAsync(accountId, tokenId);
      }
      catch
      {
        // Ignore cleanup errors
      }
    }

    _createdTokenIds.Clear();
  }

  #endregion
}
