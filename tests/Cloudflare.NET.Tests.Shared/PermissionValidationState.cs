namespace Cloudflare.NET.Tests.Shared;

using Xunit;

/// <summary>
///   Static state tracker for permission validation results.
///   <para>
///     This class is used by <c>PermissionValidationTests</c> to record whether
///     API token permissions have been validated successfully. Other integration
///     tests can then check this state and skip if validation failed.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     <b>Why this exists:</b> xUnit has no built-in mechanism to abort all remaining
///     tests when a critical test fails. By using a static state that persists across
///     the test run, we can have permission validation tests (which run first) set
///     a flag, and all other integration tests can check that flag to skip gracefully.
///   </para>
///   <para>
///     <b>Thread Safety:</b> The state is set once during permission validation (which
///     runs first due to collection ordering) and then only read by subsequent tests.
///     We use volatile to ensure visibility across threads.
///   </para>
/// </remarks>
public static class PermissionValidationState
{
  #region Properties & Fields - Non-Public

  /// <summary>Volatile flag indicating whether account token validation has completed.</summary>
  private static volatile bool _accountValidationCompleted;

  /// <summary>Volatile flag indicating whether account token validation succeeded.</summary>
  private static volatile bool _accountValidationSucceeded;

  /// <summary>Volatile flag indicating whether user token validation has completed.</summary>
  private static volatile bool _userValidationCompleted;

  /// <summary>Volatile flag indicating whether user token validation succeeded.</summary>
  private static volatile bool _userValidationSucceeded;

  /// <summary>Error message from account token validation, if any.</summary>
  private static volatile string? _accountValidationError;

  /// <summary>Error message from user token validation, if any.</summary>
  private static volatile string? _userValidationError;

  #endregion


  #region Properties & Fields - Public

  /// <summary>
  ///   Gets whether the account token permission validation has completed (pass or fail).
  /// </summary>
  public static bool AccountValidationCompleted => _accountValidationCompleted;

  /// <summary>
  ///   Gets whether the account token has all required permissions.
  ///   Returns <c>false</c> if validation hasn't run yet or if it failed.
  /// </summary>
  public static bool AccountValidationSucceeded => _accountValidationSucceeded;

  /// <summary>
  ///   Gets whether the user token permission validation has completed (pass or fail).
  /// </summary>
  public static bool UserValidationCompleted => _userValidationCompleted;

  /// <summary>
  ///   Gets whether the user token has all required permissions.
  ///   Returns <c>false</c> if validation hasn't run yet or if it failed.
  /// </summary>
  public static bool UserValidationSucceeded => _userValidationSucceeded;

  /// <summary>
  ///   Gets the error message from account token validation, if validation failed.
  ///   Returns <c>null</c> if validation succeeded or hasn't run yet.
  /// </summary>
  public static string? AccountValidationError => _accountValidationError;

  /// <summary>
  ///   Gets the error message from user token validation, if validation failed.
  ///   Returns <c>null</c> if validation succeeded or hasn't run yet.
  /// </summary>
  public static string? UserValidationError => _userValidationError;

  /// <summary>
  ///   Gets whether both account and user token validations succeeded.
  ///   Returns <c>false</c> if either hasn't run yet or if either failed.
  /// </summary>
  public static bool AllValidationsSucceeded =>
    _accountValidationSucceeded && _userValidationSucceeded;

  /// <summary>
  ///   Gets whether any permission validation has failed.
  ///   Returns <c>true</c> if either validation completed and failed.
  /// </summary>
  public static bool AnyValidationFailed =>
    (_accountValidationCompleted && !_accountValidationSucceeded) ||
    (_userValidationCompleted && !_userValidationSucceeded);

  /// <summary>
  ///   Gets a combined skip reason message if any validation failed.
  ///   Returns <c>null</c> if no validations have failed.
  /// </summary>
  public static string? SkipReason
  {
    get
    {
      if (_accountValidationCompleted && !_accountValidationSucceeded)
        return $"Account token permission validation failed. {_accountValidationError ?? "Check PermissionValidationTests output."}";

      if (_userValidationCompleted && !_userValidationSucceeded)
        return $"User token permission validation failed. {_userValidationError ?? "Check PermissionValidationTests output."}";

      return null;
    }
  }

  #endregion


  #region Methods

  /// <summary>
  ///   Records that account token permission validation succeeded.
  /// </summary>
  /// <remarks>
  ///   Called by <c>PermissionValidationTests.AccountToken_HasAllRequiredPermissions</c>
  ///   after successfully validating all required permissions.
  /// </remarks>
  public static void SetAccountValidationSucceeded()
  {
    _accountValidationError     = null;
    _accountValidationSucceeded = true;
    _accountValidationCompleted = true;
  }

  /// <summary>
  ///   Records that account token permission validation failed.
  /// </summary>
  /// <param name="errorMessage">The detailed error message describing missing permissions.</param>
  /// <remarks>
  ///   Called by <c>PermissionValidationTests.AccountToken_HasAllRequiredPermissions</c>
  ///   when required permissions are missing.
  /// </remarks>
  public static void SetAccountValidationFailed(string? errorMessage = null)
  {
    _accountValidationError     = errorMessage;
    _accountValidationSucceeded = false;
    _accountValidationCompleted = true;
  }

  /// <summary>
  ///   Records that user token permission validation succeeded.
  /// </summary>
  /// <remarks>
  ///   Called by <c>PermissionValidationTests.UserToken_HasAllRequiredPermissions</c>
  ///   after successfully validating all required permissions.
  /// </remarks>
  public static void SetUserValidationSucceeded()
  {
    _userValidationError     = null;
    _userValidationSucceeded = true;
    _userValidationCompleted = true;
  }

  /// <summary>
  ///   Records that user token permission validation failed.
  /// </summary>
  /// <param name="errorMessage">The detailed error message describing missing permissions.</param>
  /// <remarks>
  ///   Called by <c>PermissionValidationTests.UserToken_HasAllRequiredPermissions</c>
  ///   when required permissions are missing.
  /// </remarks>
  public static void SetUserValidationFailed(string? errorMessage = null)
  {
    _userValidationError     = errorMessage;
    _userValidationSucceeded = false;
    _userValidationCompleted = true;
  }

  /// <summary>
  ///   Resets all validation state. Used for testing purposes only.
  /// </summary>
  /// <remarks>
  ///   This should only be called by test infrastructure code that needs
  ///   to reset state between test runs (e.g., in test fixtures).
  /// </remarks>
  public static void Reset()
  {
    _accountValidationCompleted = false;
    _accountValidationSucceeded = false;
    _accountValidationError     = null;
    _userValidationCompleted    = false;
    _userValidationSucceeded    = false;
    _userValidationError        = null;
  }

  /// <summary>
  ///   Ensures account token permission validation succeeded.
  ///   If validation failed, skips the test using Xunit.SkippableFact's Skip.If().
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Call this in fixture <c>InitializeAsync</c> or at the start of integration tests
  ///     that require account token permissions. If permission validation hasn't run yet,
  ///     this method does nothing (the test will proceed).
  ///   </para>
  ///   <para>
  ///     If permission validation ran and failed, this method uses <c>Skip.If()</c> from
  ///     Xunit.SkippableFact to skip the test with a clear message directing the user
  ///     to check PermissionValidationTests output.
  ///   </para>
  /// </remarks>
  public static void EnsureAccountPermissionsValidated()
  {
    Skip.If(
      _accountValidationCompleted && !_accountValidationSucceeded,
      "Account token permission validation FAILED. " +
      "See PermissionValidationTests output for the list of missing permissions.");
  }

  /// <summary>
  ///   Ensures user token permission validation succeeded.
  ///   If validation failed, skips the test using Xunit.SkippableFact's Skip.If().
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Call this in fixture <c>InitializeAsync</c> or at the start of integration tests
  ///     that require user token permissions. If permission validation hasn't run yet,
  ///     this method does nothing (the test will proceed).
  ///   </para>
  ///   <para>
  ///     If permission validation ran and failed, this method uses <c>Skip.If()</c> from
  ///     Xunit.SkippableFact to skip the test with a clear message directing the user
  ///     to check PermissionValidationTests output.
  ///   </para>
  /// </remarks>
  public static void EnsureUserPermissionsValidated()
  {
    Skip.If(
      _userValidationCompleted && !_userValidationSucceeded,
      "User token permission validation FAILED. " +
      "See PermissionValidationTests output for the list of missing permissions.");
  }

  #endregion
}
