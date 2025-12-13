namespace Cloudflare.NET.Tests.Shared.Helpers;

using Fixtures;
using Xunit;

/// <summary>
///   A custom attribute for integration tests that require a user-scoped API token.
///   <para>
///     This attribute automatically skips the test if the <c>UserApiToken</c> secret is not configured.
///     Tests decorated with this attribute require a Cloudflare API token with <c>User:Edit</c> permission,
///     which is different from account-scoped tokens used by other integration tests.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     The Cloudflare User API requires user-level authentication, which is only available with user-scoped
///     API tokens. Account-scoped tokens (even with broad permissions) cannot access user-level endpoints.
///   </para>
///   <para>
///     Use this attribute instead of <see cref="IntegrationTestAttribute" /> for tests that call user-level
///     endpoints such as <c>GET /user</c> or <c>PATCH /user</c>.
///   </para>
///   <para>
///     This attribute extends <see cref="SkippableFactAttribute" /> to support dynamic runtime skipping
///     via <see cref="Skip.If" /> and <see cref="SkipException" />. This allows tests to be skipped
///     at runtime when permission validation fails.
///   </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class UserIntegrationTestAttribute : SkippableFactAttribute
{
  #region Constants & Statics

  /// <summary>
  ///   A lazily-initialized list of missing secrets required for user integration tests. This ensures the
  ///   check only happens once per test run.
  /// </summary>
  private static readonly Lazy<List<string>> MissingSecrets = new(GetMissingUserSecrets);

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="UserIntegrationTestAttribute" /> class.</summary>
  public UserIntegrationTestAttribute()
  {
    if (MissingSecrets.Value.Any())
      // If any required secrets are missing, skip the test with an informative message.
      Skip =
        $"Skipping user integration test: The following secrets are not configured: {string.Join(", ", MissingSecrets.Value)}. " +
        "Please configure a user-scoped API token with User:Edit permission.";
  }

  #endregion


  #region Methods

  /// <summary>Gets a list of secrets required specifically for user integration tests that are missing.</summary>
  /// <returns>A list of missing secret names.</returns>
  private static List<string> GetMissingUserSecrets()
  {
    var settings       = TestConfiguration.CloudflareSettings;
    var missingSecrets = new List<string>();

    // Check for the user-scoped API token (required for user-level endpoints).
    if (TestConfigurationValidator.IsSecretMissing(settings.UserApiToken))
      missingSecrets.Add($"Cloudflare:{nameof(settings.UserApiToken)}");

    return missingSecrets;
  }

  #endregion
}
