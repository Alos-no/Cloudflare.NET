namespace Cloudflare.NET.R2.Tests.Fixtures;

using Configuration;
using Microsoft.Extensions.Configuration;
using NET.Tests.Shared.Fixtures;

/// <summary>A helper class to build and validate configuration specifically for R2 integration tests.</summary>
public static class R2TestConfiguration
{
  #region Constants & Statics

  /// <summary>Gets the R2 settings loaded from the shared configuration.</summary>
  public static R2Settings R2Settings { get; }

  #endregion

  #region Constructors

  static R2TestConfiguration()
  {
    // Bind R2 settings from the shared IConfigurationRoot.
    R2Settings = new R2Settings();
    TestConfiguration.Configuration.GetSection("R2").Bind(R2Settings);
  }

  #endregion

  #region Methods

  /// <summary>Gets a list of missing secrets required for R2 integration tests.</summary>
  /// <returns>A list of missing secret names.</returns>
  public static List<string> GetMissingR2Secrets()
  {
    var missingSecrets = new List<string>();
    if (TestConfigurationValidator.IsSecretMissing(R2Settings.AccessKeyId))
      missingSecrets.Add($"R2:{nameof(R2Settings.AccessKeyId)}");
    if (TestConfigurationValidator.IsSecretMissing(R2Settings.SecretAccessKey))
      missingSecrets.Add($"R2:{nameof(R2Settings.SecretAccessKey)}");
    return missingSecrets;
  }

  #endregion
}
