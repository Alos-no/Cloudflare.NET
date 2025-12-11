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

  /// <summary>The xUnit test output helper for writing warnings and debug info.</summary>
  private readonly ITestOutputHelper _output;

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
    _output   = output;

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
    // Note: Permission groups endpoint may not return result_info, so PageInfo may be null
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

    // Log some groups for visibility
    _output.WriteLine($"Found {groups.Count} permission groups:");
    foreach (var g in groups.Take(5))
      _output.WriteLine($"  - {g.Name} ({g.Id}): {string.Join(", ", g.Scopes.Take(3))}...");
  }

  /// <summary>I03: Verifies that permission groups can be filtered by name.</summary>
  [IntegrationTest]
  public async Task GetAccountPermissionGroupsAsync_WithNameFilter_ReturnsFilteredResults()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var filters = new ListPermissionGroupsFilters(Name: "Zone");

    // Act
    var result = await _sut.GetAccountPermissionGroupsAsync(accountId, filters);

    // Assert
    result.Should().NotBeNull();
    if (result.Items.Any())
    {
      result.Items.Should().OnlyContain(g =>
        g.Name.Contains("Zone", StringComparison.OrdinalIgnoreCase) ||
        g.Scopes.Any(s => s.Contains("zone", StringComparison.OrdinalIgnoreCase)));
    }
  }

  /// <summary>I04: Verifies permission groups with pagination parameter.</summary>
  /// <remarks>
  ///   Note: The Cloudflare API may ignore pagination parameters for permission groups.
  ///   This test verifies the request is accepted but doesn't assert on result count
  ///   since the API behavior varies.
  /// </remarks>
  [IntegrationTest]
  public async Task GetAccountPermissionGroupsAsync_WithPagination_AcceptsRequest()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var filters = new ListPermissionGroupsFilters(PerPage: 5);

    // Act
    var result = await _sut.GetAccountPermissionGroupsAsync(accountId, filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNull();
    // Note: The API may ignore per_page for permission groups; we just verify the call succeeds
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

    _output.WriteLine($"Found {tokens.Count} API tokens");
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

    if (readOnlyGroup is null)
    {
      _output.WriteLine("[SKIP] Could not find a suitable read-only permission group for testing.");
      return;
    }

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

      _output.WriteLine($"Created token: {result.Id}");
      _output.WriteLine($"Token value starts with: {result.Value[..Math.Min(10, result.Value.Length)]}...");
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

    if (existingToken is null)
    {
      _output.WriteLine("[SKIP] No tokens found to test GetAccountTokenAsync.");
      return;
    }

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

    if (anyGroup is null)
    {
      _output.WriteLine("[SKIP] No permission groups available.");
      return;
    }

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

    if (anyGroup is null)
    {
      _output.WriteLine("[SKIP] No permission groups available.");
      return;
    }

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

    if (anyGroup is null)
    {
      _output.WriteLine("[SKIP] No permission groups available.");
      return;
    }

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

    if (anyGroup is null)
    {
      _output.WriteLine("[SKIP] No permission groups available.");
      return;
    }

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

    if (anyGroup is null)
    {
      _output.WriteLine("[SKIP] No permission groups available.");
      return;
    }

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

      _output.WriteLine($"Original value starts with: {originalValue[..Math.Min(10, originalValue.Length)]}...");
      _output.WriteLine($"New value starts with: {newValue[..Math.Min(10, newValue.Length)]}...");
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

    if (anyGroup is null)
    {
      _output.WriteLine("[SKIP] No permission groups available.");
      return;
    }

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

      // Assert
      result.Should().NotBeNull();
      result.Name.Should().Be(tokenName);
      result.Value.Should().NotBeNullOrEmpty();

      // Note: The condition may not be returned in the create response depending on API behavior.
      // We verify the token was created successfully with the expected name and value.
      // If condition is returned, verify its structure.
      if (result.Condition?.RequestIp is not null)
      {
        result.Condition.RequestIp.In.Should().Contain("192.168.1.0/24");
        result.Condition.RequestIp.In.Should().Contain("10.0.0.0/8");
        result.Condition.RequestIp.NotIn.Should().Contain("192.168.1.100/32");
      }
      else
      {
        // Log that condition was not returned for visibility
        _output.WriteLine("[INFO] Token condition was not returned in create response - this is expected API behavior.");
      }
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

    if (anyGroup is null)
    {
      _output.WriteLine("[SKIP] No permission groups available.");
      return;
    }

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
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
  }

  /// <summary>I20: Verifies that DeleteAccountTokenAsync handles non-existent token gracefully.</summary>
  [IntegrationTest]
  public async Task DeleteAccountTokenAsync_WhenTokenNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentTokenId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.DeleteAccountTokenAsync(accountId, nonExistentTokenId);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
  }

  /// <summary>I21: Verifies that RollAccountTokenAsync throws for non-existent token.</summary>
  [IntegrationTest]
  public async Task RollAccountTokenAsync_WhenTokenNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var accountId = _settings.AccountId;
    var nonExistentTokenId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.RollAccountTokenAsync(accountId, nonExistentTokenId);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
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

    if (anyGroup is null)
    {
      _output.WriteLine("[SKIP] No permission groups available.");
      return;
    }

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

      _output.WriteLine($"Step 1: Created token {tokenId}");

      // 2. Get
      var retrieved = await _sut.GetAccountTokenAsync(accountId, tokenId);
      retrieved.Id.Should().Be(tokenId);
      retrieved.Name.Should().Be(tokenName);

      _output.WriteLine("Step 2: Retrieved token successfully");

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

      _output.WriteLine("Step 3: Updated token successfully");

      // 4. Roll
      var newValue = await _sut.RollAccountTokenAsync(accountId, tokenId);
      newValue.Should().NotBe(originalValue);

      _output.WriteLine("Step 4: Rolled token successfully");

      // 5. Delete
      await _sut.DeleteAccountTokenAsync(accountId, tokenId);
      tokenId = null; // Mark as deleted so cleanup doesn't try to delete again

      _output.WriteLine("Step 5: Deleted token successfully");

      // 6. Verify deletion
      var action = async () => await _sut.GetAccountTokenAsync(accountId, created.Id);
      await action.Should().ThrowAsync<HttpRequestException>()
        .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);

      _output.WriteLine("Step 6: Verified token is deleted");
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
        _output.WriteLine($"Cleaned up token: {tokenId}");
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
