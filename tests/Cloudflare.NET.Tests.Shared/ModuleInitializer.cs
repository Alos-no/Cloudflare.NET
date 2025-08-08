namespace Cloudflare.NET.Tests.Shared;

using System.Runtime.CompilerServices;

public static class ModuleInitializer
{
  #region Methods

  [ModuleInitializer]
  public static void Initialize()
  {
    // Add a scrubber for the API token to avoid leaking it in snapshots.
    VerifierSettings.AddScrubber(text => text.Replace(TestConfiguration.CloudflareSettings.ApiToken, "---REDACTED-TOKEN---"));

    // Add scrubbers for other secrets.
    VerifierSettings.AddScrubber(text => text.Replace(TestConfiguration.CloudflareSettings.AccountId, "---REDACTED-ACCOUNT-ID---"));
    VerifierSettings.AddScrubber(text => text.Replace(TestConfiguration.CloudflareSettings.ZoneId, "---REDACTED-ZONE-ID---"));

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
