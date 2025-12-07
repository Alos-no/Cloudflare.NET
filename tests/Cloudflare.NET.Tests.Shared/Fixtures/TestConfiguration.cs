namespace Cloudflare.NET.Tests.Shared.Fixtures;

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

/// <summary>
///   A helper class to build configuration from multiple sources (JSON, Environment, User Secrets) for use in
///   tests.
/// </summary>
public static class TestConfiguration
{
  #region Constants & Statics

  /// <summary>Gets the lazily-initialized configuration root.</summary>
  public static IConfigurationRoot Configuration { get; }

  /// <summary>Gets the Cloudflare settings loaded from the configuration.</summary>
  public static TestCloudflareSettings CloudflareSettings { get; }

  #endregion

  #region Constructors

  /// <summary>Initializes the static TestConfiguration by building the configuration sources.</summary>
  static TestConfiguration()
  {
    // Build configuration from appsettings.json, environment variables, and user secrets.
    var builder = new ConfigurationBuilder()
                  .AddJsonFile("appsettings.json", true, true)
                  .AddEnvironmentVariables();

    // Dynamically find the assembly containing the user secrets. This is necessary because
    // the UserSecretsId is defined in the test projects, not the core library. We scan
    // the loaded assemblies to find one that has the attribute and use it as the anchor.
    var testAssemblyWithSecrets = AppDomain.CurrentDomain.GetAssemblies()
                                           .FirstOrDefault(a => a.GetCustomAttribute<UserSecretsIdAttribute>() != null);

    if (testAssemblyWithSecrets != null)
      builder.AddUserSecrets(testAssemblyWithSecrets);

    Configuration = builder.Build();


    // Bind the configuration to a strongly-typed settings object.
    CloudflareSettings = new TestCloudflareSettings();
    Configuration.GetSection("Cloudflare").Bind(CloudflareSettings);
  }

  #endregion
}

/// <summary>
///   Represents the configuration options required for the Cloudflare API client tests, extending the base options
///   with test-specific properties.
/// </summary>
public class TestCloudflareSettings : CloudflareApiOptions
{
  #region Properties & Fields - Public

  /// <summary>The Cloudflare Zone ID to use for integration tests.</summary>
  public string ZoneId { get; set; } = string.Empty;

  /// <summary>The base domain associated with the test zone.</summary>
  public string BaseDomain { get; set; } = string.Empty;

  #endregion
}
