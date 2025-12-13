namespace Cloudflare.NET.Tests.Shared;

using System.Text;
using Cloudflare.NET.ApiTokens;
using Cloudflare.NET.ApiTokens.Models;
using Cloudflare.NET.User;

/// <summary>
///   Runs permission validation lazily and thread-safely.
///   <para>
///     This class ensures validation runs exactly once, the first time any fixture
///     requests it, regardless of test parallelization. Results are cached and shared
///     via <see cref="PermissionValidationState" />.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     <b>Why this exists:</b> xUnit runs test collections in parallel by default.
///     We cannot guarantee <c>PermissionValidationTests</c> runs before other test
///     fixtures are created. By moving validation into the fixtures themselves with
///     lazy initialization, we ensure validation happens exactly once before any
///     test that needs it runs.
///   </para>
/// </remarks>
public static class PermissionValidationRunner
{
  #region Constants

  /// <summary>
  ///   Permission group names required for the CLOUDFLARE_API_TOKEN to run all account-scoped tests.
  ///   These must match the exact names returned by GetAccountPermissionGroupsAsync.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Permission names are documented at:
  ///     https://developers.cloudflare.com/fundamentals/api/reference/permissions/
  ///   </para>
  ///   <para>
  ///     To get the current list of permission names for your account, call:
  ///     GET /accounts/{account_id}/tokens/permission_groups
  ///   </para>
  /// </remarks>
  public static readonly string[] RequiredAccountPermissions =
  [
    // IAccountsApi - Account details and IRolesApi - Account roles
    "Account Settings Read",

    // IMembersApi - Account members (read operations use Account Settings Read, write operations need Write)
    "Account Settings Write",

    // IZonesApi - Zone management
    "Zone Read",
    "Zone Settings Read",
    "Zone Settings Write",

    // IDnsApi - DNS records
    "DNS Read",
    "DNS Write",

    // IAuditLogsApi - Audit logs
    "Audit Logs Read",

    // IApiTokensApi - Token management
    "API Tokens Read",
    "API Tokens Write",

    // ISubscriptionsApi - Billing/subscriptions
    "Billing Read",

    // IWorkersApi - Worker routes
    "Workers Routes Read",
    "Workers Routes Write",

    // ITurnstileApi - Turnstile widgets
    // API permission names use "Turnstile Sites" prefix (not just "Turnstile")
    "Turnstile Sites Read",
    "Turnstile Sites Write"
  ];

  #endregion


  #region Properties & Fields - Non-Public

  /// <summary>Lazy task for account token validation. Runs exactly once.</summary>
  private static readonly Lazy<Task> AccountValidationTask = new(
    RunAccountValidationAsync,
    LazyThreadSafetyMode.ExecutionAndPublication);

  /// <summary>Lazy task for user token validation. Runs exactly once.</summary>
  private static readonly Lazy<Task> UserValidationTask = new(
    RunUserValidationAsync,
    LazyThreadSafetyMode.ExecutionAndPublication);

  /// <summary>Cached API tokens API instance for validation.</summary>
  private static IApiTokensApi? _apiTokensApi;

  /// <summary>Cached User API instance for validation.</summary>
  private static IUserApi? _userApi;

  /// <summary>Cached account ID for validation.</summary>
  private static string? _accountId;

  #endregion


  #region Public Methods

  /// <summary>
  ///   Initializes the runner with required API instances.
  ///   Call this before <see cref="EnsureAccountValidationAsync" />.
  /// </summary>
  /// <param name="apiTokensApi">The API tokens API for account validation.</param>
  /// <param name="accountId">The account ID to validate against.</param>
  public static void InitializeAccountValidation(IApiTokensApi apiTokensApi, string accountId)
  {
    _apiTokensApi ??= apiTokensApi;
    _accountId ??= accountId;
  }

  /// <summary>
  ///   Initializes the runner with required API instances for user validation.
  ///   Call this before <see cref="EnsureUserValidationAsync" />.
  /// </summary>
  /// <param name="userApi">The User API for user validation.</param>
  public static void InitializeUserValidation(IUserApi userApi)
  {
    _userApi ??= userApi;
  }

  /// <summary>
  ///   Ensures account token validation has run. If validation fails, sets state
  ///   so subsequent calls to <see cref="PermissionValidationState.EnsureAccountPermissionsValidated" />
  ///   will skip tests.
  /// </summary>
  /// <returns>A task that completes when validation is done.</returns>
  /// <remarks>
  ///   This method is thread-safe. Multiple concurrent calls will wait for the
  ///   single validation task to complete. Subsequent calls return immediately.
  /// </remarks>
  public static async Task EnsureAccountValidationAsync()
  {
    // If already validated (success or failure), return immediately.
    if (PermissionValidationState.AccountValidationCompleted)
      return;

    // If not initialized, we can't validate. Proceed without validation.
    if (_apiTokensApi is null || string.IsNullOrEmpty(_accountId))
      return;

    await AccountValidationTask.Value;
  }

  /// <summary>
  ///   Ensures user token validation has run. If validation fails, sets state
  ///   so subsequent calls to <see cref="PermissionValidationState.EnsureUserPermissionsValidated" />
  ///   will skip tests.
  /// </summary>
  /// <returns>A task that completes when validation is done.</returns>
  public static async Task EnsureUserValidationAsync()
  {
    // If already validated (success or failure), return immediately.
    if (PermissionValidationState.UserValidationCompleted)
      return;

    // If not initialized, we can't validate. Proceed without validation.
    if (_userApi is null)
      return;

    await UserValidationTask.Value;
  }

  #endregion


  #region Private Methods

  /// <summary>
  ///   Runs account token validation. Called exactly once via lazy initialization.
  /// </summary>
  private static async Task RunAccountValidationAsync()
  {
    if (_apiTokensApi is null || string.IsNullOrEmpty(_accountId))
    {
      // Can't validate without API - assume success (tests will fail individually if permissions missing).
      return;
    }

    try
    {
      // Step 1: Verify token is active
      var verifyResult = await _apiTokensApi.VerifyAccountTokenAsync(_accountId);

      if (verifyResult.Status != TokenStatus.Active)
      {
        PermissionValidationState.SetAccountValidationFailed(
          $"Account token is not active. Status: {verifyResult.Status}");

        return;
      }

      // Step 2: Get token details with policies
      var token = await _apiTokensApi.GetAccountTokenAsync(_accountId, verifyResult.Id);

      // Extract all permission group IDs from the token's policies
      var tokenPermissionIds = token.Policies
        .SelectMany(p => p.PermissionGroups)
        .Select(pg => pg.Id)
        .ToHashSet();

      // Step 3: Get all available permission groups to map IDs to names
      var allGroups = new List<PermissionGroup>();

      await foreach (var group in _apiTokensApi.GetAllAccountPermissionGroupsAsync(_accountId))
        allGroups.Add(group);

      // Build a set of permission NAMES the token has
      var tokenPermissionNames = tokenPermissionIds
        .Select(id => allGroups.FirstOrDefault(g => g.Id == id)?.Name)
        .Where(name => name is not null)
        .Cast<string>()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

      // Step 4: Validate all required permissions are present
      var missingPermissions = new List<string>();

      foreach (var required in RequiredAccountPermissions)
      {
        var hasPermission = tokenPermissionNames.Any(name =>
          name.Equals(required, StringComparison.OrdinalIgnoreCase) ||
          name.Contains(required, StringComparison.OrdinalIgnoreCase));

        if (!hasPermission)
          missingPermissions.Add(required);
      }

      // Step 5: Set validation state
      if (missingPermissions.Count > 0)
      {
        var errorMessage = BuildMissingPermissionsError(
          "CLOUDFLARE_API_TOKEN",
          "account-scoped",
          missingPermissions);
        PermissionValidationState.SetAccountValidationFailed(errorMessage);
      }
      else
      {
        PermissionValidationState.SetAccountValidationSucceeded();
      }
    }
    catch (Exception ex)
    {
      // If validation itself fails, set failure state
      PermissionValidationState.SetAccountValidationFailed(
        $"Account token validation failed with exception: {ex.Message}");
    }
  }

  /// <summary>
  ///   Runs user token validation. Called exactly once via lazy initialization.
  /// </summary>
  private static async Task RunUserValidationAsync()
  {
    if (_userApi is null)
    {
      // Can't validate without API - assume success.
      return;
    }

    try
    {
      var missingPermissions = new List<string>();

      // Test User Details:Read
      try
      {
        await _userApi.GetUserAsync();
      }
      catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden)
      {
        missingPermissions.Add("User Details:Read");
      }

      // Test Memberships:Read
      try
      {
        await _userApi.ListMembershipsAsync();
      }
      catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden)
      {
        missingPermissions.Add("Memberships:Read");
      }

      // Test User:Invites:Read
      try
      {
        await _userApi.ListInvitationsAsync();
      }
      catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden)
      {
        missingPermissions.Add("User:Invites:Read");
      }

      // Set validation state
      if (missingPermissions.Count > 0)
      {
        var errorMessage = BuildMissingPermissionsError(
          "CLOUDFLARE_USER_API_TOKEN",
          "user-scoped",
          missingPermissions);
        PermissionValidationState.SetUserValidationFailed(errorMessage);
      }
      else
      {
        PermissionValidationState.SetUserValidationSucceeded();
      }
    }
    catch (Exception ex)
    {
      PermissionValidationState.SetUserValidationFailed(
        $"User token validation failed with exception: {ex.Message}");
    }
  }

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
    sb.AppendLine("║  1. Go to Cloudflare Dashboard → My Profile → API Tokens                     ║");
    sb.AppendLine("║  2. Edit the token and add the missing permissions                           ║");
    sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");

    return sb.ToString();
  }

  #endregion
}
