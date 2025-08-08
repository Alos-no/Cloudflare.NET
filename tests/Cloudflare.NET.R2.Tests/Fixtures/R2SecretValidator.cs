namespace Cloudflare.NET.R2.Tests.Fixtures;

using NET.Tests.Shared.Fixtures;

/// <summary>Validates the secrets required specifically for R2 integration tests.</summary>
public class R2SecretValidator : IIntegrationTestSecretValidator
{
  #region Methods Impl

  /// <inheritdoc />
  public List<string> GetMissingSecrets()
  {
    var r2Settings     = R2TestConfiguration.R2Settings;
    var missingSecrets = new List<string>();

    if (TestConfigurationValidator.IsSecretMissing(r2Settings.AccessKeyId))
      missingSecrets.Add($"R2:{nameof(r2Settings.AccessKeyId)}");
    if (TestConfigurationValidator.IsSecretMissing(r2Settings.SecretAccessKey))
      missingSecrets.Add($"R2:{nameof(r2Settings.SecretAccessKey)}");

    return missingSecrets;
  }

  #endregion
}
