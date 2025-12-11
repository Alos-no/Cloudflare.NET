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
///   Contains integration tests for the User API Tokens methods in <see cref="ApiTokensApi" /> class.
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
public class UserApiTokensIntegrationTests : IClassFixture<CloudflareApiTestFixture>
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

  /// <summary>Initializes a new instance of the <see cref="UserApiTokensIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public UserApiTokensIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.ApiTokensApi;
    _settings = TestConfiguration.CloudflareSettings;
    _output   = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Permission Groups Tests (I18-I20)

  /// <summary>F17-I18: Verifies that user permission groups can be listed successfully.</summary>
  [IntegrationTest]
  public async Task GetUserPermissionGroupsAsync_ReturnsPermissionGroups()
  {
    // Act
    var result = await _sut.GetUserPermissionGroupsAsync();

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().NotBeNullOrEmpty("there should be permission groups available");
  }

  /// <summary>F17-I19: Verifies that GetAllUserPermissionGroupsAsync iterates through all groups.</summary>
  [IntegrationTest]
  public async Task GetAllUserPermissionGroupsAsync_CanIterateThroughAllGroups()
  {
    // Act
    var groups = new List<PermissionGroup>();
    await foreach (var group in _sut.GetAllUserPermissionGroupsAsync())
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

  /// <summary>F17-I20: Verifies that permission groups can be filtered by name.</summary>
  [IntegrationTest]
  public async Task GetUserPermissionGroupsAsync_WithNameFilter_ReturnsFilteredResults()
  {
    // Arrange
    var filters = new ListPermissionGroupsFilters(Name: "Zone");

    // Act
    var result = await _sut.GetUserPermissionGroupsAsync(filters);

    // Assert
    result.Should().NotBeNull();
    if (result.Items.Any())
    {
      result.Items.Should().OnlyContain(g =>
        g.Name.Contains("Zone", StringComparison.OrdinalIgnoreCase) ||
        g.Scopes.Any(s => s.Contains("zone", StringComparison.OrdinalIgnoreCase)));
    }
  }

  #endregion


  #region Token Listing Tests (I01-I04)

  /// <summary>F17-I01: Verifies that user tokens can be listed successfully.</summary>
  [IntegrationTest]
  public async Task ListUserTokensAsync_ReturnsTokens()
  {
    // Act
    var result = await _sut.ListUserTokensAsync();

    // Assert
    result.Should().NotBeNull();
    // There should be at least the token being used for this test
    result.Items.Should().NotBeNullOrEmpty("there should be at least one API token (the one used for testing)");
    result.PageInfo.Should().NotBeNull();
  }

  /// <summary>F17-I02: Verifies that ListAllUserTokensAsync iterates through tokens.</summary>
  [IntegrationTest]
  public async Task ListAllUserTokensAsync_CanIterateThroughTokens()
  {
    // Act
    var tokens = new List<ApiToken>();
    await foreach (var token in _sut.ListAllUserTokensAsync())
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

  /// <summary>F17-I03: Verifies token listing with pagination.</summary>
  [IntegrationTest]
  public async Task ListUserTokensAsync_WithPagination_ReturnsPaginatedResults()
  {
    // Arrange
    var filters = new ListApiTokensFilters(PerPage: 5);

    // Act
    var result = await _sut.ListUserTokensAsync(filters);

    // Assert
    result.Should().NotBeNull();
    result.Items.Should().HaveCountLessThanOrEqualTo(5);
    result.PageInfo.Should().NotBeNull();
    result.PageInfo!.PerPage.Should().Be(5);
  }

  /// <summary>F17-I04: Verifies that token listing returns complete token models.</summary>
  [IntegrationTest]
  public async Task ListUserTokensAsync_ReturnsCompleteModels()
  {
    // Act
    var result = await _sut.ListUserTokensAsync();

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

  /// <summary>F17-I05: Verifies that GetUserTokenAsync returns 404 for non-existent token.</summary>
  [IntegrationTest]
  public async Task GetUserTokenAsync_WhenTokenNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var nonExistentTokenId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.GetUserTokenAsync(nonExistentTokenId);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
  }

  #endregion


  #region Token Verification Tests (I14-I15)

  /// <summary>F17-I14: Verifies the verify token endpoint works.</summary>
  [IntegrationTest]
  public async Task VerifyUserTokenAsync_ReturnsVerificationResult()
  {
    // Act
    var result = await _sut.VerifyUserTokenAsync();

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().NotBeNullOrEmpty();
    result.Status.Should().Be(TokenStatus.Active, "the token being used for testing should be active");
  }

  /// <summary>F17-I15: Verifies that VerifyUserTokenAsync returns token ID matching current token.</summary>
  [IntegrationTest]
  public async Task VerifyUserTokenAsync_ReturnsCurrentTokenInfo()
  {
    // Act
    var result = await _sut.VerifyUserTokenAsync();

    // Assert
    result.Should().NotBeNull();
    result.Id.Should().NotBeNullOrEmpty("should return the token ID");
    result.Status.Value.Should().NotBeNullOrEmpty("should return the token status");
    // Note: ExpiresOn and NotBefore may be null if the token has no time restrictions
  }

  #endregion


  #region Token CRUD Tests (I06-I13, I16-I17)

  /// <summary>F17-I06: Verifies that a token can be created and returns the secret value.</summary>
  [IntegrationTest]
  public async Task CreateUserTokenAsync_ReturnsTokenWithValue()
  {
    // Arrange
    var tokenName = $"cfnet-test-user-token-{Guid.NewGuid():N}";

    // Get a permission group to use
    var permGroups = await _sut.GetUserPermissionGroupsAsync();
    var readOnlyGroup = permGroups.Items.FirstOrDefault(g =>
      g.Name.Contains("Read", StringComparison.OrdinalIgnoreCase));

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
              ["com.cloudflare.api.user"] = "*"
            }
          )
        },
        ExpiresOn: TruncateToSeconds(DateTime.UtcNow.AddDays(1))
      );

      var result = await _sut.CreateUserTokenAsync(request);
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
      await CleanupTestTokens();
    }
  }

  /// <summary>F17-I07: Verifies creating a token with policies.</summary>
  [IntegrationTest]
  public async Task CreateUserTokenAsync_WithPolicies_CreatesTokenWithPolicies()
  {
    // Arrange
    var tokenName = $"cfnet-test-policies-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetUserPermissionGroupsAsync();
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
              ["com.cloudflare.api.user"] = "*"
            }
          )
        }
      );

      var result = await _sut.CreateUserTokenAsync(request);
      _createdTokenIds.Add(result.Id);

      // Assert
      result.Should().NotBeNull();
      result.Policies.Should().NotBeEmpty();
      result.Policies[0].Effect.Should().Be("allow");
      result.Policies[0].PermissionGroups.Should().NotBeEmpty();
    }
    finally
    {
      await CleanupTestTokens();
    }
  }

  /// <summary>F17-I08: Verifies creating a token with expiration.</summary>
  [IntegrationTest]
  public async Task CreateUserTokenAsync_WithExpiration_CreatesTokenWithExpiry()
  {
    // Arrange
    var tokenName = $"cfnet-test-expire-{Guid.NewGuid():N}";
    // Note: Truncate to whole seconds - Cloudflare API rejects fractional seconds.
    var expiresOn = TruncateToSeconds(DateTime.UtcNow.AddDays(7));

    var permGroups = await _sut.GetUserPermissionGroupsAsync();
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
              ["com.cloudflare.api.user"] = "*"
            }
          )
        },
        ExpiresOn: expiresOn
      );

      var result = await _sut.CreateUserTokenAsync(request);
      _createdTokenIds.Add(result.Id);

      // Assert
      result.Should().NotBeNull();
      result.ExpiresOn.Should().NotBeNull();
      result.ExpiresOn!.Value.Should().BeCloseTo(expiresOn, TimeSpan.FromMinutes(1));
    }
    finally
    {
      await CleanupTestTokens();
    }
  }

  /// <summary>F17-I09: Verifies creating a token with IP restrictions.</summary>
  [IntegrationTest]
  public async Task CreateUserTokenAsync_WithIpCondition_CreatesTokenWithConditions()
  {
    // Arrange
    var tokenName = $"cfnet-test-ip-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetUserPermissionGroupsAsync();
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
              ["com.cloudflare.api.user"] = "*"
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

      var result = await _sut.CreateUserTokenAsync(request);
      _createdTokenIds.Add(result.Id);

      // Assert
      result.Should().NotBeNull();
      result.Name.Should().Be(tokenName);
      result.Value.Should().NotBeNullOrEmpty();

      _output.WriteLine($"Created token with IP restrictions: {result.Id}");
    }
    finally
    {
      await CleanupTestTokens();
    }
  }

  /// <summary>F17-I10: Verifies that a token can be updated.</summary>
  [IntegrationTest]
  public async Task UpdateUserTokenAsync_CanUpdateTokenName()
  {
    // Arrange
    var originalName = $"cfnet-test-update-{Guid.NewGuid():N}";
    var updatedName = $"cfnet-test-updated-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetUserPermissionGroupsAsync();
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
              ["com.cloudflare.api.user"] = "*"
            }
          )
        }
      );

      var created = await _sut.CreateUserTokenAsync(createRequest);
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
              ["com.cloudflare.api.user"] = "*"
            }
          )
        }
      );

      var updated = await _sut.UpdateUserTokenAsync(created.Id, updateRequest);

      // Assert
      updated.Should().NotBeNull();
      updated.Id.Should().Be(created.Id);
      updated.Name.Should().Be(updatedName);
    }
    finally
    {
      await CleanupTestTokens();
    }
  }

  /// <summary>F17-I11: Verifies that a token can be disabled via update.</summary>
  [IntegrationTest]
  public async Task UpdateUserTokenAsync_CanDisableToken()
  {
    // Arrange
    var tokenName = $"cfnet-test-disable-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetUserPermissionGroupsAsync();
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
              ["com.cloudflare.api.user"] = "*"
            }
          )
        }
      );

      var created = await _sut.CreateUserTokenAsync(createRequest);
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
              ["com.cloudflare.api.user"] = "*"
            }
          )
        },
        Status: TokenStatus.Disabled
      );

      var updated = await _sut.UpdateUserTokenAsync(created.Id, updateRequest);

      // Assert
      updated.Status.Should().Be(TokenStatus.Disabled);
    }
    finally
    {
      await CleanupTestTokens();
    }
  }

  /// <summary>F17-I12: Verifies that token policies can be updated.</summary>
  [IntegrationTest]
  public async Task UpdateUserTokenAsync_CanUpdatePolicies()
  {
    // Arrange
    var tokenName = $"cfnet-test-policy-update-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetUserPermissionGroupsAsync();
    var groups = permGroups.Items.Take(2).ToList();

    if (groups.Count < 1)
    {
      _output.WriteLine("[SKIP] Not enough permission groups available.");
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
            new[] { new TokenPermissionGroupReference(groups[0].Id) },
            new Dictionary<string, string>
            {
              ["com.cloudflare.api.user"] = "*"
            }
          )
        }
      );

      var created = await _sut.CreateUserTokenAsync(createRequest);
      _createdTokenIds.Add(created.Id);

      // Act - Update with different permission group (if available)
      var newGroup = groups.Count > 1 ? groups[1] : groups[0];
      var updateRequest = new UpdateApiTokenRequest(
        tokenName,
        new[]
        {
          new CreateTokenPolicyRequest(
            "allow",
            new[] { new TokenPermissionGroupReference(newGroup.Id) },
            new Dictionary<string, string>
            {
              ["com.cloudflare.api.user"] = "*"
            }
          )
        }
      );

      var updated = await _sut.UpdateUserTokenAsync(created.Id, updateRequest);

      // Assert
      updated.Should().NotBeNull();
      updated.Id.Should().Be(created.Id);
      updated.Policies.Should().NotBeEmpty();
    }
    finally
    {
      await CleanupTestTokens();
    }
  }

  /// <summary>F17-I13: Verifies that a token can be deleted.</summary>
  [IntegrationTest]
  public async Task DeleteUserTokenAsync_DeletesToken()
  {
    // Arrange
    var tokenName = $"cfnet-test-delete-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetUserPermissionGroupsAsync();
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
            ["com.cloudflare.api.user"] = "*"
          }
        )
      }
    );

    var created = await _sut.CreateUserTokenAsync(createRequest);

    // Act
    await _sut.DeleteUserTokenAsync(created.Id);

    // Assert - Trying to get the deleted token should throw
    var action = async () => await _sut.GetUserTokenAsync(created.Id);
    await action.Should().ThrowAsync<HttpRequestException>()
      .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
  }

  /// <summary>F17-I16: Verifies that token rolling generates a new secret.</summary>
  [IntegrationTest]
  public async Task RollUserTokenAsync_GeneratesNewValue()
  {
    // Arrange
    var tokenName = $"cfnet-test-roll-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetUserPermissionGroupsAsync();
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
              ["com.cloudflare.api.user"] = "*"
            }
          )
        }
      );

      var created = await _sut.CreateUserTokenAsync(createRequest);
      _createdTokenIds.Add(created.Id);
      var originalValue = created.Value;

      // Act
      var newValue = await _sut.RollUserTokenAsync(created.Id);

      // Assert
      newValue.Should().NotBeNullOrEmpty();
      newValue.Should().NotBe(originalValue, "rolled token should have a different value");

      _output.WriteLine($"Original value starts with: {originalValue[..Math.Min(10, originalValue.Length)]}...");
      _output.WriteLine($"New value starts with: {newValue[..Math.Min(10, newValue.Length)]}...");
    }
    finally
    {
      await CleanupTestTokens();
    }
  }

  /// <summary>F17-I17: Verifies that rolling a token returns the new value.</summary>
  [IntegrationTest]
  public async Task RollUserTokenAsync_ReturnsNewTokenString()
  {
    // Arrange
    var tokenName = $"cfnet-test-roll-value-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetUserPermissionGroupsAsync();
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
              ["com.cloudflare.api.user"] = "*"
            }
          )
        }
      );

      var created = await _sut.CreateUserTokenAsync(createRequest);
      _createdTokenIds.Add(created.Id);

      // Act
      var newValue = await _sut.RollUserTokenAsync(created.Id);

      // Assert
      newValue.Should().NotBeNullOrEmpty();
      // Cloudflare tokens are typically 40+ characters
      newValue.Length.Should().BeGreaterThan(20);
    }
    finally
    {
      await CleanupTestTokens();
    }
  }

  #endregion


  #region Full Lifecycle Test (I21)

  /// <summary>F17-I21: Verifies the full token lifecycle: create, get, update, roll, delete.</summary>
  [IntegrationTest]
  public async Task TokenLifecycle_CreateGetUpdateRollDelete_Succeeds()
  {
    // Arrange
    var tokenName = $"cfnet-test-lifecycle-{Guid.NewGuid():N}";
    var updatedName = $"cfnet-test-lifecycle-updated-{Guid.NewGuid():N}";

    var permGroups = await _sut.GetUserPermissionGroupsAsync();
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
              ["com.cloudflare.api.user"] = "*"
            }
          )
        },
        ExpiresOn: TruncateToSeconds(DateTime.UtcNow.AddDays(30))
      );

      var created = await _sut.CreateUserTokenAsync(createRequest);
      tokenId = created.Id;
      created.Name.Should().Be(tokenName);
      created.Value.Should().NotBeNullOrEmpty();
      var originalValue = created.Value;

      _output.WriteLine($"Step 1: Created token {tokenId}");

      // 2. Get
      var retrieved = await _sut.GetUserTokenAsync(tokenId);
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
              ["com.cloudflare.api.user"] = "*"
            }
          )
        }
      );

      var updated = await _sut.UpdateUserTokenAsync(tokenId, updateRequest);
      updated.Name.Should().Be(updatedName);

      _output.WriteLine("Step 3: Updated token successfully");

      // 4. Roll
      var newValue = await _sut.RollUserTokenAsync(tokenId);
      newValue.Should().NotBe(originalValue);

      _output.WriteLine("Step 4: Rolled token successfully");

      // 5. Delete
      await _sut.DeleteUserTokenAsync(tokenId);
      tokenId = null; // Mark as deleted so cleanup doesn't try to delete again

      _output.WriteLine("Step 5: Deleted token successfully");

      // 6. Verify deletion
      var action = async () => await _sut.GetUserTokenAsync(created.Id);
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
          await _sut.DeleteUserTokenAsync(tokenId);
        }
        catch
        {
          // Ignore cleanup errors
        }
      }
    }
  }

  #endregion


  #region Error Handling Tests (I22-I26)

  /// <summary>F17-I23: Verifies that DeleteUserTokenAsync handles non-existent token gracefully.</summary>
  [IntegrationTest]
  public async Task DeleteUserTokenAsync_WhenTokenNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var nonExistentTokenId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.DeleteUserTokenAsync(nonExistentTokenId);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
  }

  /// <summary>F17-I24: Verifies that RollUserTokenAsync throws for non-existent token.</summary>
  [IntegrationTest]
  public async Task RollUserTokenAsync_WhenTokenNotFound_ThrowsHttpRequestException()
  {
    // Arrange
    var nonExistentTokenId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.RollUserTokenAsync(nonExistentTokenId);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
  }

  /// <summary>F17-I26: Verifies behavior with invalid token ID format.</summary>
  [IntegrationTest]
  public async Task GetUserTokenAsync_InvalidIdFormat_ThrowsHttpRequestException()
  {
    // Arrange
    var invalidTokenId = "invalid-format-token-id";

    // Act
    var action = async () => await _sut.GetUserTokenAsync(invalidTokenId);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().BeOneOf(
      HttpStatusCode.NotFound,
      HttpStatusCode.BadRequest,
      HttpStatusCode.Forbidden);
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
  private async Task CleanupTestTokens()
  {
    foreach (var tokenId in _createdTokenIds)
    {
      try
      {
        await _sut.DeleteUserTokenAsync(tokenId);
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
