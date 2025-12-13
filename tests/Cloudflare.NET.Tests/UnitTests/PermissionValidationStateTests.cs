namespace Cloudflare.NET.Tests.UnitTests;

using Cloudflare.NET.Tests.Shared;

/// <summary>
///   Unit tests for <see cref="PermissionValidationState"/>.
/// </summary>
public class PermissionValidationStateTests
{
  /// <summary>
  ///   Verifies that Skip.If is called (throws SkipException) when account validation has failed.
  /// </summary>
  [Fact]
  public void EnsureAccountPermissionsValidated_WhenFailed_ThrowsSkipException()
  {
    // Arrange - Reset and set failed state.
    PermissionValidationState.Reset();
    PermissionValidationState.SetAccountValidationFailed("Test failure reason");

    // Act & Assert - Should throw SkipException (from Xunit.SkippableFact).
    var exception = Assert.Throws<SkipException>(() =>
      PermissionValidationState.EnsureAccountPermissionsValidated());

    Assert.Contains("Account token permission validation FAILED", exception.Message);

    // Cleanup.
    PermissionValidationState.Reset();
  }

  /// <summary>
  ///   Verifies that no exception is thrown when account validation succeeded.
  /// </summary>
  [Fact]
  public void EnsureAccountPermissionsValidated_WhenSucceeded_DoesNotThrow()
  {
    // Arrange.
    PermissionValidationState.Reset();
    PermissionValidationState.SetAccountValidationSucceeded();

    // Act & Assert - Should not throw.
    var exception = Record.Exception(() =>
      PermissionValidationState.EnsureAccountPermissionsValidated());

    Assert.Null(exception);

    // Cleanup.
    PermissionValidationState.Reset();
  }

  /// <summary>
  ///   Verifies that no exception is thrown when validation hasn't run yet.
  /// </summary>
  [Fact]
  public void EnsureAccountPermissionsValidated_WhenNotRun_DoesNotThrow()
  {
    // Arrange - Ensure clean state.
    PermissionValidationState.Reset();

    // Act & Assert - Should not throw (validation hasn't run).
    var exception = Record.Exception(() =>
      PermissionValidationState.EnsureAccountPermissionsValidated());

    Assert.Null(exception);
  }

  /// <summary>
  ///   Verifies that Skip.If is called (throws SkipException) when user validation has failed.
  /// </summary>
  [Fact]
  public void EnsureUserPermissionsValidated_WhenFailed_ThrowsSkipException()
  {
    // Arrange - Reset and set failed state.
    PermissionValidationState.Reset();
    PermissionValidationState.SetUserValidationFailed("Test failure reason");

    // Act & Assert - Should throw SkipException (from Xunit.SkippableFact).
    var exception = Assert.Throws<SkipException>(() =>
      PermissionValidationState.EnsureUserPermissionsValidated());

    Assert.Contains("User token permission validation FAILED", exception.Message);

    // Cleanup.
    PermissionValidationState.Reset();
  }
}
