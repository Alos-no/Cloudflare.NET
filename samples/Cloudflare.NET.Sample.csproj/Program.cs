namespace Cloudflare.NET.SampleCoreConsole;

using Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Samples;

/// <summary>
///   Entry point for the Core REST sample. This demonstrates how to: - Build a Generic
///   Host and wire up the Cloudflare REST client. - Run sample scenarios for various API features
///   like Zones, Accounts (R2), and Security. - Ensure created resources are cleaned up after the
///   sample runs.
/// </summary>
public static class Program
{
  #region Methods

  /// <summary>
  ///   Main entry point. Sets up the Host, resolves the REST client and sample classes, and
  ///   runs a series of end-to-end scenarios.
  /// </summary>
  public static async Task Main(string[] args)
  {
    var builder = Host.CreateApplicationBuilder(args);

    // Configure logging.
    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(o =>
    {
      o.SingleLine      = true;
      o.IncludeScopes   = true;
      o.TimestampFormat = "HH:mm:ss.fff ";
    });
    builder.Logging.SetMinimumLevel(LogLevel.Information);

    // Register the Cloudflare REST client.
    builder.Services.AddCloudflareApiClient(builder.Configuration);

    // Register sample classes.
    builder.Services.AddTransient<ZoneSamples>();
    builder.Services.AddTransient<AccountSamples>();
    builder.Services.AddTransient<SecuritySamples>();

    using var host          = builder.Build();
    var       logger        = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("CloudflareSample");
    var       configuration = host.Services.GetRequiredService<IConfiguration>();

    // Validate configuration.
    var options = host.Services.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;
    var zoneId  = configuration["Cloudflare:ZoneId"];

    if (IsSecretMissing(options.ApiToken, "ApiToken", logger) ||
        IsSecretMissing(options.AccountId, "AccountId", logger) ||
        IsSecretMissing(zoneId, "ZoneId", logger))
      return; // Errors are logged in the helper.

    // Get the base domain name from the zone details for use in other samples.
    var baseDomain = await GetBaseDomainAsync(host.Services, zoneId!, logger);

    if (string.IsNullOrEmpty(baseDomain))
    {
      logger.LogError("Could not determine base domain from Zone ID. Aborting.");
      return;
    }

    // Resolve sample runners from DI container.
    var zoneSamples     = host.Services.GetRequiredService<ZoneSamples>();
    var accountSamples  = host.Services.GetRequiredService<AccountSamples>();
    var securitySamples = host.Services.GetRequiredService<SecuritySamples>();

    logger.LogInformation("Starting Cloudflare.NET SDK Samples...");
    logger.LogInformation("Using AccountId: {AccountId}, ZoneId: {ZoneId}", options.AccountId, zoneId);

    // Run all scenarios.
    await Runner.RunAsync(logger, "Zone/DNS Management",
                          () => zoneSamples.RunDnsSamplesAsync(zoneId!));

    await Runner.RunAsync(logger, "Account/R2 Management",
                          () => accountSamples.RunR2SamplesAsync(zoneId!, baseDomain));

    await Runner.RunAsync(logger, "Security/Zone Firewall",
                          () => securitySamples.RunZoneFirewallSamplesAsync(zoneId!));

    await Runner.RunAsync(logger, "Security/Account Firewall",
                          securitySamples.RunAccountFirewallSamplesAsync);

    logger.LogInformation("All sample scenarios complete.");
  }

  /// <summary>Fetches zone details to get the domain name for use in samples that create hostnames.</summary>
  private static async Task<string> GetBaseDomainAsync(IServiceProvider services, string zoneId, ILogger logger)
  {
    var cf     = services.GetRequiredService<ICloudflareApiClient>();

    try
    {
      var zone = await cf.Zones.GetZoneDetailsAsync(zoneId);
      return zone.Name;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to get zone details. Check your credentials and ZoneId.");
      return string.Empty;
    }
  }

  /// <summary>Checks if a configuration value is missing and logs an error if it is.</summary>
  private static bool IsSecretMissing(string? value, string name, ILogger logger)
  {
    if (string.IsNullOrWhiteSpace(value) || value.Equals("from-user-secrets", StringComparison.OrdinalIgnoreCase))
    {
      logger.LogError(
        "Configuration value 'Cloudflare:{Name}' is missing. Please configure appsettings.json, environment variables, or user secrets.",
        name);
      return true;
    }

    return false;
  }

  #endregion
}
