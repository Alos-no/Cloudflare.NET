namespace Cloudflare.NET.Tests.Shared.Fixtures;

/// <summary>Validates the core secrets required for all Cloudflare integration tests.</summary>
public class CoreSecretValidator : IIntegrationTestSecretValidator
{
  #region Methods Impl

  /// <inheritdoc />
  public List<string> GetMissingSecrets()
  {
    var settings       = TestConfiguration.CloudflareSettings;
    var missingSecrets = new List<string>();

    if (TestConfigurationValidator.IsSecretMissing(settings.ApiToken))
      missingSecrets.Add($"Cloudflare:{nameof(settings.ApiToken)}");
    if (TestConfigurationValidator.IsSecretMissing(settings.AccountId))
      missingSecrets.Add($"Cloudflare:{nameof(settings.AccountId)}");
    if (TestConfigurationValidator.IsSecretMissing(settings.ZoneId))
      missingSecrets.Add($"Cloudflare:{nameof(settings.ZoneId)}");

    return missingSecrets;
  }

  #endregion
}
