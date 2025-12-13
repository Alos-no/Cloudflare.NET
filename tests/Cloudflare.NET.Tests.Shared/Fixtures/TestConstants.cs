namespace Cloudflare.NET.Tests.Shared.Fixtures;

/// <summary>Provides constant values used across the test suite.</summary>
public static class TestConstants
{
  #region Constants & Statics

  /// <summary>The base URL for the Cloudflare API used in unit tests.</summary>
  public const string CloudflareApiBaseUrl = "https://api.cloudflare.com/client/v4";

  #endregion


  /// <summary>Contains the different categories of tests for filtering with `dotnet test`.</summary>
  public static class TestCategories
  {
    #region Constants & Statics

    /// <summary>The trait category for fast unit tests that use mocks.</summary>
    public const string Unit = "Unit";

    /// <summary>The trait category for integration tests that hit the live Cloudflare API.</summary>
    public const string Integration = "Integration";

    #endregion
  }
}
