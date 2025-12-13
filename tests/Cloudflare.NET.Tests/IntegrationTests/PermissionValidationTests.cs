namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net.Http.Headers;
using System.Text;
using ApiTokens;
using ApiTokens.Models;
using Cloudflare.NET.Tests.Shared;
using Microsoft.Extensions.Logging;
using Shared.Fixtures;
using Shared.Helpers;
using User;
using Xunit.Abstractions;


/// <summary>
///   Permission validation tests that MUST run before any other integration tests.
///   These tests VALIDATE that the configured API tokens have ALL required permissions
///   to run the full integration test suite. If any permission is missing, these tests
///   FAIL immediately with a clear error message listing what's missing.
/// </summary>
/// <remarks>
///   <para>
///     <b>IMPORTANT:</b> This test class does NOT use CloudflareApiTestFixture or UserApiTestFixture
///     because those fixtures SKIP tests when validation fails. This class must FAIL (not skip)
///     when permissions are missing, so it creates its own API clients directly.
///   </para>
///   <para>
///     This test class is in the <see cref="TestCollections.PermissionValidation" /> collection,
///     which is ordered to run first by <see cref="Shared.IntegrationTestCollectionOrderer" />.
///   </para>
///   <para>
///     <b>Why this exists:</b> Instead of having individual integration tests fail with cryptic
///     403 errors, we validate ALL required permissions upfront. This provides a single, clear
///     failure point that tells administrators exactly which permissions to add.
///   </para>
/// </remarks>
[Collection(TestCollections.PermissionValidation)]
public class PermissionValidationTests : IDisposable
{
  #region Properties & Fields - Non-Public

  /// <summary>The test settings containing AccountId, ZoneId, etc.</summary>
  private readonly TestCloudflareSettings _settings;

  /// <summary>xUnit test output helper for diagnostic messages.</summary>
  private readonly ITestOutputHelper _output;

  /// <summary>HttpClient for account-scoped API calls.</summary>
  private readonly HttpClient _accountHttpClient;

  /// <summary>HttpClient for user-scoped API calls.</summary>
  private readonly HttpClient _userHttpClient;

  /// <summary>API Tokens API for account token validation.</summary>
  private readonly IApiTokensApi _apiTokensApi;

  /// <summary>User API for user token validation.</summary>
  private readonly IUserApi _userApi;

  /// <summary>Logger factory for API clients.</summary>
  private readonly ILoggerFactory _loggerFactory;

  #endregion


  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="PermissionValidationTests" /> class.
  /// </summary>
  /// <remarks>
  ///   This constructor creates its own API clients instead of using fixtures.
  ///   This is intentional: fixtures skip tests when validation fails, but this
  ///   test class must FAIL (not skip) when permissions are missing.
  /// </remarks>
  public PermissionValidationTests(ITestOutputHelper output)
  {
    _settings = TestConfiguration.CloudflareSettings;
    _output   = output;

    // Create a minimal logger factory for the API clients.
    _loggerFactory = LoggerFactory.Create(builder =>
    {
      builder.SetMinimumLevel(LogLevel.Warning);
    });

    // Create HttpClient for account-scoped API (using ApiToken).
    _accountHttpClient = new HttpClient
    {
      BaseAddress = new Uri(_settings.ApiBaseUrl)
    };

    if (!TestConfigurationValidator.IsSecretMissing(_settings.ApiToken))
      _accountHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiToken);

    // Create HttpClient for user-scoped API (using UserApiToken).
    _userHttpClient = new HttpClient
    {
      BaseAddress = new Uri(_settings.ApiBaseUrl)
    };

    if (!TestConfigurationValidator.IsSecretMissing(_settings.UserApiToken))
      _userHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.UserApiToken);

    // Create API clients.
    _apiTokensApi = new ApiTokensApi(_accountHttpClient, _loggerFactory);
    _userApi      = new UserApi(_userHttpClient, _loggerFactory);
  }

  #endregion


  #region IDisposable

  /// <summary>Disposes of the HTTP clients and logger factory.</summary>
  public void Dispose()
  {
    _accountHttpClient.Dispose();
    _userHttpClient.Dispose();
    _loggerFactory.Dispose();
    GC.SuppressFinalize(this);
  }

  #endregion


  #region Account Token Validation

  /// <summary>
  ///   Validates that the CLOUDFLARE_API_TOKEN has ALL required permissions to run
  ///   the full account-scoped integration test suite.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This test retrieves the token's actual permissions via the API and compares
  ///     them against the required permissions list. If ANY permission is missing,
  ///     the test FAILS with a detailed error message.
  ///   </para>
  ///   <para>
  ///     This test uses <see cref="IntegrationTestAttribute"/> which extends
  ///     <see cref="SkippableFactAttribute"/>, but the skip is only for missing
  ///     configuration (secrets). If secrets are present but permissions are missing,
  ///     this test FAILS (not skips).
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task AccountToken_HasAllRequiredPermissions()
  {
    _output.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
    _output.WriteLine("║     VALIDATING CLOUDFLARE_API_TOKEN PERMISSIONS                  ║");
    _output.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
    _output.WriteLine($"AccountId: {_settings.AccountId}");
    _output.WriteLine("");

    // Step 1: Verify token is active
    _output.WriteLine("[Step 1] Verifying token is active...");
    var verifyResult = await _apiTokensApi.VerifyAccountTokenAsync(_settings.AccountId);

    verifyResult.Should().NotBeNull("VerifyAccountTokenAsync should return a result");
    verifyResult.Status.Should().Be(TokenStatus.Active, "token must be active to run tests");
    _output.WriteLine($"  Token Status: {verifyResult.Status} ✓");

    // Step 2: Get token details with policies
    _output.WriteLine("");
    _output.WriteLine("[Step 2] Retrieving token permissions...");

    var token = await _apiTokensApi.GetAccountTokenAsync(_settings.AccountId, verifyResult.Id);
    token.Should().NotBeNull("GetAccountTokenAsync should return token details");

    // Extract all permission group IDs from the token's policies
    var tokenPermissionIds = token.Policies
      .SelectMany(p => p.PermissionGroups)
      .Select(pg => pg.Id)
      .ToHashSet();

    _output.WriteLine($"  Token has {tokenPermissionIds.Count} permission groups assigned");

    // Step 3: Get all available permission groups to map IDs to names
    _output.WriteLine("");
    _output.WriteLine("[Step 3] Resolving permission names...");

    var allGroups = new List<PermissionGroup>();
    await foreach (var group in _apiTokensApi.GetAllAccountPermissionGroupsAsync(_settings.AccountId))
      allGroups.Add(group);

    // Build a set of permission NAMES the token has
    var tokenPermissionNames = tokenPermissionIds
      .Select(id => allGroups.FirstOrDefault(g => g.Id == id)?.Name)
      .Where(name => name is not null)
      .Cast<string>()
      .ToHashSet(StringComparer.OrdinalIgnoreCase);

    _output.WriteLine($"  Resolved {tokenPermissionNames.Count} permission names");

    // Step 4: Validate all required permissions are present
    _output.WriteLine("");
    _output.WriteLine("[Step 4] Validating required permissions...");

    var missingPermissions = new List<string>();
    var presentPermissions = new List<string>();

    foreach (var required in PermissionValidationRunner.RequiredAccountPermissions)
    {
      // Check for exact match or partial match (e.g., "Zone Read" in "Zone Read" or "Zone.Read")
      var hasPermission = tokenPermissionNames.Any(name =>
        name.Equals(required, StringComparison.OrdinalIgnoreCase) ||
        name.Contains(required, StringComparison.OrdinalIgnoreCase));

      if (hasPermission)
      {
        presentPermissions.Add(required);
        _output.WriteLine($"  ✓ {required}");
      }
      else
      {
        missingPermissions.Add(required);
        _output.WriteLine($"  ✗ {required} - MISSING");
      }
    }

    // Step 5: Report results and fail if permissions are missing
    _output.WriteLine("");
    _output.WriteLine("═══════════════════════════════════════════════════════════════════");
    _output.WriteLine($"  Present: {presentPermissions.Count}/{PermissionValidationRunner.RequiredAccountPermissions.Length}");
    _output.WriteLine($"  Missing: {missingPermissions.Count}");

    if (missingPermissions.Count > 0)
    {
      var errorMessage = BuildMissingPermissionsError(
        "CLOUDFLARE_API_TOKEN",
        "account-scoped",
        missingPermissions);

      // Record failure state so subsequent tests SKIP gracefully.
      PermissionValidationState.SetAccountValidationFailed(errorMessage);

      // FAIL the test (not skip) - this is the whole point of this test class.
      Assert.Fail(errorMessage);
    }

    // Record success state so subsequent tests proceed normally.
    PermissionValidationState.SetAccountValidationSucceeded();

    _output.WriteLine("");
    _output.WriteLine("  [PASS] Token has all required permissions for integration tests");
  }

  #endregion


  #region User Token Validation

  /// <summary>
  ///   Validates that the CLOUDFLARE_USER_API_TOKEN has ALL required permissions to run
  ///   the full user-scoped integration test suite.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     User token permissions are validated by attempting to access each required endpoint.
  ///     This is the only reliable way to check user token permissions.
  ///   </para>
  ///   <para>
  ///     <b>Note:</b> Audit Logs is an ACCOUNT-scoped permission, not user-scoped.
  ///     It is validated in <see cref="AccountToken_HasAllRequiredPermissions"/>.
  ///   </para>
  /// </remarks>
  [UserIntegrationTest]
  public async Task UserToken_HasAllRequiredPermissions()
  {
    _output.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
    _output.WriteLine("║     VALIDATING CLOUDFLARE_USER_API_TOKEN PERMISSIONS             ║");
    _output.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
    _output.WriteLine("");

    var missingPermissions = new List<string>();
    var presentPermissions = new List<string>();

    // Test User Details:Read
    _output.WriteLine("[Testing] User Details:Read (GetUserAsync)...");

    try
    {
      var user = await _userApi.GetUserAsync();
      user.Should().NotBeNull();
      presentPermissions.Add("User Details:Read");
      _output.WriteLine("  ✓ User Details:Read - User authenticated successfully");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden)
    {
      missingPermissions.Add("User Details:Read");
      _output.WriteLine("  ✗ User Details:Read - MISSING (403 Forbidden)");
    }

    // Test Memberships:Read
    _output.WriteLine("[Testing] Memberships:Read (ListMembershipsAsync)...");

    try
    {
      var memberships = await _userApi.ListMembershipsAsync();
      memberships.Should().NotBeNull();
      presentPermissions.Add("Memberships:Read");
      _output.WriteLine($"  ✓ Memberships:Read - Found {memberships.Items.Count} memberships");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden)
    {
      missingPermissions.Add("Memberships:Read");
      _output.WriteLine("  ✗ Memberships:Read - MISSING (403 Forbidden)");
    }

    // Test User:Invites:Read
    _output.WriteLine("[Testing] User:Invites:Read (ListInvitationsAsync)...");

    try
    {
      var invitations = await _userApi.ListInvitationsAsync();
      invitations.Should().NotBeNull();
      presentPermissions.Add("User:Invites:Read");
      _output.WriteLine($"  ✓ User:Invites:Read - Found {invitations.Count} invitations");
    }
    catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden)
    {
      missingPermissions.Add("User:Invites:Read");
      _output.WriteLine("  ✗ User:Invites:Read - MISSING (403 Forbidden)");
    }

    // NOTE: Audit Logs is an ACCOUNT-scoped permission, not user-scoped.
    // It is validated in AccountToken_HasAllRequiredPermissions, not here.

    // Report results
    _output.WriteLine("");
    _output.WriteLine("═══════════════════════════════════════════════════════════════════");
    _output.WriteLine($"  Present: {presentPermissions.Count}");
    _output.WriteLine($"  Missing: {missingPermissions.Count}");

    if (missingPermissions.Count > 0)
    {
      var errorMessage = BuildMissingPermissionsError(
        "CLOUDFLARE_USER_API_TOKEN",
        "user-scoped",
        missingPermissions);

      // Record failure state so subsequent tests SKIP gracefully.
      PermissionValidationState.SetUserValidationFailed(errorMessage);

      // FAIL the test (not skip) - this is the whole point of this test class.
      Assert.Fail(errorMessage);
    }

    // Record success state so subsequent tests proceed normally.
    PermissionValidationState.SetUserValidationSucceeded();

    _output.WriteLine("");
    _output.WriteLine("  [PASS] User token has all required permissions for integration tests");
  }

  #endregion


  #region Helper Methods

  /// <summary>
  ///   Builds a comprehensive, actionable error message for missing permissions.
  /// </summary>
  private static string BuildMissingPermissionsError(
    string tokenName,
    string tokenScope,
    List<string> missingPermissions)
  {
    var sb = new StringBuilder();

    sb.AppendLine();
    sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════╗");
    sb.AppendLine("║              INTEGRATION TEST PERMISSION VALIDATION FAILED                   ║");
    sb.AppendLine("╠══════════════════════════════════════════════════════════════════════════════╣");
    sb.AppendLine($"║  Token: {tokenName,-68} ║");
    sb.AppendLine($"║  Scope: {tokenScope,-68} ║");
    sb.AppendLine("╠══════════════════════════════════════════════════════════════════════════════╣");
    sb.AppendLine("║  MISSING PERMISSIONS (add these to your token):                              ║");

    foreach (var permission in missingPermissions)
    {
      var displayPermission = permission.Length > 71 ? permission[..68] + "..." : permission;
      sb.AppendLine($"║    ✗ {displayPermission,-71} ║");
    }

    sb.AppendLine("╠══════════════════════════════════════════════════════════════════════════════╣");
    sb.AppendLine("║  HOW TO FIX:                                                                 ║");
    sb.AppendLine("║                                                                              ║");
    sb.AppendLine("║  1. Go to Cloudflare Dashboard → My Profile → API Tokens                     ║");
    sb.AppendLine("║  2. Edit the existing token or create a new one                              ║");
    sb.AppendLine("║  3. Add ALL the missing permissions listed above                             ║");
    sb.AppendLine("║  4. Update your test configuration:                                          ║");
    sb.AppendLine("║     • User secrets: dotnet user-secrets set \"Cloudflare:ApiToken\" \"<tok>\"    ║");
    sb.AppendLine("║     • Environment: CLOUDFLARE__APITOKEN=<token>                              ║");
    sb.AppendLine("║     • GitHub Actions: Update repository secrets                              ║");
    sb.AppendLine("║                                                                              ║");
    sb.AppendLine("║  For permission reference, see:                                              ║");
    sb.AppendLine("║  https://developers.cloudflare.com/fundamentals/api/reference/permissions/   ║");
    sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");

    return sb.ToString();
  }

  #endregion
}
