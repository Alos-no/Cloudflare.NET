namespace Cloudflare.NET.R2.Tests;

using System.Runtime.CompilerServices;
using Fixtures;
using NET.Tests.Shared.Fixtures;

public static class ModuleInitializer
{
  #region Methods

  [ModuleInitializer]
  public static void Initialize()
  {
    // The shared initializer in Cloudflare.NET.Tests.Shared.dll is run automatically by the runtime.
    // It already scrubs AccountID and ApiToken.
    var r2Settings = R2TestConfiguration.R2Settings;

    if (!TestConfigurationValidator.IsSecretMissing(r2Settings.AccessKeyId))
      VerifierSettings.AddScrubber(text => text.Replace(r2Settings.AccessKeyId, "---REDACTED-R2-ACCESS-KEY-ID---"));

    if (!TestConfigurationValidator.IsSecretMissing(r2Settings.SecretAccessKey))
      VerifierSettings.AddScrubber(text => text.Replace(r2Settings.SecretAccessKey, "---REDACTED-R2-SECRET-ACCESS-KEY---"));
  }

  #endregion
}
