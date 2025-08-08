namespace Cloudflare.NET.Tests.Shared.Fixtures;

/// <summary>
///   Provides helper methods for validating that the necessary test configuration and
///   secrets are present.
/// </summary>
public static class TestConfigurationValidator
{
  #region Methods

  /// <summary>Checks if a configuration value is null, empty, or still has its placeholder value.</summary>
  /// <param name="value">The configuration value to check.</param>
  /// <returns>True if the secret is missing, otherwise false.</returns>
  public static bool IsSecretMissing(string? value)
  {
    return string.IsNullOrWhiteSpace(value) || value.Equals("from-user-secrets", StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  ///   Gets a list of all required secrets that are currently missing for the full
  ///   integration test suite.
  /// </summary>
  /// <returns>A list of names of the missing secrets. The list is empty if all secrets are present.</returns>
  public static List<string> GetMissingIntegrationSecrets()
  {
    var settings       = TestConfiguration.CloudflareSettings;
    var missingSecrets = new List<string>();

    if (IsSecretMissing(settings.ApiToken))
      missingSecrets.Add($"Cloudflare:{nameof(settings.ApiToken)}");
    if (IsSecretMissing(settings.AccountId))
      missingSecrets.Add($"Cloudflare:{nameof(settings.AccountId)}");
    if (IsSecretMissing(settings.ZoneId))
      missingSecrets.Add($"Cloudflare:{nameof(settings.ZoneId)}");

    return missingSecrets;
  }

  #endregion
}
