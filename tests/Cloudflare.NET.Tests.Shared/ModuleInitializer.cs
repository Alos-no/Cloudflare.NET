namespace Cloudflare.NET.Tests.Shared;

using System.Runtime.CompilerServices;

public static class ModuleInitializer
{
  #region Methods

  [ModuleInitializer]
  public static void Initialize()
  {
    var settings = TestConfiguration.CloudflareSettings;

    // Add a scrubber for the API token to avoid leaking it in snapshots, but only if the token exists.
    if (!TestConfigurationValidator.IsSecretMissing(settings.ApiToken))
      VerifierSettings.AddScrubber(text => text.Replace(settings.ApiToken, "---REDACTED-TOKEN---"));

    // Add scrubbers for other secrets, but only if they exist.
    if (!TestConfigurationValidator.IsSecretMissing(settings.AccountId))
      VerifierSettings.AddScrubber(text => text.Replace(settings.AccountId, "---REDACTED-ACCOUNT-ID---"));

    if (!TestConfigurationValidator.IsSecretMissing(settings.ZoneId))
      VerifierSettings.AddScrubber(text => text.Replace(settings.ZoneId, "---REDACTED-ZONE-ID---"));

    // Use Argonaut for beautiful diffs.
    VerifyDiffPlex.Initialize();

    // Configure snapshot location to be in a "_snapshots" directory next to the test file.
    DerivePathInfo((sourceFile, projectDirectory, type, method) => new(
                     Path.Combine(Path.GetDirectoryName(sourceFile)!, "_snapshots"),
                     type.Name,
                     method.Name));
  }

  #endregion
}
